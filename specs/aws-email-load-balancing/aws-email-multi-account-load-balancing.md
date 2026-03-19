# AWS Email Multi-Account Load Balancing

Switch language: [中文](./aws-email-multi-account-load-balancing.zh.md)

## Summary

This document describes the AWS SES SMTP multi-account load-balancing design for the verifier service. The goal is to remove the single-account bottleneck in the email pipeline by introducing local round-robin account selection, same-request failover, and failure cooldown while keeping `IEmailSender` as the application entry point.

## Background

The previous verifier email pipeline used a single AWS SES SMTP account. If that account hit quota, throttling, authentication issues, or regional SMTP availability problems, the whole email verification flow became unavailable.

Current implementation context:

- Business entry stays in `EmailVerifyCodeSender`, which builds email templates and queues messages through `IEmailSender`.
- This feature keeps the existing ABP email/background-job execution semantics; it does not add a durable background-job store.
- `AwsEmailSender` handles account selection, failover, cooldown, and delivery orchestration.
- `AwsSmtpEmailDeliveryClient` performs the actual SMTP transport.
- Multi-account configuration is now supported through `awsEmail.Accounts[]`.
- Legacy root-level `awsEmail.From`, `FromName`, `SmtpUsername`, `SmtpPassword`, `ConfigSet`, `Host`, and `Port` are still accepted as fallback when `Accounts` is not configured.

## Goals

- Support multiple AWS SMTP accounts inside the verifier service.
- Distribute email traffic locally with `RoundRobin`.
- Retry within the same send request by failing over to the next available AWS account.
- Put retryable failed accounts into a temporary cooldown window.
- Preserve the existing application entry point and avoid API changes in controllers or app services.

## Non-Goals

- No cross-instance global balancing.
- No global SES quota coordination across nodes, regions, or restarts.
- No durable background-job persistence or execution-model change in this version.
- No non-AWS email provider integration in this version.
- No DTO, controller, or HTTP contract changes.
- No new documentation site or generator.

## Configuration Design

Root section:

```json
"awsEmail": {
  "SelectionMode": "RoundRobin",
  "FailureCooldownSeconds": 300,
  "Image": "https://example.com/email-banner.png",
  "Accounts": [
    {
      "Key": "primary",
      "Enabled": true,
      "From": "noreply@example.com",
      "FromName": "Portkey Finance",
      "SmtpUsername": "smtp-user-1",
      "SmtpPassword": "smtp-password-1",
      "Host": "email-smtp.ap-northeast-1.amazonaws.com",
      "Port": 587,
      "ConfigSet": "portkey-default"
    },
    {
      "Key": "secondary",
      "Enabled": true,
      "From": "noreply@example.com",
      "FromName": "Portkey Finance",
      "SmtpUsername": "smtp-user-2",
      "SmtpPassword": "smtp-password-2",
      "Host": "email-smtp.us-west-2.amazonaws.com",
      "Port": 587,
      "ConfigSet": "portkey-failover"
    }
  ]
}
```

Configuration fields:

- `SelectionMode`
  - Current supported value: `RoundRobin`
- `FailureCooldownSeconds`
  - Cooldown duration after a retryable failure
- `Image`
  - Shared template asset URL used by email body builders
- `Accounts[]`
  - Multi-account AWS SMTP source of truth
- Legacy root-level SMTP fields
  - Supported only as backward-compatible fallback when `Accounts` is empty

Account fields:

- `Key`
  - Stable account identifier for routing and logs
- `Enabled`
  - Disabled accounts are excluded from candidate selection
- `From`
- `FromName`
- `SmtpUsername`
- `SmtpPassword`
- `Host`
- `Port`
- `ConfigSet`
  - Optional SES configuration set, propagated through `X-SES-CONFIGURATION-SET`

## Implementation Design

Main components:

- `EmailVerifyCodeSender`
  - Builds verification and notification HTML templates
  - Calls `IEmailSender.QueueAsync(..., true)` as the application entry
  - Does not change whether the host executes that queue through durable background jobs or in-process execution
- `AwsEmailSender`
  - Keeps `IEmailSender` as the single application-level email entry point
  - Loads enabled AWS accounts from `AwsEmailOptions`
  - Applies round-robin selection, failover, cooldown, and logging
- `AwsSmtpEmailDeliveryClient`
  - Performs the actual SMTP send using AWS SES SMTP credentials

Data flow:

1. Business code creates email subject and HTML body.
2. `EmailVerifyCodeSender` queues the message through `IEmailSender`.
3. `AwsEmailSender` resolves the candidate AWS SMTP accounts.
4. The sender picks the first account using local round-robin.
5. If a retryable error happens, the failed account enters cooldown and the sender tries the next account in the same request.
6. If all accounts are in cooldown, the sender fails fast instead of generating a retry storm against every configured account.
7. If every candidate fails, the sender logs the attempted account chain and throws a generic temporary-unavailable exception.

## Routing and Failure Handling

Selection behavior:

- Round-robin is local to the process.
- Only enabled accounts participate in selection.
- Accounts in cooldown are skipped during normal candidate selection.
- If every account is cooling down, the sender fails fast and waits for cooldown expiry.

Retryable failure behavior:

- Retryable errors are intentionally limited to transient SMTP failures such as SES quota/rate throttling and service-unavailable style responses.
- Retryable errors move the current account into cooldown.
- The same email send request continues with the next candidate account.
- Cooldown expires automatically based on `FailureCooldownSeconds`.

Non-retryable failure behavior:

- The sender stops immediately for invalid addresses, permanent recipient failures, authentication/authorization failures, and local message-construction issues.

Message handling:

- Each failover attempt uses an attempt-local cloned `MailMessage`.
- The sender first clones the original message into an in-memory template.
- Every account attempt is created from that template so account-specific mutations do not leak across retries.
- The selected account always decides the SMTP `From` address. Caller-provided `From` values do not pin later failover attempts to the wrong SES identity.
- This also protects failover for attachments, alternate views, custom headers, alternate view `BaseUri`, and non-seekable attachment streams.

Logging behavior:

- Logs include `CorrelationId`, selected account key, attempt order, masked recipient, and cooldown expiration time.
- Success logs distinguish preferred-account success from failover success.

## Tests and Validation

Covered scenarios:

- Legacy single-account fallback still works when `Accounts` is not configured.
- Multi-account round-robin order works in a single instance.
- Same-request failover succeeds when the first account hits a retryable SES quota/rate error.
- Cooldown skips recently failed accounts.
- Expired cooldown allows the account to re-enter selection.
- HTML emails remain queued and preserve `IsBodyHtml` during SMTP send.
- Explicitly configured but fully disabled `Accounts` do not fall back to legacy root fields.
- All-accounts-in-cooldown path fails fast without new SMTP attempts.
- Non-seekable attachment streams still survive failover.
- Alternate view `BaseUri` survives failover.
- `X-SES-CONFIGURATION-SET` switches per attempt and is cleared when the target account has no config set.
- Non-retryable exceptions stop failover immediately across the covered matrix (`FormatException`, `ArgumentException`, `SmtpFailedRecipientException`, `SmtpFailedRecipientsException`, and permanent SMTP auth failures).
- Account-specific `From` addresses are applied during failover.
- All-account failure returns a generic temporary-unavailable exception while the detailed attempt chain stays in logs.

Validation commands used during implementation:

```bash
dotnet build CAVerifierServer.sln
DOTNET_ROLL_FORWARD=Major dotnet test test/CAVerifierServer.Application.Tests/CAVerifierServer.Application.Tests.csproj --filter AwsEmailSenderLoadBalancingTests
```

## Rollout and Rollback

Recommended rollout:

- Release the code with one enabled account first.
- Verify whether the target host environment has durable ABP background-job storage enabled; this feature does not add it.
- Validate email success rate, SMTP authentication, and account routing logs.
- Enable additional accounts gradually.
- Observe failover and cooldown behavior under real SES responses.
- Ensure each entry in `awsEmail.Accounts[]` maps to an independently usable SES sender identity and quota pool; do not model multiple credentials from the same exhausted SES account as separate capacity.

Rollback strategy:

- Keep only one enabled account in `awsEmail.Accounts[]`.
- If needed, remove `Accounts` entirely and use the legacy root-level SMTP fields.
- No controller, API, or DTO rollback is required.

## Related Implementation

- `src/CAVerifierServer.Application/Options/AwsEmailOptions.cs`
- `src/CAVerifierServer.Application/Email/AwsEmailSender.cs`
- `src/CAVerifierServer.Application/Email/AwsSmtpEmailDeliveryClient.cs`
- `src/CAVerifierServer.Application/VerifyCodeSender/EmailVerifyCodeSender.cs`
- `test/CAVerifierServer.Application.Tests/EmailSender/AwsEmailSenderLoadBalancingTests.cs`
