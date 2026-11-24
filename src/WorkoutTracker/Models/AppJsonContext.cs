using System.Text.Json.Serialization;

namespace WorkoutTracker.Models;

// Source-generated serializer: trim-safe for WebAssembly publish and faster than reflection.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = false)]
[JsonSerializable(typeof(AppData))]
public partial class AppJsonContext : JsonSerializerContext;
