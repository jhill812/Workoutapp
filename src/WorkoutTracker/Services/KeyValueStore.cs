using Microsoft.JSInterop;

namespace WorkoutTracker.Services;

/// <summary>Abstraction over persistent string storage so the data layer is testable.</summary>
public interface IKeyValueStore
{
    ValueTask<string?> GetAsync(string key);
    ValueTask SetAsync(string key, string value);
    ValueTask RemoveAsync(string key);
}

/// <summary>Browser localStorage — survives reloads and works offline.</summary>
public sealed class LocalStorageStore(IJSRuntime js) : IKeyValueStore
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("localStorage.getItem", key);
    public ValueTask SetAsync(string key, string value) => js.InvokeVoidAsync("localStorage.setItem", key, value);
    public ValueTask RemoveAsync(string key) => js.InvokeVoidAsync("localStorage.removeItem", key);
}

public sealed class InMemoryStore : IKeyValueStore
{
    public Dictionary<string, string> Items { get; } = [];
    public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(Items.GetValueOrDefault(key));
    public ValueTask SetAsync(string key, string value) { Items[key] = value; return ValueTask.CompletedTask; }
    public ValueTask RemoveAsync(string key) { Items.Remove(key); return ValueTask.CompletedTask; }
}
