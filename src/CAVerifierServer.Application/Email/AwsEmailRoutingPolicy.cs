using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Timing;

namespace CAVerifierServer.Email;

public sealed class AwsEmailRoutingPolicy
{
    private readonly AwsEmailOptions _options;
    private readonly IClock _clock;
    private readonly IReadOnlyList<AwsEmailAccountOptions> _configuredAccounts;
    private readonly ILogger<AwsEmailRoutingPolicy> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _cooldownAccounts = new(StringComparer.OrdinalIgnoreCase);
    private int _roundRobinCursor = -1;

    public AwsEmailRoutingPolicy(IOptions<AwsEmailOptions> awsEmailOptions, IClock clock,
        ILogger<AwsEmailRoutingPolicy> logger)
    {
        _options = awsEmailOptions.Value;
        _clock = clock;
        _logger = logger;
        _configuredAccounts = AwsEmailAccountProvider.BuildConfiguredAccounts(_options);
    }

    public IReadOnlyList<AwsEmailAccountOptions> ConfiguredAccounts => _configuredAccounts;

    public List<AwsEmailAccountOptions> GetOrderedCandidateAccounts(out DateTime? nextAvailableAt)
    {
        var now = _clock.Now;
        var availableAccounts = _configuredAccounts.Where(account => !IsCoolingDown(account.Key, now)).ToList();
        if (availableAccounts.Count == 0)
        {
            nextAvailableAt = _cooldownAccounts.Values.DefaultIfEmpty().Cast<DateTime?>().Min();
            return availableAccounts;
        }

        nextAvailableAt = null;
        if (_options.SelectionMode != AwsEmailSelectionMode.RoundRobin)
        {
            _logger.LogWarning("Unsupported aws email selection mode {SelectionMode}. Falling back to RoundRobin.",
                _options.SelectionMode);
        }

        var startIndex = GetStartIndex(availableAccounts.Count);
        return availableAccounts.Skip(startIndex).Concat(availableAccounts.Take(startIndex)).ToList();
    }

    public DateTime MarkCooldown(string accountKey)
    {
        var cooldownUntil = _clock.Now.AddSeconds(Math.Max(0, _options.FailureCooldownSeconds));
        _cooldownAccounts[accountKey] = cooldownUntil;
        return cooldownUntil;
    }

    public void ClearCooldownOnSuccess(string accountKey, DateTime attemptStartedAt)
    {
        if (!_cooldownAccounts.TryGetValue(accountKey, out var cooldownUntil))
        {
            return;
        }

        if (cooldownUntil <= attemptStartedAt)
        {
            _cooldownAccounts.TryRemove(new KeyValuePair<string, DateTime>(accountKey, cooldownUntil));
        }
    }

    private int GetStartIndex(int count)
    {
        if (count <= 1)
        {
            return 0;
        }

        var next = Interlocked.Increment(ref _roundRobinCursor);
        return (int)((uint)next % (uint)count);
    }

    private bool IsCoolingDown(string accountKey, DateTime now)
    {
        if (!_cooldownAccounts.TryGetValue(accountKey, out var cooldownUntil))
        {
            return false;
        }

        if (cooldownUntil > now)
        {
            return true;
        }

        _cooldownAccounts.TryRemove(accountKey, out _);
        return false;
    }
}
