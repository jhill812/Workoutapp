using WorkoutTracker.Models;
using WorkoutTracker.Services;
using static WorkoutTracker.Tests.Build;

namespace WorkoutTracker.Tests;

public class StatsTests
{
    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 10, 133.333)]
    [InlineData(60, 5, 70)]
    [InlineData(0, 5, 0)]
    [InlineData(100, 0, 0)]
    public void EstimatedOneRepMax_UsesEpley(double weight, int reps, double expected) =>
        Assert.Equal(expected, Stats.EstimatedOneRepMax(weight, reps), 2);

    [Fact]
    public void Volume_CountsOnlyCompletedNonCardioSets()
    {
        var bench = Lift("bench", (100, 5), (100, 5));
        bench.Sets.Add(new SetEntry { Weight = 100, Reps = 5, Completed = false });
        var run = new WorkoutExercise { Type = ExerciseType.Cardio, Sets = [new() { Weight = 5, Reps = 5, Completed = true }] };

        Assert.Equal(1000, Stats.Volume(Workout(Day(2026, 11, 2), bench, run)));
        Assert.Equal(3, Stats.CompletedSets(Workout(Day(2026, 11, 2), bench, run)));
    }

    [Theory]
    [InlineData(2026, 11, 23, 2026, 11, 23)] // Monday
    [InlineData(2026, 11, 29, 2026, 11, 23)] // Sunday belongs to the week that started Monday
    [InlineData(2026, 11, 25, 2026, 11, 23)]
    public void StartOfWeek_IsMonday(int y, int m, int d, int ey, int em, int ed) =>
        Assert.Equal(new DateOnly(ey, em, ed), Stats.StartOfWeek(new DateOnly(y, m, d)));

    [Fact]
    public void WeekStreak_CountsConsecutiveWeeks_AndToleratesEmptyCurrentWeek()
    {
        var today = Day(2026, 11, 24); // Tuesday, nothing yet this week
        var workouts = new[]
        {
            Workout(Day(2026, 11, 17)), Workout(Day(2026, 11, 19)), // last week
            Workout(Day(2026, 11, 10)),                             // 2 weeks ago
            Workout(Day(2026, 10, 20)),                             // gap before this
        };
        Assert.Equal(2, Stats.WeekStreak(workouts, today));
        Assert.Equal(3, Stats.WeekStreak(workouts.Append(Workout(Day(2026, 11, 23))), today));
    }

    [Fact]
    public void WeekStreak_IsZeroWhenLastWeekMissed() =>
        Assert.Equal(0, Stats.WeekStreak([Workout(Day(2026, 11, 3))], Day(2026, 11, 24)));

    [Fact]
    public void WeekStreak_IgnoresActiveWorkouts()
    {
        var active = Workout(Day(2026, 11, 24));
        active.FinishedAt = null;
        Assert.Equal(0, Stats.WeekStreak([active], Day(2026, 11, 24)));
    }

    [Fact]
    public void Weekly_ReturnsOldestFirstIncludingEmptyWeeks()
    {
        var weeks = Stats.Weekly([Workout(Day(2026, 11, 24), Lift("bench", (50, 10)))], Day(2026, 11, 25), 4);
        Assert.Equal(4, weeks.Count);
        Assert.Equal(new DateOnly(2026, 11, 2), weeks[0].WeekStart);
        Assert.Equal([0, 0, 0, 1], weeks.Select(w => w.Workouts));
        Assert.Equal(500, weeks[^1].Volume);
    }

    [Fact]
    public void ExerciseHistory_AndRecord_TrackBestPerformance()
    {
        var workouts = new[]
        {
            Workout(Day(2026, 11, 1), Lift("bench", (80, 8), (85, 5))),
            Workout(Day(2026, 11, 8), Lift("bench", (90, 3)), Lift("squat", (120, 5))),
            Workout(Day(2026, 11, 15), Lift("bench", (85, 8))),
        };

        var history = Stats.ExerciseHistory(workouts, "bench");
        Assert.Equal(3, history.Count);
        Assert.Equal(85, history[0].BestWeight);
        Assert.Equal(80 * 8 + 85 * 5, history[0].Volume);

        var pr = Stats.GetRecord(workouts, "bench")!;
        Assert.Equal(90, pr.HeaviestWeight);
        Assert.Equal(Stats.EstimatedOneRepMax(85, 8), pr.BestOneRepMax, 3); // 85x8 beats 90x3 on e1RM
        Assert.Equal(Day(2026, 11, 15), pr.AchievedAt);
        Assert.Equal(3, pr.Sessions);
        Assert.Null(Stats.GetRecord(workouts, "deadlift"));
    }

    [Fact]
    public void AllRecords_OrdersByMostTrained()
    {
        var workouts = new[]
        {
            Workout(Day(2026, 11, 1), Lift("bench", (80, 8)), Lift("squat", (100, 5))),
            Workout(Day(2026, 11, 8), Lift("squat", (105, 5))),
        };
        Assert.Equal(["squat", "bench"], Stats.AllRecords(workouts).Select(r => r.ExerciseId));
    }

    [Fact]
    public void MuscleSplit_SumsCompletedSetsSinceDate()
    {
        var legs = Lift("squat", (100, 5), (100, 5));
        legs.Muscle = MuscleGroup.Legs;
        var workouts = new[]
        {
            Workout(Day(2026, 11, 20), legs, Lift("bench", (80, 8))),
            Workout(Day(2026, 9, 1), Lift("bench", (80, 8), (80, 8), (80, 8))), // too old
        };
        var split = Stats.MuscleSplit(workouts, Day(2026, 11, 1, 0));
        Assert.Equal([(MuscleGroup.Legs, 2), (MuscleGroup.Chest, 1)], split);
    }
}
