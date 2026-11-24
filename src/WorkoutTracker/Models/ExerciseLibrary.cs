namespace WorkoutTracker.Models;

/// <summary>Built-in exercises. Ids are stable slugs; never change an existing id.</summary>
public static class ExerciseLibrary
{
    public static IReadOnlyList<Exercise> BuiltIn { get; } =
    [
        E("bench-press", "Bench Press", MuscleGroup.Chest),
        E("incline-bench-press", "Incline Bench Press", MuscleGroup.Chest),
        E("dumbbell-bench-press", "Dumbbell Bench Press", MuscleGroup.Chest),
        E("incline-dumbbell-press", "Incline Dumbbell Press", MuscleGroup.Chest),
        E("chest-fly", "Chest Fly", MuscleGroup.Chest),
        E("cable-crossover", "Cable Crossover", MuscleGroup.Chest),
        E("push-up", "Push-Up", MuscleGroup.Chest, ExerciseType.Bodyweight),
        E("dip", "Dip", MuscleGroup.Triceps, ExerciseType.Bodyweight),

        E("deadlift", "Deadlift", MuscleGroup.Back),
        E("barbell-row", "Barbell Row", MuscleGroup.Back),
        E("dumbbell-row", "Dumbbell Row", MuscleGroup.Back),
        E("lat-pulldown", "Lat Pulldown", MuscleGroup.Back),
        E("seated-cable-row", "Seated Cable Row", MuscleGroup.Back),
        E("pull-up", "Pull-Up", MuscleGroup.Back, ExerciseType.Bodyweight),
        E("chin-up", "Chin-Up", MuscleGroup.Back, ExerciseType.Bodyweight),
        E("face-pull", "Face Pull", MuscleGroup.Shoulders),

        E("overhead-press", "Overhead Press", MuscleGroup.Shoulders),
        E("dumbbell-shoulder-press", "Dumbbell Shoulder Press", MuscleGroup.Shoulders),
        E("lateral-raise", "Lateral Raise", MuscleGroup.Shoulders),
        E("rear-delt-fly", "Rear Delt Fly", MuscleGroup.Shoulders),
        E("shrug", "Shrug", MuscleGroup.Back),

        E("barbell-curl", "Barbell Curl", MuscleGroup.Biceps),
        E("dumbbell-curl", "Dumbbell Curl", MuscleGroup.Biceps),
        E("hammer-curl", "Hammer Curl", MuscleGroup.Biceps),
        E("preacher-curl", "Preacher Curl", MuscleGroup.Biceps),
        E("triceps-pushdown", "Triceps Pushdown", MuscleGroup.Triceps),
        E("skull-crusher", "Skull Crusher", MuscleGroup.Triceps),
        E("overhead-triceps-extension", "Overhead Triceps Extension", MuscleGroup.Triceps),
        E("close-grip-bench-press", "Close-Grip Bench Press", MuscleGroup.Triceps),

        E("squat", "Squat", MuscleGroup.Legs),
        E("front-squat", "Front Squat", MuscleGroup.Legs),
        E("leg-press", "Leg Press", MuscleGroup.Legs),
        E("romanian-deadlift", "Romanian Deadlift", MuscleGroup.Legs),
        E("lunge", "Lunge", MuscleGroup.Legs),
        E("bulgarian-split-squat", "Bulgarian Split Squat", MuscleGroup.Legs),
        E("leg-extension", "Leg Extension", MuscleGroup.Legs),
        E("leg-curl", "Leg Curl", MuscleGroup.Legs),
        E("calf-raise", "Calf Raise", MuscleGroup.Legs),
        E("hip-thrust", "Hip Thrust", MuscleGroup.Glutes),
        E("glute-bridge", "Glute Bridge", MuscleGroup.Glutes, ExerciseType.Bodyweight),

        E("plank", "Plank", MuscleGroup.Core, ExerciseType.Cardio),
        E("crunch", "Crunch", MuscleGroup.Core, ExerciseType.Bodyweight),
        E("hanging-leg-raise", "Hanging Leg Raise", MuscleGroup.Core, ExerciseType.Bodyweight),
        E("cable-crunch", "Cable Crunch", MuscleGroup.Core),
        E("russian-twist", "Russian Twist", MuscleGroup.Core, ExerciseType.Bodyweight),

        E("running", "Running", MuscleGroup.Cardio, ExerciseType.Cardio),
        E("cycling", "Cycling", MuscleGroup.Cardio, ExerciseType.Cardio),
        E("rowing", "Rowing", MuscleGroup.Cardio, ExerciseType.Cardio),
        E("elliptical", "Elliptical", MuscleGroup.Cardio, ExerciseType.Cardio),
        E("stair-climber", "Stair Climber", MuscleGroup.Cardio, ExerciseType.Cardio),
        E("jump-rope", "Jump Rope", MuscleGroup.Cardio, ExerciseType.Cardio),
        E("kettlebell-swing", "Kettlebell Swing", MuscleGroup.FullBody),
        E("burpee", "Burpee", MuscleGroup.FullBody, ExerciseType.Bodyweight),
    ];

    private static Exercise E(string id, string name, MuscleGroup muscle, ExerciseType type = ExerciseType.Strength)
        => new() { Id = id, Name = name, Muscle = muscle, Type = type };
}
