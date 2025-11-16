using Moba.WebApp.Components;
using Moba.SharedUI.ViewModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register SharedUI ViewModels
builder.Services.AddTransient<CounterViewModel>();

// Register Backend services (Z21 will be created per ViewModel instance)
// Note: Z21 connection is managed by CounterViewModel, not as singleton service
builder.Services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();

// Register ViewModel factories
builder.Services.AddSingleton<Moba.SharedUI.Service.IJourneyViewModelFactory, Moba.WebApp.Service.WebJourneyViewModelFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// S6966: Await RunAsync instead.
await app.RunAsync();