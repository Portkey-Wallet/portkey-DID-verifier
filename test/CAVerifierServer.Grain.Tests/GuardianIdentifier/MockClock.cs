

using Microsoft.Extensions.Options;
using Volo.Abp.Timing;

namespace CAVerifierServer.Grain.Tests.GuardianIdentifier;


public class MockClock : IClock
{

    private int _offset;

    public void SetOffset(int offset)
    {
        _offset = offset;
    }

    protected AbpClockOptions Options { get; }

    public MockClock(IOptions<AbpClockOptions> options)
    {
        Options = options.Value;
    }

    public virtual DateTime _Now => Options.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
    
    public DateTime Now => _Now.AddMinutes(_offset);

    public virtual DateTimeKind Kind => Options.Kind;

    public virtual bool SupportsMultipleTimezone => Options.Kind == DateTimeKind.Utc;

    public virtual DateTime Normalize(DateTime dateTime)
    {
        if (Kind == DateTimeKind.Unspecified || Kind == dateTime.Kind)
        {
            return dateTime;
        }

        if (Kind == DateTimeKind.Local && dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime.ToLocalTime();
        }

        if (Kind == DateTimeKind.Utc && dateTime.Kind == DateTimeKind.Local)
        {
            return dateTime.ToUniversalTime();
        }

        return DateTime.SpecifyKind(dateTime, Kind);
    }
}