using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTracker.Components;
using WorkoutTracker.Models;
using WorkoutTracker.Pages;
using WorkoutTracker.Services;

namespace WorkoutTracker.Tests;

public class ComponentTests : BunitContext
{
    private readonly WorkoutService _data;
    private readonly FakeTime _time = new(new DateTimeOffset(2026, 11, 24, 9, 0, 0, TimeSpan.Zero));

    public ComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _data = new WorkoutService(new InMemoryStore(), _time);
        _data.EnsureLoadedAsync().GetAwaiter().GetResult();
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton(_data);
        Services.AddSingleton<RestTimerService>();
        Services.AddSingleton<DeviceService>();
    }

    [Fact]
    public void Home_EmptyState_StartsWorkoutAndNavigates()
    {
        var cut = Render<Home>();
        Assert.Contains("Good morning", cut.Markup);
        Assert.Contains("No workouts yet", cut.Markup);

        cut.Find("button.btn-primary").Click();

        var active = Assert.IsType<Workout>(_data.ActiveWorkout);
        Assert.EndsWith($"workout/{active.Id}", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void ExerciseCard_CompletingEmptySet_CopiesPreviousAndRaisesEvent()
    {
        var entry = WorkoutExercise.From(ExerciseLibrary.BuiltIn[0]);
        entry.Sets.Add(new SetEntry());
        var completed = 0;
        var cut = Render<ExerciseCard>(p => p
            .Add(x => x.Entry, entry)
            .Add(x => x.Previous, [new SetEntry { Weight = 70, Reps = 8, Completed = true }])
            .Add(x => x.OnSetCompleted, () => completed++));

        Assert.Contains("70 × 8", cut.Find(".prev").TextContent);
        cut.Find(".check-btn").Click();

        Assert.True(entry.Sets[0].Completed);
        Assert.Equal(70, entry.Sets[0].Weight);
        Assert.Equal(8, entry.Sets[0].Reps);
        Assert.Equal(1, completed);
        Assert.Contains("done", cut.Find(".set-row").ClassName);
    }

    [Fact]
    public void ExerciseCard_EditsWeightInPounds_StoresKilograms()
    {
        var entry = WorkoutExercise.From(ExerciseLibrary.BuiltIn[0]);
        entry.Sets.Add(new SetEntry());
        var cut = Render<ExerciseCard>(p => p.Add(x => x.Entry, entry).Add(x => x.Unit, WeightUnit.Lb));

        cut.Find("input[aria-label='Weight, set 1']").Change("225");
        cut.Find("input[aria-label='Reps, set 1']").Change("5");

        Assert.Equal(225 * Units.KgPerLb, entry.Sets[0].Weight, 6);
        Assert.Equal(5, entry.Sets[0].Reps);
        Assert.Contains("lb", cut.Find(".set-head").TextContent);
    }

    [Fact]
    public void ExerciseCard_AddSetCopiesLastSet()
    {
        var entry = WorkoutExercise.From(ExerciseLibrary.BuiltIn[0]);
        entry.Sets.Add(new SetEntry { Weight = 60, Reps = 10, Completed = true });
        var cut = Render<ExerciseCard>(p => p.Add(x => x.Entry, entry));

        cut.FindAll("button").First(b => b.TextContent.Contains("Add set")).Click();

        Assert.Equal(2, entry.Sets.Count);
        Assert.Equal(60, entry.Sets[1].Weight);
        Assert.False(entry.Sets[1].Completed);
        Assert.Equal(2, cut.FindAll(".set-row").Count);
    }

    [Fact]
    public async Task WorkoutPage_FinishFlow_SavesToHistory()
    {
        var w = await _data.StartWorkoutAsync();
        var entry = await _data.AddExerciseAsync(w, ExerciseLibrary.BuiltIn[0]);
        entry.Sets[0] = new SetEntry { Weight = 50, Reps = 10 };

        var cut = Render<WorkoutPage>(p => p.Add(x => x.Id, w.Id));
        cut.Find(".check-btn").Click();
        Assert.True(Services.GetRequiredService<RestTimerService>().IsRunning);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Finish").Click();
        cut.Find(".dialog .btn-primary").Click();

        Assert.Null(_data.ActiveWorkout);
        Assert.Single(_data.History);
        Assert.False(Services.GetRequiredService<RestTimerService>().IsRunning);
    }

    [Fact]
    public void ExercisePicker_FiltersAndPicksMultiple()
    {
        IReadOnlyList<Exercise>? picked = null;
        var cut = Render<ExercisePicker>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OnPick, (IReadOnlyList<Exercise> list) => picked = list));

        cut.Find("input[type=search]").Input("curl");
        var rows = cut.FindAll(".pick-list .list-row");
        Assert.All(rows, r => Assert.Contains("Curl", r.TextContent));

        rows[0].Click();
        cut.FindAll(".pick-list .list-row")[1].Click();
        cut.Find(".sheet-footer button").Click();

        Assert.NotNull(picked);
        Assert.Equal(2, picked!.Count);
    }

    [Fact]
    public async Task Settings_ChangesUnit()
    {
        var cut = Render<Settings>();
        cut.FindAll(".segmented button").First(b => b.TextContent.Contains("lb")).Click();
        Assert.Equal(WeightUnit.Lb, _data.Unit);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Progress_ShowsPersonalRecords()
    {
        var w = await _data.StartWorkoutAsync();
        var entry = await _data.AddExerciseAsync(w, ExerciseLibrary.BuiltIn.First(e => e.Id == "squat"));
        entry.Sets[0] = new SetEntry { Weight = 100, Reps = 5, Completed = true };
        await _data.FinishWorkoutAsync(w);

        var cut = Render<Progress>();
        Assert.Contains("Squat", cut.Find(".list").TextContent);
        Assert.Contains("1RM", cut.Markup);
    }

    [Theory]
    [InlineData(0, "263.89")]
    [InlineData(0.5, "131.95")]
    [InlineData(2, "0")]
    public void ProgressRing_DashOffsetReflectsValue(double value, string offset)
    {
        var cut = Render<ProgressRing>(p => p.Add(x => x.Value, value));
        Assert.Equal(offset, cut.Find(".ring-fill").GetAttribute("stroke-dashoffset"));
    }
}
