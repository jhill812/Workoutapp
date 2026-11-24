namespace WorkoutTracker.Services;

/// <summary>Countdown between sets. Shared app-wide so it keeps running while you navigate.</summary>
public sealed class RestTimerService(TimeProvider time) : IDisposable
{
    private ITimer? _timer;
    private DateTimeOffset? _endsAt;

    public event Action? Tick;
    /// <summary>Raised once when the countdown reaches zero (not when skipped).</summary>
    public event Action? Finished;

    public bool IsRunning => _endsAt is not null;
    public TimeSpan Total { get; private set; }

    public TimeSpan Remaining =>
        _endsAt is { } end ? TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling((end - time.GetUtcNow()).TotalSeconds))) : TimeSpan.Zero;

    public double Progress => Total <= TimeSpan.Zero ? 0 : Math.Clamp(1 - Remaining / Total, 0, 1);

    public void Start(int seconds)
    {
        if (seconds <= 0) { Stop(); return; }
        Total = TimeSpan.FromSeconds(seconds);
        _endsAt = time.GetUtcNow() + Total;
        _timer ??= time.CreateTimer(_ => Check(), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
        Tick?.Invoke();
    }

    public void Adjust(int seconds)
    {
        if (_endsAt is not { } end) return;
        _endsAt = end.AddSeconds(seconds);
        Total += TimeSpan.FromSeconds(seconds);
        if (Remaining <= TimeSpan.Zero || Total <= TimeSpan.Zero) Stop();
        else Tick?.Invoke();
    }

    public void Stop()
    {
        _endsAt = null;
        _timer?.Dispose();
        _timer = null;
        Tick?.Invoke();
    }

    /// <summary>Advances state; called by the internal timer and directly by tests.</summary>
    public void Check()
    {
        if (!IsRunning) return;
        if (Remaining <= TimeSpan.Zero)
        {
            Stop();
            Finished?.Invoke();
        }
        else Tick?.Invoke();
    }

    public void Dispose() => _timer?.Dispose();
}
