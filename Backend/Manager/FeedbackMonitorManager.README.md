# FeedbackMonitorManager - Usage Guide

## Overview
The `FeedbackMonitorManager` is responsible for collecting raw feedback statistics from all Z21 InPorts for monitoring purposes (e.g., mobile apps, dashboards).

## Architecture

### Manager Responsibilities
Your application now has **four feedback perspectives**:

| Manager | Perspective | Purpose |
|---------|-------------|---------|
| `WorkflowManager` | Workflow | Executes workflows on feedback |
| `JourneyManager` | Train/Journey | Manages train journeys and stations |
| `PlatformManager` | Platform | Platform-centric announcements |
| `FeedbackMonitorManager` | **Statistics** | **Collects raw feedback data for external clients** |

## Key Design Principles

### ✅ Single Responsibility
- `FeedbackMonitorManager` **only** records statistics
- Other managers handle their entity-specific logic
- No overlap, no duplication

### ✅ No Entity Processing
- Unlike other managers, `FeedbackMonitorManager` does **not**:
  - Process entities (Workflow, Journey, Platform)
  - Apply timer-based filtering
  - Execute workflows or actions
- It simply records: "InPort X was triggered at time Y"

### ✅ Independent Operation
- Can be instantiated alongside other managers
- All managers can coexist and listen to the same Z21 instance
- Each manager processes feedbacks independently

## Usage Example

```csharp
// Setup Z21 connection
var z21 = new Z21();
z21.Connect("192.168.0.111");

// Create FeedbackMonitor instance
var feedbackMonitor = new FeedbackMonitor();

// Create the monitoring manager
var monitorManager = new FeedbackMonitorManager(z21, feedbackMonitor);

// Create other managers (they work independently)
var workflowManager = new WorkflowManager(z21, workflows);
var journeyManager = new JourneyManager(z21, journeys);
var platformManager = new PlatformManager(z21, platforms);

// Now when a feedback arrives:
// 1. FeedbackMonitorManager records it in statistics
// 2. WorkflowManager checks if any workflow matches
// 3. JourneyManager updates journey state
// 4. PlatformManager checks if any platform matches
// All happen independently!

// Reset all statistics
monitorManager.ResetAll();

// Cleanup
monitorManager.Dispose();
```

## Integration with FeedbackApi

The `FeedbackMonitor` is registered as a singleton in the API:

```csharp
// In FeedbackApi/Program.cs
builder.Services.AddSingleton<FeedbackMonitor>();
```

The `FeedbackBroadcastService` listens to `FeedbackMonitor` events and pushes updates to connected clients via SignalR.

## Next Steps

In your main application (WinUI), you need to:
1. Create a `FeedbackMonitor` instance
2. Create a `FeedbackMonitorManager` with your Z21 instance
3. The manager will automatically populate the monitor with statistics
4. (Optional) Pass the same `FeedbackMonitor` to the `FeedbackApi` if you want to share state

**Note:** The FeedbackApi already creates its own `FeedbackMonitor` instance. If you want to share the same statistics between your desktop app and the API, you would need to expose the API or use a shared database. For now, the API and desktop app have independent monitoring.
