using Microsoft.JSInterop;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>Thin wrapper over browser/phone features (vibration, sound, files, theme).</summary>
public sealed class DeviceService(IJSRuntime js)
{
    public ValueTask Buzz(params int[] pattern) => js.InvokeVoidAsync("workoutApp.buzz", pattern);
    public ValueTask RestDoneAlert() => js.InvokeVoidAsync("workoutApp.restDone");
    public ValueTask DownloadText(string fileName, string content, string mime = "application/json") =>
        js.InvokeVoidAsync("workoutApp.download", fileName, content, mime);
    public ValueTask ApplyTheme(ThemePreference theme) =>
        js.InvokeVoidAsync("workoutApp.setTheme", theme.ToString().ToLowerInvariant());
    public ValueTask<bool> Confirm(string message) => js.InvokeAsync<bool>("confirm", message);
}
