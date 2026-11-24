using WorkoutTracker.Models;

namespace WorkoutTracker.Tests;

public class UnitsTests
{
    [Theory]
    [InlineData(135)]
    [InlineData(225)]
    [InlineData(2.5)]
    public void PoundsRoundTripThroughKilograms(double lb)
    {
        var kg = Units.FromDisplayWeight(lb, WeightUnit.Lb);
        Assert.Equal(lb, Units.ToDisplayWeight(kg, WeightUnit.Lb));
    }

    [Fact]
    public void KilogramsAreStoredAsIs() => Assert.Equal(62.5, Units.FromDisplayWeight(62.5, WeightUnit.Kg));

    [Fact]
    public void MilesConvert() => Assert.Equal(1, Units.ToDisplayDistance(1.609344, WeightUnit.Lb));

    [Theory]
    [InlineData(500, "500 kg")]
    [InlineData(12_345, "12.3k kg")]
    [InlineData(0, "0 kg")]
    public void VolumeIsCompact(double kg, string expected) => Assert.Equal(expected, Fmt.Volume(kg, WeightUnit.Kg));

    [Fact]
    public void FormatsSetsByType()
    {
        Assert.Equal("60 × 8", Fmt.Set(new SetEntry { Weight = 60, Reps = 8 }, ExerciseType.Strength, WeightUnit.Kg));
        Assert.Equal("12 reps", Fmt.Set(new SetEntry { Reps = 12 }, ExerciseType.Bodyweight, WeightUnit.Kg));
        Assert.Equal("+10 × 6", Fmt.Set(new SetEntry { Weight = 10, Reps = 6 }, ExerciseType.Bodyweight, WeightUnit.Kg));
        Assert.Equal("30:00 · 5 km", Fmt.Set(new SetEntry { DurationSeconds = 1800, DistanceKm = 5 }, ExerciseType.Cardio, WeightUnit.Kg));
    }

    [Fact]
    public void DurationAndClock()
    {
        Assert.Equal("45m", Fmt.Duration(TimeSpan.FromMinutes(45)));
        Assert.Equal("1h 05m", Fmt.Duration(TimeSpan.FromMinutes(65)));
        Assert.Equal("1:30", Fmt.Clock(TimeSpan.FromSeconds(90)));
        Assert.Equal("1:00:05", Fmt.Clock(TimeSpan.FromSeconds(3605)));
    }

    [Fact]
    public void RelativeDay()
    {
        var now = new DateTime(2026, 11, 24, 12, 0, 0);
        Assert.Equal("Today", Fmt.RelativeDay(now.AddHours(-2), now));
        Assert.Equal("Yesterday", Fmt.RelativeDay(now.AddDays(-1), now));
        Assert.Equal("Saturday", Fmt.RelativeDay(now.AddDays(-3), now));
        Assert.Equal("Tue, Nov 3", Fmt.RelativeDay(new DateTime(2026, 11, 3), now));
        Assert.Equal("Nov 3, 2025", Fmt.RelativeDay(new DateTime(2025, 11, 3), now));
    }
}
