using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public sealed record WeekSummary(DateOnly WeekStart, int Workouts, double Volume, TimeSpan Duration);

public sealed record ExerciseSession(
    DateTime Date, Guid WorkoutId, IReadOnlyList<SetEntry> Sets,
    double BestWeight, double BestOneRepMax, double Volume, int TotalReps, int MaxReps,
    int TotalSeconds, double TotalKm);

public sealed record PersonalRecord(
    string ExerciseId, string Name, ExerciseType Type,
    double HeaviestWeight, double BestOneRepMax, int MaxReps, double BestSessionVolume,
    int LongestSeconds, double LongestKm, DateTime AchievedAt, int Sessions);

/// <summary>Pure statistics over workout history. All methods ignore uncompleted sets.</summary>
public static class Stats
{
    /// <summary>Epley estimate of one-rep max.</summary>
    public static double EstimatedOneRepMax(double weight, int reps) =>
        weight <= 0 || reps <= 0 ? 0 : reps == 1 ? weight : weight * (1 + reps / 30.0);

    public static double SetVolume(SetEntry s, ExerciseType type) =>
        s.Completed && type != ExerciseType.Cardio ? s.Weight * s.Reps : 0;

    public static double Volume(Workout w) =>
        w.Exercises.Sum(e => e.Sets.Sum(s => SetVolume(s, e.Type)));

    public static int CompletedSets(Workout w) => w.Exercises.Sum(e => e.Sets.Count(s => s.Completed));

    public static TimeSpan Duration(Workout w, DateTime now) =>
        (w.FinishedAt ?? now) - w.StartedAt;

    public static DateOnly StartOfWeek(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7)); // Monday

    public static DateOnly StartOfWeek(DateTime d) => StartOfWeek(DateOnly.FromDateTime(d));

    public static int WorkoutsThisWeek(IEnumerable<Workout> workouts, DateTime today)
    {
        var week = StartOfWeek(today);
        return workouts.Count(w => !w.IsActive && StartOfWeek(w.StartedAt) == week);
    }

    /// <summary>
    /// Consecutive weeks (Mon–Sun) with at least one workout. The current week counts if it has a
    /// workout, but an empty current week doesn't break the streak yet.
    /// </summary>
    public static int WeekStreak(IEnumerable<Workout> workouts, DateTime today)
    {
        var weeks = workouts.Where(w => !w.IsActive).Select(w => StartOfWeek(w.StartedAt)).ToHashSet();
        var week = StartOfWeek(today);
        if (!weeks.Contains(week)) week = week.AddDays(-7);
        var streak = 0;
        while (weeks.Contains(week))
        {
            streak++;
            week = week.AddDays(-7);
        }
        return streak;
    }

    /// <summary>Summary for the last <paramref name="weeks"/> weeks, oldest first, including empty weeks.</summary>
    public static IReadOnlyList<WeekSummary> Weekly(IEnumerable<Workout> workouts, DateTime today, int weeks = 8)
    {
        var current = StartOfWeek(today);
        var byWeek = workouts.Where(w => !w.IsActive).ToLookup(w => StartOfWeek(w.StartedAt));
        return Enumerable.Range(0, weeks).Reverse().Select(i =>
        {
            var start = current.AddDays(-7 * i);
            var ws = byWeek[start].ToList();
            return new WeekSummary(start, ws.Count, ws.Sum(Volume),
                TimeSpan.FromTicks(ws.Sum(w => Duration(w, today).Ticks)));
        }).ToList();
    }

    /// <summary>Every finished session of one exercise, oldest first.</summary>
    public static IReadOnlyList<ExerciseSession> ExerciseHistory(IEnumerable<Workout> workouts, string exerciseId) =>
        workouts.Where(w => !w.IsActive)
            .OrderBy(w => w.StartedAt)
            .SelectMany(w => w.Exercises.Where(e => e.ExerciseId == exerciseId).Select(e => (w, e)))
            .Select(x =>
            {
                var sets = x.e.Sets.Where(s => s.Completed).ToList();
                return (x, sets);
            })
            .Where(t => t.sets.Count > 0)
            .Select(t => new ExerciseSession(
                t.x.w.StartedAt, t.x.w.Id, t.sets,
                BestWeight: t.sets.Max(s => s.Weight),
                BestOneRepMax: t.sets.Max(s => EstimatedOneRepMax(s.Weight, s.Reps)),
                Volume: t.sets.Sum(s => SetVolume(s, t.x.e.Type)),
                TotalReps: t.sets.Sum(s => s.Reps),
                MaxReps: t.sets.Max(s => s.Reps),
                TotalSeconds: t.sets.Sum(s => s.DurationSeconds),
                TotalKm: t.sets.Sum(s => s.DistanceKm)))
            .ToList();

    public static PersonalRecord? GetRecord(IEnumerable<Workout> workouts, string exerciseId)
    {
        var list = workouts.ToList();
        var history = ExerciseHistory(list, exerciseId);
        if (history.Count == 0) return null;

        var info = list.SelectMany(w => w.Exercises).First(e => e.ExerciseId == exerciseId);
        var bestKey = info.Type switch
        {
            ExerciseType.Cardio => history.MaxBy(h => (h.TotalKm, h.TotalSeconds))!,
            ExerciseType.Bodyweight => history.MaxBy(h => (h.BestOneRepMax, h.MaxReps))!,
            _ => history.MaxBy(h => h.BestOneRepMax)!
        };
        return new PersonalRecord(
            exerciseId, info.Name, info.Type,
            HeaviestWeight: history.Max(h => h.BestWeight),
            BestOneRepMax: history.Max(h => h.BestOneRepMax),
            MaxReps: history.Max(h => h.MaxReps),
            BestSessionVolume: history.Max(h => h.Volume),
            LongestSeconds: history.Max(h => h.TotalSeconds),
            LongestKm: history.Max(h => h.TotalKm),
            AchievedAt: bestKey.Date,
            Sessions: history.Count);
    }

    /// <summary>Records for every exercise ever performed, most-trained first.</summary>
    public static IReadOnlyList<PersonalRecord> AllRecords(IEnumerable<Workout> workouts)
    {
        var list = workouts.Where(w => !w.IsActive).ToList();
        return list.SelectMany(w => w.Exercises).Select(e => e.ExerciseId).Distinct()
            .Select(id => GetRecord(list, id))
            .OfType<PersonalRecord>()
            .OrderByDescending(r => r.Sessions).ThenBy(r => r.Name)
            .ToList();
    }

    /// <summary>Completed sets per muscle group since a date, largest first.</summary>
    public static IReadOnlyList<(MuscleGroup Muscle, int Sets)> MuscleSplit(IEnumerable<Workout> workouts, DateTime since) =>
        workouts.Where(w => !w.IsActive && w.StartedAt >= since)
            .SelectMany(w => w.Exercises)
            .GroupBy(e => e.Muscle)
            .Select(g => (g.Key, g.Sum(e => e.Sets.Count(s => s.Completed))))
            .Where(x => x.Item2 > 0)
            .OrderByDescending(x => x.Item2)
            .ToList();
}
