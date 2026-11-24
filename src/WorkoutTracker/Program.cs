using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WorkoutTracker;
using WorkoutTracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IKeyValueStore, LocalStorageStore>();
builder.Services.AddSingleton<WorkoutService>();
builder.Services.AddSingleton<RestTimerService>();
builder.Services.AddSingleton<DeviceService>();

await builder.Build().RunAsync();
