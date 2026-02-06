# Startup Optimization - Baseline & Progress

**Date:** 2026-02-10  
**Status:** 🔄 Optimization Phase 1 Complete

---

## Changes Made

### Phase 1: App Startup Optimization (COMPLETED)

#### 1.1: Created PostStartupInitializationService
- **File:** `WinUI/Service/PostStartupInitializationService.cs`
- **Purpose:** Defers non-critical initialization (HealthCheckService, WebApp) until after MainWindow is visible
- **Benefits:**
  - Eliminates blocking calls in App constructor
  - Allows UI to become responsive before heavy operations
  - Non-blocking error handling - errors in deferred services don't crash app

#### 1.2: Optimized WinUI/App.xaml.cs
- Removed `HealthCheckService` initialization from startup
- Moved `SpeechHealthCheck` registration to deferred phase
- Updated `OnLaunched()` to trigger `InitializePostStartupServicesAsync()`
- Added 30-second timeout for deferred services

**Key Changes:**
```csharp
// Before: Synchronous initialization blocked startup
services.AddSingleton<HealthCheckService>();
services.AddSingleton<SpeechHealthCheck>();

// After: Deferred to PostStartupInitializationService
services.AddSingleton<PostStartupInitializationService>();
```

#### 1.3: Optimized MAUI/MauiProgram.cs
- Marked `RestApiDiscoveryService` as lazy-loaded
- Added comments about deferred backend initialization
- Kept essential services for immediate startup

**Key Changes:**
```csharp
// Network discovery now deferred - won't block startup
builder.Services.AddSingleton<RestApiDiscoveryService>(sp =>
    new Lazy<RestApiDiscoveryService>(() => new RestApiDiscoveryService()).Value);
```

---

## Measurement Baseline

### Before Optimization (BASELINE NEEDED)
**Instructions:**
1. Revert to commit before these changes
2. Use VS Profiler → Application Timeline
3. Measure these metrics:
   - Time to MainWindow.Activate() (UI visible)
   - Time to AutoLoadLastSolution (interactive)
   - Total startup duration

**Record:**
```
Baseline Startup Times (Before):
- UI Visible: ___ ms
- Interactive: ___ ms
- Total: ___ ms
```

### After Optimization (MEASURE NOW)
**Instructions:**
1. Build in Release mode: `dotnet build -c Release`
2. Run the app and measure with VS Profiler
3. Record same metrics

**Record:**
```
Optimized Startup Times (After):
- UI Visible: ___ ms
- Interactive: ___ ms
- Total: ___ ms

Improvement:
- UI Visible: -___ ms (___ %)
- Interactive: -___ ms (___ %)
- Total: -___ ms (___ %)
```

---

## How to Measure Startup Time

### Option 1: Visual Studio Profiler (Recommended)
1. **Start Profiling:**
   - Debug → Performance Profiler
   - Select "Application Timeline"
   - Click "Start"

2. **Run App:**
   - Launch app normally
   - Wait for app to load completely
   - Stop profiling (click "Stop collection")

3. **Analyze:**
   - Look for `Application.Start()` duration
   - Check `OnLaunched()` duration
   - Note when UI becomes interactive

### Option 2: Stopwatch in Code
```csharp
private static DateTime _appStartTime = DateTime.Now;

public App()
{
    var elapsed = DateTime.Now - _appStartTime;
    Debug.WriteLine($"[Timing] App constructor: {elapsed.TotalMilliseconds}ms");
    // ...
}

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var elapsed = DateTime.Now - _appStartTime;
    Debug.WriteLine($"[Timing] OnLaunched: {elapsed.TotalMilliseconds}ms");
    // ...
}
```

### Option 3: ETW Trace
```powershell
dotnet-trace collect --profile cpu-sampling -d 5 -- dotnet YourApp.dll
```

---

## Functional Validation Checklist

- [ ] App starts without errors
- [ ] MainWindow appears responsive
- [ ] Z21 connection works (delayed but functional)
- [ ] HealthCheckService starts after UI loads
- [ ] No runtime errors in Output window
- [ ] Auto-load solution works
- [ ] Navigation works correctly

---

## Files Modified

1. ✅ `WinUI/Service/PostStartupInitializationService.cs` (NEW)
2. ✅ `WinUI/App.xaml.cs` (MODIFIED)
3. ✅ `MAUI/MauiProgram.cs` (MODIFIED)

---

## Next Steps

### Phase 2: TrackLibrary Lazy-Loading (Pending)
- [ ] Check `TrackLibrary.PikoA/WR.cs` usage
- [ ] Implement lazy-loading wrapper
- [ ] Defer until TrackPlanPage first accessed

### Phase 3: Z21 Connection Deferral (Pending)
- [ ] Defer `Z21BackgroundService` initialization
- [ ] Connect on-demand when user navigates to train control
- [ ] Maintain transparent error handling

### Phase 4: Performance Benchmarking (Pending)
- [ ] Measure actual improvement in startup time
- [ ] Compare WinUI vs MAUI startup times
- [ ] Document best practices for future features

---

## Performance Goals

**Target:** Reduce startup time by 500ms-1s

- **UI Visible:** < 1000ms
- **Interactive (auto-load complete):** < 3000ms
- **Total app ready:** < 5000ms
