using WorkoutTracker.Models;

namespace WorkoutTracker.Tests;

public sealed class FakeTime(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Current { get; set; } = start;
    public override DateTimeOffset GetUtcNow() => Current;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public void Advance(TimeSpan by) => Current += by;
}

public static class Build
{
    public static DateTime Day(int y, int m, int d, int h = 18) => new(y, m, d, h, 0, 0);

    public static Workout Workout(DateTime start, params WorkoutExercise[] exercises) => new()
    {
        Name = "Test",
        StartedAt = start,
        FinishedAt = start.AddMinutes(60),
        Exercises = exercises.ToList()
    };

    public static WorkoutExercise Lift(string id, params (double kg, int reps)[] sets) => new()
    {
        ExerciseId = id,
        Name = id,
        Type = ExerciseType.Strength,
        Muscle = MuscleGroup.Chest,
        Sets = sets.Select(s => new SetEntry { Weight = s.kg, Reps = s.reps, Completed = true }).ToList()
    };
}
