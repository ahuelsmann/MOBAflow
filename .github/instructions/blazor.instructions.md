---
description: Blazor Server specific patterns for WebApp with SignalR, component lifecycle, and state management
applyTo: "WebApp/**/*.{cs,razor}"
---

# Blazor Server Development Guidelines

## 🎯 Blazor-Specific Patterns

### Component Lifecycle

```csharp
// ✅ CORRECT: Proper lifecycle implementation
@code {
    [Inject] public IJourneyViewModelFactory Factory { get; set; } = default!;
    
    private JourneyViewModel? _viewModel;
    
    protected override void OnInitialized()
    {
        // One-time initialization
        _viewModel = Factory.Create(model);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged); // ✅ Thread-safe UI update
    }
    
    public void Dispose()
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}
```

### State Management & UI Updates

```csharp
// ✅ CORRECT: InvokeAsync + StateHasChanged
private void OnZ21Event(object? sender, EventArgs e)
{
    InvokeAsync(() =>
    {
        // Update properties
        SomeProperty = newValue;
        StateHasChanged(); // ✅ Force UI refresh
    });
}

// ❌ WRONG: Direct update without InvokeAsync
private void OnZ21Event(object? sender, EventArgs e)
{
    SomeProperty = newValue; // UI won't update reliably
}
```

### Dependency Injection

```csharp
// ✅ CORRECT: @inject in .razor file
@inject IJourneyViewModelFactory JourneyFactory
@inject IZ21 Z21Client
@inject NavigationManager Navigation

// ✅ CORRECT: [Inject] in @code block
@code {
    [Inject] public IJourneyViewModelFactory Factory { get; set; } = default!;
    [Inject] public IZ21 Z21 { get; set; } = default!;
}

// ❌ WRONG: Constructor injection in Blazor components
public JourneyPage(IJourneyViewModelFactory factory) // Don't do this!
{
}
```

### Event Handling

```csharp
// ✅ CORRECT: Async event handlers
private async Task SaveAsync()
{
    IsSaving = true;
    try
    {
        await _backend.SaveAsync();
        StatusMessage = "Saved successfully";
    }
    catch (Exception ex)
    {
        ErrorMessage = ex.Message;
    }
    finally
    {
        IsSaving = false;
    }
}

// In .razor:
<button @onclick="SaveAsync">Save</button>
```

## 🔄 SignalR for Real-Time Updates

### Hub Setup

```csharp
// WebApp/Hubs/Z21Hub.cs
public class Z21Hub : Hub
{
    private readonly IZ21 _z21;
    
    public Z21Hub(IZ21 z21)
    {
        _z21 = z21;
    }
    
    public async Task SendCommand(byte[] data)
    {
        await _z21.SendAsync(data);
    }
}

// Program.cs
builder.Services.AddSignalR();
app.MapHub<Z21Hub>("/z21hub");
```

### Client Connection

```razor
@inject NavigationManager Navigation
@implements IAsyncDisposable

@code {
    private HubConnection? _hubConnection;
    
    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/z21hub"))
            .Build();
            
        _hubConnection.On<string>("ReceiveUpdate", (message) =>
        {
            InvokeAsync(() =>
            {
                StatusMessage = message;
                StateHasChanged();
            });
        });
        
        await _hubConnection.StartAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

## 🎨 Razor Syntax Patterns

### Conditional Rendering

```razor
<!-- ✅ CORRECT: @if for conditional content -->
@if (IsLoading)
{
    <p>Loading...</p>
}
else if (HasError)
{
    <div class="alert alert-danger">@ErrorMessage</div>
}
else
{
    <JourneyList Items="@Journeys" />
}

<!-- ✅ CORRECT: Ternary for inline conditions -->
<button class="btn @(IsSaving ? "disabled" : "")">Save</button>
```

### Loops & Collections

```razor
<!-- ✅ CORRECT: @foreach with @key -->
@foreach (var journey in Journeys)
{
    <JourneyCard @key="journey.Id" Journey="@journey" />
}

<!-- ❌ WRONG: No @key (causes rendering issues) -->
@foreach (var journey in Journeys)
{
    <JourneyCard Journey="@journey" />
}
```

### Two-Way Binding

```razor
<!-- ✅ CORRECT: @bind with event -->
<input @bind="SearchTerm" @bind:event="oninput" />

<!-- ✅ CORRECT: Custom component binding -->
<MyInput @bind-Value="JourneyName" />

@code {
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
}
```

## 🧩 Component Communication

### Parent → Child (Parameters)

```razor
<!-- Parent.razor -->
<ChildComponent Title="@PageTitle" OnClick="@HandleClick" />

<!-- ChildComponent.razor -->
@code {
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public EventCallback OnClick { get; set; }
    
    private async Task InvokeClick()
    {
        await OnClick.InvokeAsync();
    }
}
```

### Child → Parent (EventCallback)

```razor
<!-- Child.razor -->
<button @onclick="NotifyParent">Click Me</button>

@code {
    [Parameter] public EventCallback<string> OnDataChanged { get; set; }
    
    private async Task NotifyParent()
    {
        await OnDataChanged.InvokeAsync("New Data");
    }
}

<!-- Parent.razor -->
<Child OnDataChanged="HandleDataChanged" />

@code {
    private void HandleDataChanged(string data)
    {
        // Handle the event
    }
}
```

### Cascading Values

```razor
<!-- App.razor or Layout -->
<CascadingValue Value="@CurrentUser">
    @Body
</CascadingValue>

<!-- Any child component -->
@code {
    [CascadingParameter] public User? CurrentUser { get; set; }
}
```

## 🔧 DI Registration

```csharp
// Program.cs
builder.Services.AddSingleton<IUiDispatcher, BlazorUiDispatcher>();
builder.Services.AddSingleton<IJourneyViewModelFactory, WebJourneyViewModelFactory>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthProvider>();

// ⚠️ Scoped vs Singleton:
// - Singleton: Backend services (IZ21, Solution)
// - Scoped: Per-request state (Auth, User preferences)
// - Transient: Rarely used in Blazor Server
```

## 📱 Rendering Modes

```razor
<!-- ✅ Blazor Server (default) -->
@page "/journeys"
@rendermode InteractiveServer

<!-- ✅ Static SSR for non-interactive pages -->
@page "/about"
@rendermode Static

<!-- ✅ Auto (decides at runtime) -->
@page "/dashboard"
@rendermode InteractiveAuto
```

## 🚦 Navigation

```csharp
// ✅ Programmatic navigation
@inject NavigationManager Navigation

private void NavigateToJourney(int id)
{
    Navigation.NavigateTo($"/journey/{id}");
}

// ✅ NavLink for menu items
<NavLink href="journeys" ActiveClass="active">
    Journeys
</NavLink>
```

## 🔒 Authentication & Authorization

```razor
<!-- ✅ Require authentication -->
<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity?.Name</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in</p>
    </NotAuthorized>
</AuthorizeView>

<!-- ✅ Role-based authorization -->
<AuthorizeView Roles="Admin">
    <button @onclick="DeleteAll">Delete All</button>
</AuthorizeView>
```

## 📋 Checklist

When modifying Blazor code:

- [ ] Use `InvokeAsync(StateHasChanged)` for async updates
- [ ] `@inject` or `[Inject]` for DI (no constructor injection)
- [ ] Implement `IDisposable` or `IAsyncDisposable` for event cleanup
- [ ] Use `@key` in loops
- [ ] `EventCallback<T>` for component events
- [ ] Scoped services for per-request state
- [ ] SignalR for real-time updates
- [ ] Proper lifecycle methods (`OnInitialized`, `OnAfterRender`)
- [ ] Thread-safe event handling

## 🗂️ File Organization

```
WebApp/
├── Pages/                  ← Routable pages (@page)
│   ├── Index.razor
│   ├── Journeys.razor
│   └── Trains.razor
├── Components/             ← Reusable components
│   ├── JourneyCard.razor
│   └── TrainList.razor
├── Shared/                 ← Layout components
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── Factory/                ← ViewModel factories
│   ├── WebJourneyViewModelFactory.cs
│   └── ...
├── Service/                ← Blazor-specific services
│   └── BlazorUiDispatcher.cs
└── Program.cs              ← DI registration
```

## ⚡ Performance Tips

```csharp
// ✅ Virtualization for large lists
<Virtualize Items="@Journeys" Context="journey">
    <JourneyCard Journey="@journey" />
</Virtualize>

// ✅ Lazy loading
<LazyLoad>
    <HeavyComponent />
</LazyLoad>

// ✅ ShouldRender override
protected override bool ShouldRender()
{
    return _hasChanges; // Only re-render when needed
}
```
