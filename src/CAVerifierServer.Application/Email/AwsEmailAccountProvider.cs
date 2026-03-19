using System;
using System.Collections.Generic;
using System.Linq;
using CAVerifierServer.Options;

namespace CAVerifierServer.Email;

internal static class AwsEmailAccountProvider
{
    public static IReadOnlyList<AwsEmailAccountOptions> BuildConfiguredAccounts(AwsEmailOptions options)
    {
        var configuredAccounts = options.Accounts ?? new List<AwsEmailAccountOptions>();
        var hasConfiguredAccounts = configuredAccounts.Count > 0;
        var accounts = configuredAccounts
            .Where(account => account != null)
            .Select((account, index) => NormalizeAccount(account, index))
            .Where(account => account.Enabled)
            .ToList();

        if (accounts.Count == 0 && !hasConfiguredAccounts && HasLegacyAccount(options))
        {
            accounts.Add(NormalizeAccount(new AwsEmailAccountOptions
            {
                Key = "default",
                Enabled = true,
                From = options.From,
                FromName = options.FromName,
                SmtpUsername = options.SmtpUsername,
                SmtpPassword = options.SmtpPassword,
                ConfigSet = options.ConfigSet,
                Host = options.Host,
                Port = options.Port
            }, 0));
        }

        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("No enabled aws email account is configured.");
        }

        var duplicatedKeys = accounts.GroupBy(account => account.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicatedKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicated aws email account keys: {string.Join(", ", duplicatedKeys)}");
        }

        return accounts;
    }

    private static AwsEmailAccountOptions NormalizeAccount(AwsEmailAccountOptions account, int index)
    {
        var normalized = new AwsEmailAccountOptions
        {
            Key = string.IsNullOrWhiteSpace(account.Key) ? $"account-{index + 1}" : account.Key.Trim(),
            Enabled = account.Enabled,
            From = account.From?.Trim(),
            FromName = account.FromName?.Trim(),
            SmtpUsername = account.SmtpUsername?.Trim(),
            SmtpPassword = account.SmtpPassword,
            ConfigSet = account.ConfigSet?.Trim(),
            Host = account.Host?.Trim(),
            Port = account.Port
        };

        ValidateAccount(normalized);
        return normalized;
    }

    private static void ValidateAccount(AwsEmailAccountOptions account)
    {
        if (!account.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(account.From))
        {
            throw new InvalidOperationException($"Aws email account {account.Key} must configure From.");
        }

        if (string.IsNullOrWhiteSpace(account.SmtpUsername))
        {
            throw new InvalidOperationException(
                $"Aws email account {account.Key} must configure SmtpUsername.");
        }

        if (string.IsNullOrWhiteSpace(account.SmtpPassword))
        {
            throw new InvalidOperationException(
                $"Aws email account {account.Key} must configure SmtpPassword.");
        }

        if (string.IsNullOrWhiteSpace(account.Host))
        {
            throw new InvalidOperationException($"Aws email account {account.Key} must configure Host.");
        }

        if (account.Port <= 0)
        {
            throw new InvalidOperationException($"Aws email account {account.Key} must configure Port.");
        }
    }

    private static bool HasLegacyAccount(AwsEmailOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.From) &&
               !string.IsNullOrWhiteSpace(options.SmtpUsername) &&
               !string.IsNullOrWhiteSpace(options.SmtpPassword) &&
               !string.IsNullOrWhiteSpace(options.Host) &&
               options.Port > 0;
    }
}
