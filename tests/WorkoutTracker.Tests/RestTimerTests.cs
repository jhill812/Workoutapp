using WorkoutTracker.Services;

namespace WorkoutTracker.Tests;

public class RestTimerTests
{
    private readonly FakeTime _time = new(DateTimeOffset.UnixEpoch);

    [Fact]
    public void CountsDownAndFiresFinishedOnce()
    {
        using var timer = new RestTimerService(_time);
        var finished = 0;
        timer.Finished += () => finished++;

        timer.Start(90);
        Assert.True(timer.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(90), timer.Remaining);

        _time.Advance(TimeSpan.FromSeconds(30));
        timer.Check();
        Assert.Equal(TimeSpan.FromSeconds(60), timer.Remaining);
        Assert.Equal(1 / 3.0, timer.Progress, 3);

        _time.Advance(TimeSpan.FromSeconds(61));
        timer.Check();
        timer.Check();
        Assert.False(timer.IsRunning);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void AdjustAddsAndRemovesTime()
    {
        using var timer = new RestTimerService(_time);
        timer.Start(60);
        timer.Adjust(15);
        Assert.Equal(TimeSpan.FromSeconds(75), timer.Remaining);
        timer.Adjust(-80);
        Assert.False(timer.IsRunning);
    }

    [Fact]
    public void StopDoesNotRaiseFinished()
    {
        using var timer = new RestTimerService(_time);
        var finished = false;
        timer.Finished += () => finished = true;
        timer.Start(60);
        timer.Stop();
        Assert.False(timer.IsRunning);
        Assert.False(finished);
    }

    [Fact]
    public void StartWithZeroDoesNothing()
    {
        using var timer = new RestTimerService(_time);
        timer.Start(0);
        Assert.False(timer.IsRunning);
    }
}
