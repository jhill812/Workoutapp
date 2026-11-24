namespace WorkoutTracker.Models;

public enum MuscleGroup { Chest, Back, Shoulders, Biceps, Triceps, Legs, Glutes, Core, Cardio, FullBody }

/// <summary>How sets of an exercise are measured.</summary>
public enum ExerciseType
{
    /// <summary>Weight × reps (barbell, dumbbell, machine).</summary>
    Strength,
    /// <summary>Reps with optional added weight (pull-ups, dips, push-ups).</summary>
    Bodyweight,
    /// <summary>Duration and distance (running, rowing, cycling).</summary>
    Cardio
}

public enum WeightUnit { Kg, Lb }

public enum ThemePreference { System, Dark, Light }

public sealed class Exercise
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public MuscleGroup Muscle { get; set; }
    public ExerciseType Type { get; set; }
    public bool IsCustom { get; set; }
}

public sealed class SetEntry
{
    /// <summary>Weight in kilograms (added weight for bodyweight exercises).</summary>
    public double Weight { get; set; }
    public int Reps { get; set; }
    public int DurationSeconds { get; set; }
    /// <summary>Distance in kilometres.</summary>
    public double DistanceKm { get; set; }
    public bool Completed { get; set; }

    public SetEntry Clone(bool resetCompleted = true) => new()
    {
        Weight = Weight,
        Reps = Reps,
        DurationSeconds = DurationSeconds,
        DistanceKm = DistanceKm,
        Completed = !resetCompleted && Completed
    };
}

public sealed class WorkoutExercise
{
    public string ExerciseId { get; set; } = "";
    // Snapshot of exercise details so history survives deleting a custom exercise.
    public string Name { get; set; } = "";
    public MuscleGroup Muscle { get; set; }
    public ExerciseType Type { get; set; }
    public List<SetEntry> Sets { get; set; } = [];
    public string? Notes { get; set; }

    public static WorkoutExercise From(Exercise e) => new()
    {
        ExerciseId = e.Id,
        Name = e.Name,
        Muscle = e.Muscle,
        Type = e.Type
    };
}

public sealed class Workout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<WorkoutExercise> Exercises { get; set; } = [];
    public string? Notes { get; set; }

    public bool IsActive => FinishedAt is null;
}

public sealed class UserSettings
{
    public WeightUnit Unit { get; set; } = WeightUnit.Kg;
    public int DefaultRestSeconds { get; set; } = 90;
    public int WeeklyGoal { get; set; } = 3;
    public ThemePreference Theme { get; set; } = ThemePreference.System;
}

public sealed class AppData
{
    public int Version { get; set; } = 1;
    public List<Workout> Workouts { get; set; } = [];
    public List<Exercise> CustomExercises { get; set; } = [];
    public UserSettings Settings { get; set; } = new();
}
