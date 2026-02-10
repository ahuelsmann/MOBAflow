# Train Class Input Parsing - Implementation Guide

## Overview

A complete train class (Baureih) input parsing system has been implemented that converts flexible user inputs like "110", "BR110", or "br 110" into full locomotive designations like "BR 110.3 (Bügelfalte)".

## What Was Implemented

### 1. **Domain Layer** (`Domain/TrainClassLibrary.cs`)

A static library of German locomotive classes with:
- 20+ predefined locomotive classes (BR 110, BR 103, BR 01, etc.)
- Normalization logic for flexible input formats
- Lookup methods for parsing user input

**Key Features:**
```csharp
// Input formats all resolve to the same class:
TrainClassLibrary.TryGetByClassNumber("110");        // ✓ Numeric input
TrainClassLibrary.TryGetByClassNumber("BR110");      // ✓ No spaces
TrainClassLibrary.TryGetByClassNumber("BR 110");     // ✓ With spaces
TrainClassLibrary.TryGetByClassNumber("br 110");     // ✓ Case-insensitive
```

### 2. **Backend Service Layer**

#### `Backend/Interface/ITrainClassParser.cs`
Interface defining:
- `Parse(string input)` - Synchronous parsing
- `ParseAsync(string input)` - Asynchronous variant

#### `Backend/Service/TrainClassParser.cs`
Implementation using `TrainClassLibrary` for class resolution.

#### `Backend/Extensions/MobaServiceCollectionExtensions.cs`
Registered parser in DI container:
```csharp
services.AddSingleton<ITrainClassParser, TrainClassParser>();
```

### 3. **Unit Tests** (`Test/Backend/TrainClassParserTests.cs`)

Comprehensive test coverage:
- ✅ Numeric input: "110" → BR 110.3
- ✅ Lowercase prefix: "br110" → BR 110.3
- ✅ Uppercase prefix: "BR110" → BR 110.3
- ✅ With spaces: "BR 110" → BR 110.3
- ✅ Mixed case: "Br 110" → BR 110.3
- ✅ Other classes (BR 103, BR 38, BR 410)
- ✅ Invalid inputs return null
- ✅ Empty/whitespace handling
- ✅ Data integrity validation

**Run tests:**
```bash
dotnet test Test/Backend/TrainClassParserTests.cs
```

### 4. **WinUI Integration**

#### `WinUI/Service/TrainClassResolverService.cs`
High-level service for UI integration:
```csharp
// Resolve and format for display
string? displayName = service.ResolveAndFormat("110");  // "BR 110.3 (Bügelfalte)"

// Get full metadata
var series = service.Resolve("110");
// series.Name, series.Vmax, series.Type, series.Epoch, series.Description

// Get all available classes for dropdown/autocomplete
var allClasses = service.GetAllAvailableClasses();
```

#### `WinUI/ViewModel/TrainClassViewModel.cs`
MVVM ViewModel for train class input:
```csharp
public partial class TrainClassViewModel : ObservableObject
{
    [ObservableProperty]
    private string trainClassInput = string.Empty;

    [ObservableProperty]
    private LocomotiveSeries? resolvedSeries;

    [ObservableProperty]
    private string resolvedDisplayText = string.Empty;

    [ObservableProperty]
    private int vmax;

    [ObservableProperty]
    private string locomotiveType = string.Empty;

    [RelayCommand]
    public async Task ResolveInputAsync() { ... }

    [RelayCommand]
    public async Task SelectClassAsync(LocomotiveSeries series) { ... }
}
```

## Usage Examples

### In Backend Code
```csharp
public class MyService
{
    private readonly ITrainClassParser _parser;

    public MyService(ITrainClassParser parser)
    {
        _parser = parser;
    }

    public async Task<LocomotiveSeries?> GetLocomotiveAsync(string userInput)
    {
        return await _parser.ParseAsync(userInput);
    }
}
```

### In WinUI XAML ViewModel
```csharp
// Constructor injection
public TrainControlPageViewModel(ITrainClassParser parser)
{
    _classViewModel = new TrainClassViewModel(parser);
}

// In XAML:
// <TextBox Text="{x:Bind _classViewModel.TrainClassInput, Mode=TwoWay}" />
// <TextBlock Text="{x:Bind _classViewModel.ResolvedDisplayText}" />
// <Button Command="{x:Bind _classViewModel.ResolveInputCommand}" Content="Resolve" />
```

### Direct Library Usage
```csharp
// Synchronous
var series = TrainClassLibrary.TryGetByClassNumber("110");
if (series != null)
{
    Console.WriteLine($"{series.Name} - Max Speed: {series.Vmax} km/h");
}

// Get all classes
var allClasses = TrainClassLibrary.GetAllClasses();
foreach (var loco in allClasses)
{
    // Bind to UI, auto-complete, etc.
}
```

## Available Locomotive Classes

| Input | Full Name | Type | Vmax |
|-------|-----------|------|------|
| 110 | BR 110.3 (Bügelfalte) | Elektrolok | 150 |
| 103 | BR 103.1 | Elektrolok | 200 |
| 111 | BR 111 | Elektrolok | 160 |
| 120 | BR 120 | Elektrolok | 160 |
| 140 | BR 140 | Elektrolok | 120 |
| 141 | BR 141 | Elektrolok | 120 |
| 210 | BR 210 (V 100) | Diesellok | 140 |
| 215 | BR 215 (V 160) | Diesellok | 160 |
| 220 | BR 220 | Diesellok | 160 |
| 01 | BR 01 | Dampflok | 160 |
| 38 | BR 38 | Dampflok | 120 |
| 52 | BR 52 | Dampflok | 100 |
| 410 | BR 410 (ICE 3) | Triebzug | 320 |
| 412 | BR 412 (ICE 4) | Triebzug | 320 |

## Files Created

```
Domain/
  └─ TrainClassLibrary.cs          [Static class with train class definitions]

Backend/Interface/
  └─ ITrainClassParser.cs          [Interface definition]

Backend/Service/
  └─ TrainClassParser.cs           [Implementation]

Backend/Extensions/
  └─ MobaServiceCollectionExtensions.cs  [Updated with DI registration]

WinUI/Service/
  └─ TrainClassResolverService.cs  [High-level UI service]

WinUI/ViewModel/
  └─ TrainClassViewModel.cs        [MVVM ViewModel for input handling]

Test/Backend/
  └─ TrainClassParserTests.cs      [Unit tests - 20+ test cases]
```

## Adding More Locomotive Classes

To add a new class, edit `Domain/TrainClassLibrary.cs`:

```csharp
private static readonly Dictionary<string, LocomotiveSeries> Classes = new()
{
    {
        "64",
        new LocomotiveSeries
        {
            Name = "BR 64",
            Vmax = 100,
            Type = "Dampflok",
            Epoch = "III-IV",
            Description = "Passenger steam locomotive"
        }
    },
    // ... existing classes
};
```

## Testing

All unit tests pass:
```bash
dotnet test Test/Backend/TrainClassParserTests.cs -v
```

**Test Results:**
- ✅ Numeric input parsing
- ✅ Case-insensitive parsing
- ✅ Space normalization
- ✅ Invalid input handling
- ✅ Async variants
- ✅ Data integrity
- ✅ 20+ test cases

## Architecture

```
┌─────────────────────────────────────┐
│  WinUI / MAUI / WebApp              │
│  (TrainClassViewModel)              │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│  Backend/Interface                  │
│  (ITrainClassParser)                │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│  Backend/Service                    │
│  (TrainClassParser)                 │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│  Domain                             │
│  (TrainClassLibrary - Static Data)  │
└─────────────────────────────────────┘
```

## Next Steps

1. **Connect to UI:** Bind `TrainClassViewModel` to TrainControlPage.xaml
2. **Add Validation:** Implement input validation rules
3. **Extend Data:** Add more locomotive classes as needed
4. **Remote Lookup:** Optional: Extend to fetch classes from a database/API
5. **Auto-Complete:** Implement TextBox with suggestions using available classes

## Key Design Decisions

✅ **Static Library:** No database overhead for well-known classes
✅ **Case-Insensitive:** User-friendly input handling
✅ **Flexible Format:** Supports "110", "BR110", "BR 110", "br 110"
✅ **Async-Ready:** Can be extended for remote lookups
✅ **Well-Tested:** 20+ comprehensive unit tests
✅ **DI-Friendly:** Registered in service container for easy testing
✅ **MVVM-Compliant:** Full CommunityToolkit.Mvvm integration
