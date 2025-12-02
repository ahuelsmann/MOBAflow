# Domain Model Rules — Quick Reference

## 🎯 Golden Rule

**Domain models are PURE POCOs — no attributes, no logic, no dependencies!**

---

## ✅ ALLOWED in Domain

```csharp
namespace Moba.Domain;

public class Station
{
    // Simple properties with primitive types
    public string Name { get; set; }
    public int Track { get; set; } = 1;
    public DateTime? Arrival { get; set; }
    
    // Navigation properties (object references)
    public Workflow? Flow { get; set; }
    public Guid? WorkflowId { get; set; }  // For serialization
    
    // Collections
    public List<Platform> Platforms { get; set; } = new();
    
    // Simple constructor
    public Station()
    {
        Name = "New Station";
        Platforms = new List<Platform>();
    }
}
```

---

## ❌ FORBIDDEN in Domain

### 1. JSON Serialization Attributes

```csharp
// ❌ WRONG!
[JsonConverter(typeof(CustomConverter))]
public string Name { get; set; }

[JsonPropertyName("station_name")]
public string Name { get; set; }

[JsonIgnore]
public string Internal { get; set; }
```

**Fix**: Move converters to `Backend.Converters` or `Common.Converters` and register in `JsonSerializerOptions`.

---

### 2. Validation Attributes

```csharp
// ❌ WRONG!
[Required]
[StringLength(100)]
public string Name { get; set; }

[Range(1, 20)]
public int Track { get; set; }
```

**Fix**: Implement validation in `Backend.Services.ValidationService` or ViewModels.

---

### 3. Property Change Notification

```csharp
// ❌ WRONG!
public class Station : INotifyPropertyChanged
{
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }
}

// ❌ WRONG!
public partial class Station : ObservableObject
{
    [ObservableProperty]
    private string _name;
}
```

**Fix**: Implement in `SharedUI.ViewModel.StationViewModel` using `ObservableObject`.

---

### 4. Business Logic

```csharp
// ❌ WRONG!
public class Journey
{
    public void ProcessFeedback(int inPort)
    {
        CurrentCounter++;
        if (CurrentCounter >= Stations[CurrentPos].NumberOfLapsToStop)
        {
            // Execute workflow...
        }
    }
}
```

**Fix**: Move logic to `Backend.Manager.JourneyManager` or `Backend.Services.*`.

---

### 5. External Dependencies

```csharp
// ❌ WRONG!
using Moba.Backend.Services;
using Moba.Common.Helpers;

public class Station
{
    private readonly IWorkflowService _workflowService;  // ❌
}
```

**Fix**: Domain models should only reference `System.*` namespaces.

---

## 🔧 Where to Put What

| Concern | Location | Example |
|---------|----------|---------|
| **Data structure** | `Domain/*.cs` | `public class Station { ... }` |
| **JSON serialization** | `Backend.Converters` or `Common.Converters` | `WorkflowReferenceConverter` |
| **Validation** | `Backend.Services.ValidationService` | `CanDeleteStation(Station s)` |
| **Business logic** | `Backend.Manager` or `Backend.Services` | `JourneyManager.ProcessFeedback()` |
| **UI binding** | `SharedUI.ViewModel` | `StationViewModel : ObservableObject` |
| **Type conversion** | `SharedUI.ViewModel` (for display) | `EffectiveTrack => Track.ToString()` |

---

## 📐 Type Guidelines

### Primitive Properties

```csharp
// ✅ Use appropriate primitive types
public int Track { get; set; } = 1;           // NOT string!
public DateTime? Arrival { get; set; }         // NOT string!
public uint InPort { get; set; }               // NOT int if always positive
```

### Navigation Properties

```csharp
// ✅ Use object references AND IDs for serialization
public Workflow? Flow { get; set; }            // Navigation property
public Guid? WorkflowId { get; set; }          // For JSON serialization

// Custom converter handles Flow ↔ WorkflowId mapping
```

### Collections

```csharp
// ✅ Initialize collections in constructor
public List<Station> Stations { get; set; }

public Journey()
{
    Stations = new List<Station>();  // Not []
}
```

---

## 🧪 How to Test Domain Models

```csharp
// ✅ CORRECT: Simple property tests
[Test]
public void Station_DefaultTrack_ShouldBe1()
{
    var station = new Station();
    Assert.That(station.Track, Is.EqualTo(1));
}

// ✅ CORRECT: Serialization tests (in Backend tests!)
[Test]
public void Station_SerializeWithWorkflow_ShouldUseWorkflowId()
{
    var station = new Station { WorkflowId = Guid.NewGuid() };
    var json = JsonSerializer.Serialize(station, options);
    Assert.That(json, Does.Contain("\"WorkflowId\""));
    Assert.That(json, Does.Not.Contain("\"Flow\""));
}
```

---

## 🚨 Red Flags

If you see these in Domain models, **STOP and refactor**:

- [ ] `using System.Text.Json.Serialization;`
- [ ] `using System.ComponentModel.DataAnnotations;`
- [ ] `using CommunityToolkit.Mvvm;`
- [ ] `[Attribute]` on any property
- [ ] Methods with business logic
- [ ] Dependencies on Backend/SharedUI/Common

---

## 💡 Quick Fixes

### Problem: JSON converter needed

```csharp
// ❌ BEFORE (Domain/Station.cs)
[JsonConverter(typeof(TrackConverter))]
public int Track { get; set; }

// ✅ AFTER (Domain/Station.cs)
public int Track { get; set; }

// ✅ AFTER (Backend/Converters/TrackConverter.cs)
public class TrackConverter : JsonConverter<int> { ... }

// ✅ AFTER (Backend/Services/IoService.cs)
var options = new JsonSerializerOptions();
options.Converters.Add(new TrackConverter());
```

### Problem: Validation needed

```csharp
// ❌ BEFORE (Domain/Station.cs)
[Required]
[StringLength(100)]
public string Name { get; set; }

// ✅ AFTER (Domain/Station.cs)
public string Name { get; set; }

// ✅ AFTER (Backend/Services/ValidationService.cs)
public ValidationResult ValidateStation(Station station)
{
    if (string.IsNullOrWhiteSpace(station.Name))
        return ValidationResult.Failure("Name is required");
    if (station.Name.Length > 100)
        return ValidationResult.Failure("Name too long");
    return ValidationResult.Success();
}
```

### Problem: Property change notification needed

```csharp
// ❌ BEFORE (Domain/Station.cs)
public partial class Station : ObservableObject
{
    [ObservableProperty]
    private string _name;
}

// ✅ AFTER (Domain/Station.cs)
public class Station
{
    public string Name { get; set; }
}

// ✅ AFTER (SharedUI/ViewModel/StationViewModel.cs)
public partial class StationViewModel : ObservableObject
{
    [ObservableProperty]
    private Station model;
    
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }
}
```

---

## 📚 Related Documentation

- **Architecture Overview**: [docs/ARCHITECTURE.md](../ARCHITECTURE.md)
- **Clean Architecture Status**: [docs/CLEAN-ARCHITECTURE-FINAL-STATUS.md](../CLEAN-ARCHITECTURE-FINAL-STATUS.md)
- **Full Copilot Instructions**: [.github/copilot-instructions.md](../.github/copilot-instructions.md)

---

**Last Updated**: 2025-01-18  
**Version**: 1.0
