using System.Globalization;

namespace WorkoutTracker.Models;

public static class Units
{
    public const double KgPerLb = 0.45359237;
    public const double KmPerMile = 1.609344;

    public static double ToDisplayWeight(double kg, WeightUnit unit) =>
        Math.Round(unit == WeightUnit.Kg ? kg : kg / KgPerLb, 2);

    public static double FromDisplayWeight(double value, WeightUnit unit) =>
        unit == WeightUnit.Kg ? value : value * KgPerLb;

    public static double ToDisplayDistance(double km, WeightUnit unit) =>
        Math.Round(unit == WeightUnit.Kg ? km : km / KmPerMile, 2);

    public static double FromDisplayDistance(double value, WeightUnit unit) =>
        unit == WeightUnit.Kg ? value : value * KmPerMile;

    public static string WeightLabel(WeightUnit unit) => unit == WeightUnit.Kg ? "kg" : "lb";
    public static string DistanceLabel(WeightUnit unit) => unit == WeightUnit.Kg ? "km" : "mi";
}

public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Number(double value) => value.ToString("0.##", Inv);

    public static string Weight(double kg, WeightUnit unit) =>
        $"{Number(Units.ToDisplayWeight(kg, unit))} {Units.WeightLabel(unit)}";

    public static string Distance(double km, WeightUnit unit) =>
        $"{Number(Units.ToDisplayDistance(km, unit))} {Units.DistanceLabel(unit)}";

    /// <summary>Compact volume, e.g. 12.4k kg.</summary>
    public static string Volume(double kg, WeightUnit unit)
    {
        var v = Units.ToDisplayWeight(kg, unit);
        var text = v >= 10_000 ? (v / 1000).ToString("0.#", Inv) + "k" : v.ToString("#,0", Inv);
        return $"{text} {Units.WeightLabel(unit)}";
    }

    public static string Duration(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:00}m" : $"{Math.Max(0, t.Minutes)}m";

    public static string Clock(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";

    public static string Set(SetEntry s, ExerciseType type, WeightUnit unit) => type switch
    {
        ExerciseType.Cardio => string.Join(" · ", new[]
        {
            s.DurationSeconds > 0 ? Clock(TimeSpan.FromSeconds(s.DurationSeconds)) : null,
            s.DistanceKm > 0 ? Distance(s.DistanceKm, unit) : null
        }.Where(x => x is not null)),
        ExerciseType.Bodyweight => s.Weight > 0
            ? $"+{Number(Units.ToDisplayWeight(s.Weight, unit))} × {s.Reps}"
            : $"{s.Reps} reps",
        _ => $"{Number(Units.ToDisplayWeight(s.Weight, unit))} × {s.Reps}"
    };

    public static string MuscleName(MuscleGroup m) => m == MuscleGroup.FullBody ? "Full Body" : m.ToString();

    public static string RelativeDay(DateTime date, DateTime now)
    {
        var days = (now.Date - date.Date).Days;
        return days switch
        {
            0 => "Today",
            1 => "Yesterday",
            < 7 and > 1 => date.ToString("dddd", Inv),
            _ => date.Year == now.Year ? date.ToString("ddd, MMM d", Inv) : date.ToString("MMM d, yyyy", Inv)
        };
    }
}
