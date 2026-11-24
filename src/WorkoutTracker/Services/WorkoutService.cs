using System.Text.Json;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>Owns all app data: workouts, custom exercises and settings. Persists on every change.</summary>
public sealed class WorkoutService(IKeyValueStore store, TimeProvider time)
{
    public const string StorageKey = "workout-tracker:data";
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private AppData _data = new();
    private bool _loaded;

    /// <summary>Raised after any persisted change.</summary>
    public event Action? Changed;

    public DateTime Now => time.GetLocalNow().DateTime;
    public UserSettings Settings => _data.Settings;
    public WeightUnit Unit => _data.Settings.Unit;

    /// <summary>Finished workouts, newest first.</summary>
    public IReadOnlyList<Workout> History =>
        _data.Workouts.Where(w => !w.IsActive).OrderByDescending(w => w.StartedAt).ToList();

    public Workout? ActiveWorkout => _data.Workouts.FirstOrDefault(w => w.IsActive);

    public IReadOnlyList<Exercise> Exercises =>
        ExerciseLibrary.BuiltIn.Concat(_data.CustomExercises)
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await _loadLock.WaitAsync();
        try
        {
            if (_loaded) return;
            var json = await store.GetAsync(StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    _data = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppData) ?? new AppData();
                }
                catch (JsonException)
                {
                    // Never silently destroy user data: keep a copy of the unreadable payload.
                    await store.SetAsync(StorageKey + ":corrupt", json);
                    _data = new AppData();
                }
            }
            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public Workout? GetWorkout(Guid id) => _data.Workouts.FirstOrDefault(w => w.Id == id);

    public Exercise? FindExercise(string id) =>
        ExerciseLibrary.BuiltIn.FirstOrDefault(e => e.Id == id) ?? _data.CustomExercises.FirstOrDefault(e => e.Id == id);

    /// <summary>Starts a workout (optionally copying a previous one). Returns the existing one if already active.</summary>
    public async Task<Workout> StartWorkoutAsync(Workout? template = null)
    {
        if (ActiveWorkout is { } active) return active;

        var now = Now;
        var workout = new Workout
        {
            Name = string.IsNullOrWhiteSpace(template?.Name) ? DefaultName(now) : template.Name,
            StartedAt = now,
            Exercises = template?.Exercises.Select(e => new WorkoutExercise
            {
                ExerciseId = e.ExerciseId,
                Name = e.Name,
                Muscle = e.Muscle,
                Type = e.Type,
                Sets = e.Sets.Select(s => s.Clone()).ToList()
            }).ToList() ?? []
        };
        _data.Workouts.Add(workout);
        await PersistAsync();
        return workout;
    }

    public static string DefaultName(DateTime at) => at.Hour switch
    {
        < 12 => "Morning Workout",
        < 17 => "Afternoon Workout",
        _ => "Evening Workout"
    };

    /// <summary>Adds an exercise to a workout, pre-filling one set from the last time it was done.</summary>
    public async Task<WorkoutExercise> AddExerciseAsync(Workout workout, Exercise exercise)
    {
        var entry = WorkoutExercise.From(exercise);
        var previous = GetPreviousSets(exercise.Id, workout.Id);
        entry.Sets.Add(previous is { Count: > 0 } ? previous[0].Clone() : new SetEntry());
        workout.Exercises.Add(entry);
        await PersistAsync();
        return entry;
    }

    /// <summary>Persist in-place edits made to a workout.</summary>
    public Task SaveAsync() => PersistAsync();

    /// <summary>Finishes a workout, dropping unfinished sets and exercises left empty.</summary>
    public async Task FinishWorkoutAsync(Workout workout)
    {
        foreach (var ex in workout.Exercises) ex.Sets.RemoveAll(s => !s.Completed);
        workout.Exercises.RemoveAll(e => e.Sets.Count == 0);
        workout.FinishedAt = Now;
        await PersistAsync();
    }

    public async Task DeleteWorkoutAsync(Guid id)
    {
        if (_data.Workouts.RemoveAll(w => w.Id == id) > 0) await PersistAsync();
    }

    /// <summary>Most recent completed sets of an exercise in another finished workout.</summary>
    public IReadOnlyList<SetEntry>? GetPreviousSets(string exerciseId, Guid excludeWorkoutId) =>
        _data.Workouts
            .Where(w => !w.IsActive && w.Id != excludeWorkoutId)
            .OrderByDescending(w => w.StartedAt)
            .SelectMany(w => w.Exercises)
            .FirstOrDefault(e => e.ExerciseId == exerciseId && e.Sets.Count > 0)
            ?.Sets;

    /// <summary>Recent distinct workouts usable as templates for "repeat workout".</summary>
    public IReadOnlyList<Workout> RecentTemplates(int count = 5) =>
        History.Where(w => w.Exercises.Count > 0)
            .DistinctBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Take(count).ToList();

    public async Task<Exercise> AddCustomExerciseAsync(string name, MuscleGroup muscle, ExerciseType type)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Exercise name is required.", nameof(name));
        if (Exercises.Any(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"An exercise named \"{name}\" already exists.", nameof(name));

        var exercise = new Exercise
        {
            Id = "custom-" + Guid.NewGuid().ToString("N")[..12],
            Name = name,
            Muscle = muscle,
            Type = type,
            IsCustom = true
        };
        _data.CustomExercises.Add(exercise);
        await PersistAsync();
        return exercise;
    }

    public async Task DeleteCustomExerciseAsync(string id)
    {
        if (_data.CustomExercises.RemoveAll(e => e.Id == id) > 0) await PersistAsync();
    }

    public async Task UpdateSettingsAsync(Action<UserSettings> update)
    {
        update(_data.Settings);
        _data.Settings.WeeklyGoal = Math.Clamp(_data.Settings.WeeklyGoal, 1, 14);
        _data.Settings.DefaultRestSeconds = Math.Clamp(_data.Settings.DefaultRestSeconds, 0, 3600);
        await PersistAsync();
    }

    public string ExportJson() =>
        JsonSerializer.Serialize(_data, new AppJsonContext(new JsonSerializerOptions(AppJsonContext.Default.Options) { WriteIndented = true }).AppData);

    /// <summary>Replaces all data with an exported backup. Existing data is untouched if the file is invalid.</summary>
    public async Task<(bool Ok, string Message)> ImportJsonAsync(string json)
    {
        AppData? imported;
        try
        {
            imported = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppData);
        }
        catch (JsonException)
        {
            return (false, "That file isn't a valid Workout Tracker backup.");
        }
        if (imported is null) return (false, "The backup file is empty.");

        imported.Workouts ??= [];
        imported.CustomExercises ??= [];
        imported.Settings ??= new UserSettings();
        _data = imported;
        await PersistAsync();
        return (true, $"Imported {imported.Workouts.Count} workouts and {imported.CustomExercises.Count} custom exercises.");
    }

    public async Task ClearAllAsync()
    {
        _data = new AppData();
        await store.RemoveAsync(StorageKey);
        Changed?.Invoke();
    }

    private async Task PersistAsync()
    {
        await store.SetAsync(StorageKey, JsonSerializer.Serialize(_data, AppJsonContext.Default.AppData));
        Changed?.Invoke();
    }
}
