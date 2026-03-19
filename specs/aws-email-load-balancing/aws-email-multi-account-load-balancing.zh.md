# AWS 邮件多账号负载方案

切换语言：[English](./aws-email-multi-account-load-balancing.md)

## 摘要

本文档描述 verifier 服务的 AWS SES SMTP 多账号负载方案。目标是在不改变业务入口 `IEmailSender` 的前提下，引入本地 `RoundRobin` 账号选择、同次请求 failover 和失败冷却机制，消除单账号带来的邮件链路单点瓶颈。

## 背景

此前 verifier 邮件链路只使用单个 AWS SES SMTP 账号。只要该账号触发额度上限、限流、认证异常或区域 SMTP 可用性问题，整个邮件验证码链路就会不可用。

当前实现上下文如下：

- 业务入口仍然在 `EmailVerifyCodeSender`，负责组装邮件模板并通过 `IEmailSender` 入队。
- 这次 feature 保留现有 ABP email/background-job 执行语义，不会额外引入 durable background-job store。
- `AwsEmailSender` 负责账号选择、failover、cooldown 和发送编排。
- `AwsSmtpEmailDeliveryClient` 负责实际 SMTP 投递。
- 多账号配置已经通过 `awsEmail.Accounts[]` 支持。
- 当 `Accounts` 未配置时，仍兼容 legacy root-level 的 `awsEmail.From`、`FromName`、`SmtpUsername`、`SmtpPassword`、`ConfigSet`、`Host`、`Port`。

## 目标

- 在 verifier 服务内支持多个 AWS SMTP 账号。
- 使用本地 `RoundRobin` 分摊邮件流量。
- 在同一次发送请求内，首账号失败后自动切换到下一个账号。
- 对可重试失败的账号施加临时冷却窗口。
- 保持控制层和应用层入口不变，不修改现有 HTTP 接口和 DTO。

## 非目标

- 本版本不做跨实例全局均衡。
- 本版本不提供跨节点、跨区域或重启后的全局 SES quota 协调。
- 本版本不变更现有 background job 的持久化和执行模型。
- 本版本不引入非 AWS 邮件 provider。
- 不修改 Controller、DTO 或 HTTP 协议。
- 不引入额外文档站点或生成器。

## 配置设计

根配置示例：

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

配置字段说明：

- `SelectionMode`
  - 当前仅支持 `RoundRobin`
- `FailureCooldownSeconds`
  - 可重试失败后的冷却时长
- `Image`
  - 邮件模板共享图片资源地址
- `Accounts[]`
  - AWS SMTP 多账号配置主入口
- legacy root-level SMTP 字段
  - 仅在 `Accounts` 为空时作为兼容 fallback 生效

单账号字段说明：

- `Key`
  - 用于路由和日志的稳定账号标识
- `Enabled`
  - 关闭后不参与候选账号选择
- `From`
- `FromName`
- `SmtpUsername`
- `SmtpPassword`
- `Host`
- `Port`
- `ConfigSet`
  - 可选，透传到 `X-SES-CONFIGURATION-SET`

## 实现设计

核心组件：

- `EmailVerifyCodeSender`
  - 负责构建验证码和通知类 HTML 模板
  - 通过 `IEmailSender.QueueAsync(..., true)` 作为应用层入口
  - 但不会改变宿主当前是 durable background job 还是 in-process 执行
- `AwsEmailSender`
  - 保持 `IEmailSender` 作为统一应用层入口
  - 从 `AwsEmailOptions` 中解析启用的 AWS 账号
  - 负责 round-robin、failover、cooldown 和日志
- `AwsSmtpEmailDeliveryClient`
  - 使用 AWS SES SMTP 凭据执行实际 SMTP 发送

数据流：

1. 业务代码生成邮件主题和 HTML 正文。
2. `EmailVerifyCodeSender` 通过 `IEmailSender` 入队。
3. `AwsEmailSender` 解析当前可用 AWS SMTP 账号集合。
4. 发送器按本地 round-robin 选择首个账号。
5. 如果出现可重试错误，则将当前账号打入 cooldown，并在同一次请求内尝试下一个账号。
6. 如果所有账号都在 cooldown，则快速失败，避免对全部账号形成重试风暴。
7. 如果所有候选账号都失败，则把账号尝试链路记录到日志中，并抛出通用的临时不可用异常。

## 路由与失败处理

选择策略：

- round-robin 为进程内本地行为。
- 只有 `Enabled=true` 的账号参与候选集合。
- 正常情况下，处于 cooldown 的账号会被跳过。
- 当所有账号都在 cooldown 时，发送器会立即失败并等待 cooldown 到期。

可重试错误处理：

- 可重试错误被刻意限制为临时性的 SMTP 故障，例如 SES quota/rate throttling 和 service-unavailable 类响应。
- 可重试错误会让当前账号进入 cooldown。
- 同一次发送请求会继续尝试下一个候选账号。
- cooldown 到期后，该账号自动重新参与选择。

不可重试错误处理：

- 遇到非法地址、永久收件人失败、认证/授权失败以及本地消息构造错误时，会立即停止 failover。

消息对象处理：

- 每次 failover 都使用独立的 attempt-local `MailMessage`。
- 发送器会先把原始消息克隆成一份内存模板。
- 每个账号尝试都从该模板派生，避免账号级别的修改污染后续重试。
- 实际 SMTP `From` 地址始终由选中的账号决定，调用方传入的 `From` 不会把 failover 锁死到错误的 SES identity。
- 这也保护了附件、alternate views、自定义 headers、alternate view `BaseUri` 和 non-seekable attachment stream 的 failover 行为。

日志行为：

- 日志包含 `CorrelationId`、账号 key、尝试顺序、脱敏收件人和 cooldown 到期时间。
- 成功日志区分“首选账号成功”和“failover 成功”。

## 测试与验证

已覆盖场景：

- `Accounts` 未配置时，legacy 单账号 fallback 仍可工作。
- 单实例内多账号 round-robin 顺序正确。
- 首账号遇到可重试的 SES quota/rate 错误时，同次请求 failover 可成功发送。
- cooldown 能跳过刚失败的账号。
- cooldown 到期后，账号可重新参与选择。
- HTML 邮件仍按 HTML 形式入队，并在 SMTP 发送时保留 `IsBodyHtml`。
- 显式配置但全部 disabled 的 `Accounts` 不会错误 fallback 到 legacy root fields。
- 所有账号都在 cooldown 时，会快速失败且不新增 SMTP 尝试。
- non-seekable attachment stream 在 failover 下仍能保留内容。
- alternate view `BaseUri` 在 failover 下仍可保留。
- `X-SES-CONFIGURATION-SET` 会按 attempt 切换，且目标账号未配置时会清空旧 header。
- 不可重试异常矩阵会立即停止 failover，覆盖 `FormatException`、`ArgumentException`、`SmtpFailedRecipientException`、`SmtpFailedRecipientsException` 以及永久性 SMTP 认证错误。
- failover 时会切换到目标账号对应的 `From` 地址。
- 所有账号都失败时，对外只返回通用临时不可用异常，详细尝试链路保留在日志中。

实现阶段使用的验证命令：

```bash
dotnet build CAVerifierServer.sln
DOTNET_ROLL_FORWARD=Major dotnet test test/CAVerifierServer.Application.Tests/CAVerifierServer.Application.Tests.csproj --filter AwsEmailSenderLoadBalancingTests
```

## 上线与回滚

建议上线方式：

- 先以单个启用账号发布新代码。
- 先确认目标宿主环境是否已经启用 durable 的 ABP background-job storage；这次 feature 本身不会补上这层能力。
- 验证邮件成功率、SMTP 认证和账号路由日志。
- 再逐步打开其他账号。
- 结合真实 SES 响应观察 failover 与 cooldown 行为。
- 确保 `awsEmail.Accounts[]` 中的每个条目都对应独立可用的 SES sender identity 和 quota 池，不要把同一已耗尽 SES account 的多组 credentials 误当成新增容量。

回滚策略：

- 在 `awsEmail.Accounts[]` 中仅保留一个启用账号。
- 如有需要，也可以移除 `Accounts` 并改回 legacy root-level SMTP 字段。
- 无需回滚 Controller、API 或 DTO。

## 相关实现

- `src/CAVerifierServer.Application/Options/AwsEmailOptions.cs`
- `src/CAVerifierServer.Application/Email/AwsEmailSender.cs`
- `src/CAVerifierServer.Application/Email/AwsSmtpEmailDeliveryClient.cs`
- `src/CAVerifierServer.Application/VerifyCodeSender/EmailVerifyCodeSender.cs`
- `test/CAVerifierServer.Application.Tests/EmailSender/AwsEmailSenderLoadBalancingTests.cs`
