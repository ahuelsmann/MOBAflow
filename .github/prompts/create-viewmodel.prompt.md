---
description: Template for creating platform-specific ViewModels with proper factory pattern and DI
---

# Create Platform-Specific ViewModel

I need to create a new ViewModel for a model class. Please help me:

## 1. Create Base ViewModel in SharedUI

```csharp
// SharedUI/ViewModel/[ModelName]ViewModel.cs
namespace Moba.SharedUI.ViewModel;

public abstract class [ModelName]ViewModel : ObservableObject
{
    protected readonly [ModelName] _model;
    
    protected [ModelName]ViewModel([ModelName] model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _model.PropertyChanged += OnModelPropertyChanged;
    }
    
    // Expose model properties
    public string Name => _model.Name;
    
    // Abstract method for platform-specific UI updates
    protected abstract void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e);
}
```

## 2. Create Factory Interface in SharedUI

```csharp
// SharedUI/Service/I[ModelName]ViewModelFactory.cs
namespace Moba.SharedUI.Service;

public interface I[ModelName]ViewModelFactory
{
    [ModelName]ViewModel Create([ModelName] model);
}
```

## 3. Create WinUI Implementation

```csharp
// WinUI/Factory/WinUI[ModelName]ViewModelFactory.cs
namespace Moba.WinUI.Factory;

public class WinUI[ModelName]ViewModelFactory : I[ModelName]ViewModelFactory
{
    private readonly DispatcherQueue _dispatcher;
    
    public WinUI[ModelName]ViewModelFactory(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
    }
    
    public [ModelName]ViewModel Create([ModelName] model)
    {
        return new WinUI[ModelName]ViewModel(model, _dispatcher);
    }
}

// WinUI/ViewModel/[ModelName]ViewModel.cs
namespace Moba.WinUI.ViewModel;

public class [ModelName]ViewModel : SharedUI.ViewModel.[ModelName]ViewModel
{
    private readonly DispatcherQueue _dispatcher;
    
    public [ModelName]ViewModel([ModelName] model, DispatcherQueue dispatcher)
        : base(model)
    {
        _dispatcher = dispatcher;
    }
    
    protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() => OnPropertyChanged(e.PropertyName));
    }
}
```

## 4. Create MAUI Implementation

```csharp
// MAUI/Factory/Maui[ModelName]ViewModelFactory.cs
namespace Moba.MAUI.Factory;

public class Maui[ModelName]ViewModelFactory : I[ModelName]ViewModelFactory
{
    public [ModelName]ViewModel Create([ModelName] model)
    {
        return new Maui[ModelName]ViewModel(model);
    }
}

// MAUI/ViewModel/[ModelName]ViewModel.cs (if needed, can reuse SharedUI)
namespace Moba.MAUI.ViewModel;

public class [ModelName]ViewModel : SharedUI.ViewModel.[ModelName]ViewModel
{
    public [ModelName]ViewModel([ModelName] model)
        : base(model)
    {
    }
    
    protected override async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(() => 
            OnPropertyChanged(e.PropertyName));
    }
}
```

## 5. Register in DI

### WinUI (App.xaml.cs)
```csharp
services.AddSingleton<I[ModelName]ViewModelFactory, WinUI[ModelName]ViewModelFactory>();
```

### MAUI (MauiProgram.cs)
```csharp
builder.Services.AddSingleton<I[ModelName]ViewModelFactory, Maui[ModelName]ViewModelFactory>();
```

### Blazor (Program.cs)
```csharp
builder.Services.AddSingleton<I[ModelName]ViewModelFactory, Blazor[ModelName]ViewModelFactory>();
```

## 6. Update DI Tests

### Test/WinUI/WinUiDiTests.cs
```csharp
[Test]
public void [ModelName]ViewModelFactory_ShouldBeRegistered()
{
    var factory = _serviceProvider.GetService<I[ModelName]ViewModelFactory>();
    Assert.That(factory, Is.Not.Null);
    Assert.That(factory, Is.InstanceOf<WinUI[ModelName]ViewModelFactory>());
}
```

## Usage Example

```csharp
public class MainViewModel
{
    private readonly I[ModelName]ViewModelFactory _factory;
    
    public MainViewModel(I[ModelName]ViewModelFactory factory)
    {
        _factory = factory;
    }
    
    public void LoadData([ModelName] model)
    {
        var viewModel = _factory.Create(model);
        // Use viewModel...
    }
}
```

---

## Model Details

**Model Name**: [YOUR_MODEL_NAME]  
**Model Location**: Backend/Model/[YOUR_MODEL_NAME].cs  
**Properties to Expose**: [LIST_PROPERTIES]  
**Commands Needed**: [LIST_COMMANDS]

## Platforms to Support

- [ ] WinUI (DispatcherQueue)
- [ ] MAUI (MainThread)
- [ ] Blazor (StateHasChanged)

#file:docs/DI-INSTRUCTIONS.md
#file:.github/instructions/winui.instructions.md
#file:.github/instructions/maui.instructions.md
