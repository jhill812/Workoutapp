namespace WorkoutTracker.Components;

public sealed record ConfirmRequest(string Title, string Message, string ConfirmText, Func<Task> OnConfirm, bool Danger = true);
