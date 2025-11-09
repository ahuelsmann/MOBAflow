using FeedbackApi.Services;
using Moba.Backend;
using Moba.Backend.Hub;
using Moba.Backend.Manager;
using Moba.Backend.Monitor;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var z21IpAddress = builder.Configuration["Z21:IpAddress"] ?? "192.168.0.111";

// Register Backend services
builder.Services.AddSingleton<Z21>();
builder.Services.AddSingleton<FeedbackMonitor>();
builder.Services.AddSingleton<FeedbackMonitorManager>();

// SignalR für Feedback-Broadcasting
builder.Services.AddSignalR();

// Background services
builder.Services.AddHostedService<Z21ConnectionService>();
builder.Services.AddHostedService<FeedbackBroadcastService>();

// CORS für Android-App (alle Origins erlauben - nur für Development!)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// SignalR-Hub
app.MapHub<FeedbackHub>("/feedbackHub");

// Minimaler Health-Check Endpoint
app.MapGet("/health", () => "OK");

// API endpoint to get Z21 IP address
app.MapGet("/api/z21/ip", () => z21IpAddress);

// API endpoint to simulate feedback (for testing)
app.MapPost("/api/feedback/simulate/{inPort}", (uint inPort, FeedbackMonitor monitor) =>
{
    System.Diagnostics.Debug.WriteLine($"🧪 API: Simulating feedback for InPort {inPort}");
    monitor.RecordFeedback(inPort, $"Simulated {inPort}", "Test");
    return Results.Ok(new { inPort, message = "Feedback simulated" });
});

await app.RunAsync();
