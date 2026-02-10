# Quick Integration Guide - Train Class Parser

## 5-Minute Setup

### Step 1: Register in DI (Already Done ✓)
The `ITrainClassParser` is already registered in `Backend/Extensions/MobaServiceCollectionExtensions.cs`:
```csharp
services.AddSingleton<ITrainClassParser, TrainClassParser>();
```

### Step 2: Inject into ViewModel
```csharp
public partial class TrainControlPageViewModel : ObservableObject
{
    private readonly ITrainClassParser _parser;

    public TrainControlPageViewModel(ITrainClassParser parser)
    {
        _parser = parser;
    }
}
```

### Step 3: Use in Code
```csharp
// Synchronous
var series = _parser.Parse("110");  // Returns LocomotiveSeries or null

// Asynchronous
var series = await _parser.ParseAsync("BR 110");

// Check if resolved
if (series != null)
{
    Console.WriteLine($"{series.Name} - {series.Vmax} km/h");
}
```

### Step 4: Bind to XAML
```xaml
<!-- Simple display -->
<TextBlock Text="{x:Bind ResolvedName, Mode=OneWay}" />

<!-- With input -->
<TextBox Text="{x:Bind TrainClassInput, Mode=TwoWay}" />
<Button Content="Resolve" Command="{x:Bind ResolveCommand}" />
```

## Use Cases

### 1. Auto-Complete Input
```csharp
var allClasses = TrainClassLibrary.GetAllClasses();
// Bind to ComboBox ItemsSource
```

### 2. Quick Lookup
```csharp
var locomotive = TrainClassLibrary.TryGetByClassNumber("110");
DisplayLocomotive(locomotive);
```

### 3. Validation
```csharp
bool IsValidTrainClass(string input)
{
    return _parser.Parse(input) != null;
}
```

### 4. Form Processing
```csharp
async Task ProcessTrainAsync(string userInput)
{
    var series = await _parser.ParseAsync(userInput);
    if (series != null)
    {
        await SaveToDatabase(series);
    }
}
```

## Testing Integration

### Manual Test (Console)
```csharp
var parser = new TrainClassParser();

// Test various inputs
Console.WriteLine(parser.Parse("110")?.Name);      // BR 110.3 (Bügelfalte)
Console.WriteLine(parser.Parse("BR110")?.Name);    // BR 110.3 (Bügelfalte)
Console.WriteLine(parser.Parse("br 110")?.Name);   // BR 110.3 (Bügelfalte)
Console.WriteLine(parser.Parse("999")?.Name);      // (null)
```

### Unit Test (Existing)
```bash
dotnet test Test/Backend/TrainClassParserTests.cs
```

## Adding New Classes

Edit `Domain/TrainClassLibrary.cs` and add to the `Classes` dictionary:

```csharp
{
    "64",
    new LocomotiveSeries
    {
        Name = "BR 64",
        Vmax = 100,
        Type = "Dampflok",
        Epoch = "III-IV",
        Description = "Your description here"
    }
}
```

Then the parser automatically supports all input variations:
- "64"
- "BR64"
- "BR 64"
- "br 64"
- etc.

## Troubleshooting

### "ITrainClassParser not found in DI"
**Solution:** Ensure `services.AddMobaBackendServices()` is called in your App setup:
```csharp
// App.xaml.cs or Program.cs
services.AddMobaBackendServices();
```

### Parse returns null for valid input
**Check:**
1. Is the class number in `TrainClassLibrary.Classes` dictionary?
2. Does it use correct formatting in the dictionary (e.g., "110", not "BR110")?
3. Check the key format:
   ```csharp
   { "110", new LocomotiveSeries { ... } }  // ✓ Correct
   ```

### Display shows empty despite valid input
**Check:** Is `ResolvedDisplayText` property binding correctly?
```xaml
<TextBlock Text="{x:Bind ViewModel.ResolvedDisplayText, Mode=OneWay}" />
```

## Files Reference

| File | Purpose |
|------|---------|
| `Domain/TrainClassLibrary.cs` | Static train class definitions |
| `Backend/Interface/ITrainClassParser.cs` | Interface |
| `Backend/Service/TrainClassParser.cs` | Implementation |
| `WinUI/Service/TrainClassResolverService.cs` | High-level UI helper |
| `WinUI/ViewModel/TrainClassViewModel.cs` | MVVM ViewModel |
| `Test/Backend/TrainClassParserTests.cs` | Unit tests |

## Performance Notes

- **TrainClassLibrary lookup:** O(1) dictionary access
- **No I/O:** All data is static in-memory
- **No async overhead:** Synchronous Parse() is instant
- **ParseAsync():** Wraps synchronous result in Task for API consistency

Safe to call from UI thread without blocking.

## Extending for Remote Lookup

If you need to fetch classes from a database or API:

```csharp
public class RemoteTrainClassParser : ITrainClassParser
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, LocomotiveSeries> _cache = new();

    public LocomotiveSeries? Parse(string input)
    {
        // First try local library
        if (TrainClassLibrary.TryGetByClassNumber(input) is { } series)
            return series;
        
        // Then try cache
        var normalized = NormalizeInput(input);
        return _cache.TryGetValue(normalized, out var cached) ? cached : null;
    }

    public async Task<LocomotiveSeries?> ParseAsync(string input)
    {
        // First try local
        if (Parse(input) is { } series)
            return series;
        
        // Then fetch from remote
        var response = await _httpClient.GetAsync($"/api/trains/{input}");
        if (response.IsSuccessStatusCode)
        {
            var series = await response.Content.ReadAsAsync<LocomotiveSeries>();
            _cache[input] = series;
            return series;
        }
        
        return null;
    }
}
```

Then register it:
```csharp
services.AddSingleton<ITrainClassParser, RemoteTrainClassParser>();
```

## More Examples

### MAUI Integration
Same as WinUI - use `ITrainClassParser` in constructor, works identically.

### Blazor Integration
```csharp
// Blazor component
@inject ITrainClassParser TrainClassParser

<input @bind="input" placeholder="110 or BR110" />
<button @onclick="ResolveAsync">Resolve</button>

@if (resolved != null)
{
    <p>@resolved.Name - @resolved.Vmax km/h</p>
}

@code {
    private string input = "";
    private LocomotiveSeries? resolved;
    
    private async Task ResolveAsync()
    {
        resolved = await TrainClassParser.ParseAsync(input);
    }
}
```

---

**Status:** ✅ Ready to use!  
**Tests:** ✅ All passing  
**Build:** ✅ Clean  
