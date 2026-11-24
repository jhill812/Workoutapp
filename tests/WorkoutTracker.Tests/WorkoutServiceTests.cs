using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.Tests;

public class WorkoutServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly FakeTime _time = new(new DateTimeOffset(2026, 11, 24, 18, 30, 0, TimeSpan.Zero));

    private async Task<WorkoutService> CreateAsync()
    {
        var svc = new WorkoutService(_store, _time);
        await svc.EnsureLoadedAsync();
        return svc;
    }

    private static Exercise Bench => ExerciseLibrary.BuiltIn.First(e => e.Id == "bench-press");

    [Fact]
    public void BuiltInExerciseIdsAreUnique() =>
        Assert.Equal(ExerciseLibrary.BuiltIn.Count, ExerciseLibrary.BuiltIn.Select(e => e.Id).Distinct().Count());

    [Fact]
    public async Task StartWorkout_CreatesSingleActiveWorkoutNamedForTimeOfDay()
    {
        var svc = await CreateAsync();
        var w = await svc.StartWorkoutAsync();
        Assert.Equal("Evening Workout", w.Name);
        Assert.Same(w, svc.ActiveWorkout);
        Assert.Same(w, await svc.StartWorkoutAsync()); // never two active workouts
        Assert.Empty(svc.History);
    }

    [Fact]
    public async Task Data_PersistsAcrossInstances()
    {
        var svc = await CreateAsync();
        var w = await svc.StartWorkoutAsync();
        await svc.AddExerciseAsync(w, Bench);
        w.Exercises[0].Sets[0] = new SetEntry { Weight = 80, Reps = 5, Completed = true };
        await svc.FinishWorkoutAsync(w);
        await svc.UpdateSettingsAsync(s => s.Unit = WeightUnit.Lb);

        var reloaded = await CreateAsync();
        var saved = Assert.Single(reloaded.History);
        Assert.Equal(80, saved.Exercises[0].Sets[0].Weight);
        Assert.Equal(WeightUnit.Lb, reloaded.Settings.Unit);
        Assert.Null(reloaded.ActiveWorkout);
    }

    [Fact]
    public async Task Finish_DropsIncompleteSetsAndEmptyExercises()
    {
        var svc = await CreateAsync();
        var w = await svc.StartWorkoutAsync();
        var bench = await svc.AddExerciseAsync(w, Bench);
        bench.Sets = [new() { Weight = 80, Reps = 5, Completed = true }, new() { Weight = 80, Reps = 5 }];
        await svc.AddExerciseAsync(w, ExerciseLibrary.BuiltIn.First(e => e.Id == "squat")); // never completed

        _time.Advance(TimeSpan.FromMinutes(50));
        await svc.FinishWorkoutAsync(w);

        var ex = Assert.Single(w.Exercises);
        Assert.Single(ex.Sets);
        Assert.Equal(TimeSpan.FromMinutes(50), w.FinishedAt - w.StartedAt);
    }

    [Fact]
    public async Task AddExercise_PrefillsFromPreviousSession()
    {
        var svc = await CreateAsync();
        var first = await svc.StartWorkoutAsync();
        var bench = await svc.AddExerciseAsync(first, Bench);
        bench.Sets = [new() { Weight = 82.5, Reps = 6, Completed = true }];
        await svc.FinishWorkoutAsync(first);

        var second = await svc.StartWorkoutAsync();
        var entry = await svc.AddExerciseAsync(second, Bench);
        Assert.Equal(82.5, entry.Sets[0].Weight);
        Assert.False(entry.Sets[0].Completed);
        Assert.Equal(82.5, svc.GetPreviousSets(Bench.Id, second.Id)![0].Weight);
    }

    [Fact]
    public async Task StartFromTemplate_CopiesExercisesWithFreshSets()
    {
        var svc = await CreateAsync();
        var first = await svc.StartWorkoutAsync();
        first.Name = "Push Day";
        var bench = await svc.AddExerciseAsync(first, Bench);
        bench.Sets = [new() { Weight = 80, Reps = 5, Completed = true }, new() { Weight = 80, Reps = 5, Completed = true }];
        await svc.FinishWorkoutAsync(first);

        var template = Assert.Single(svc.RecentTemplates());
        var repeat = await svc.StartWorkoutAsync(template);

        Assert.Equal("Push Day", repeat.Name);
        Assert.NotSame(first.Exercises[0], repeat.Exercises[0]);
        Assert.Equal(2, repeat.Exercises[0].Sets.Count);
        Assert.All(repeat.Exercises[0].Sets, s => Assert.False(s.Completed));
        Assert.All(first.Exercises[0].Sets, s => Assert.True(s.Completed)); // original untouched
    }

    [Fact]
    public async Task CustomExercises_ValidateAndPersist()
    {
        var svc = await CreateAsync();
        var e = await svc.AddCustomExerciseAsync("  Landmine Press ", MuscleGroup.Shoulders, ExerciseType.Strength);
        Assert.Equal("Landmine Press", e.Name);
        Assert.True(e.IsCustom);
        Assert.Contains(svc.Exercises, x => x.Id == e.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.AddCustomExerciseAsync("bench press", MuscleGroup.Chest, ExerciseType.Strength));
        await Assert.ThrowsAsync<ArgumentException>(() => svc.AddCustomExerciseAsync("   ", MuscleGroup.Chest, ExerciseType.Strength));

        Assert.NotNull((await CreateAsync()).FindExercise(e.Id));
        await svc.DeleteCustomExerciseAsync(e.Id);
        Assert.Null(svc.FindExercise(e.Id));
    }

    [Fact]
    public async Task ExportImport_RoundTrips()
    {
        var svc = await CreateAsync();
        var w = await svc.StartWorkoutAsync();
        var bench = await svc.AddExerciseAsync(w, Bench);
        bench.Sets[0].Completed = true;
        await svc.FinishWorkoutAsync(w);
        await svc.AddCustomExerciseAsync("Zercher Squat", MuscleGroup.Legs, ExerciseType.Strength);
        var json = svc.ExportJson();
        Assert.Contains("\"strength\"", json, StringComparison.OrdinalIgnoreCase); // enums readable as strings

        await svc.ClearAllAsync();
        Assert.Empty(svc.History);

        var (ok, message) = await svc.ImportJsonAsync(json);
        Assert.True(ok, message);
        Assert.Single(svc.History);
        Assert.Contains(svc.Exercises, e => e.Name == "Zercher Squat");
    }

    [Fact]
    public async Task Import_InvalidFile_KeepsExistingData()
    {
        var svc = await CreateAsync();
        await svc.UpdateSettingsAsync(s => s.WeeklyGoal = 5);
        var (ok, _) = await svc.ImportJsonAsync("{ not json");
        Assert.False(ok);
        Assert.Equal(5, svc.Settings.WeeklyGoal);
    }

    [Fact]
    public async Task CorruptStorage_StartsFreshButKeepsBackupCopy()
    {
        _store.Items[WorkoutService.StorageKey] = "garbage{";
        var svc = await CreateAsync();
        Assert.Empty(svc.History);
        Assert.Equal("garbage{", _store.Items[WorkoutService.StorageKey + ":corrupt"]);
    }

    [Fact]
    public async Task Settings_AreClamped()
    {
        var svc = await CreateAsync();
        await svc.UpdateSettingsAsync(s => { s.WeeklyGoal = 0; s.DefaultRestSeconds = -5; });
        Assert.Equal(1, svc.Settings.WeeklyGoal);
        Assert.Equal(0, svc.Settings.DefaultRestSeconds);
    }

    [Fact]
    public async Task ChangedEvent_FiresOnSave()
    {
        var svc = await CreateAsync();
        var count = 0;
        svc.Changed += () => count++;
        await svc.StartWorkoutAsync();
        await svc.SaveAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task DeleteWorkout_Removes()
    {
        var svc = await CreateAsync();
        var w = await svc.StartWorkoutAsync();
        await svc.DeleteWorkoutAsync(w.Id);
        Assert.Null(svc.ActiveWorkout);
        Assert.Null(svc.GetWorkout(w.Id));
    }
}
