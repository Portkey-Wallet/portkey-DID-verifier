using System;
using AElf.Kernel;
using AElf.Types;
using CAVerifierServer.Options;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.Infrastructure;

public interface IVerifiedRequestTimestampCacheProvider
{
    bool IsVerificationRequestExpiredOrHandledBefore(Hash verificationRequestHash, Timestamp requestTimestamp);
}

public class VerifiedRequestTimestampCacheProvider : IVerifiedRequestTimestampCacheProvider
{
    private readonly MemoryCache _validateResultsCache;
    private readonly VerificationRequestExpireTimeOptions _verificationRequestExpireTimeOptions;

    public VerifiedRequestTimestampCacheProvider(
        IOptionsSnapshot<VerificationRequestExpireTimeOptions> verificationRequestExpireTimeOptions)
    {
        _verificationRequestExpireTimeOptions = verificationRequestExpireTimeOptions.Value;

        _validateResultsCache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency =
                TimeSpan.FromSeconds(_verificationRequestExpireTimeOptions.ExpireTime)
        });
    }

    public bool IsVerificationRequestExpiredOrHandledBefore(Hash verificationRequestHash, Timestamp requestTimestamp)
    {
        var now = TimestampHelper.GetUtcNow();
        var duration = now - requestTimestamp;
        if (duration.Seconds > _verificationRequestExpireTimeOptions.ExpireTime)
        {
            // Not handled before, but expired.
            return true;
        }

        if (_validateResultsCache.TryGetValue(verificationRequestHash, out var handleTimestamp) && handleTimestamp is not null)
        {
            // Handled before.
            return true;
        }

        _validateResultsCache.Set(verificationRequestHash, now);
        return false;
    }
}