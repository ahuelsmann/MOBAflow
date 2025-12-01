# 🚀 Async/Await Best Practices

Complete guidelines for using async/await patterns in MOBAflow.

---

## ❓ When to Use Async

- **Use Async for I/O-bound work**: Any operation that involves network, file, or database should be async.
- **Avoid Async for CPU-bound work**: If your method is doing heavy computations, keep it synchronous or offload to a background thread.

---

## 📇 Async Method Naming

- **Suffix with `Async`**: This is crucial for differentiating between sync and async methods at a glance.
- **Example**: For a method that fetches data, use `FetchDataAsync` instead of `FetchData`.

---

## 🚫 Anti-Patterns to Avoid

### 1. **Async Overhead for Simple Tasks**
Don't make simple property getters/setters async. It's unnecessary and adds overhead.

```csharp
// ❌ BAD: Overusing async
public async Task<int> GetNumberAsync() => await Task.FromResult(_number);
```

### 2. **Blocking on Async Code**
Avoid using `.Result` or `.Wait()` on tasks. It can cause deadlocks, especially in UI applications.

```csharp
// ❌ BAD: Blocking async code
var data = LoadDataAsync().Result; // Potential deadlock!
```

### 3. **Fire-and-Forget**
If you must fire-and-forget, at least log the exceptions in the async method.

```csharp
// ❌ BAD: Ignoring exceptions
public void Start() { _ = DoWorkAsync(); }

// ✅ GOOD: Handle exceptions
public void Start()
{
    _ = Task.Run(async () =>
    {
        try { await DoWorkAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Error in DoWorkAsync"); }
    });
}
```

### 4. **Async Void**
This should be avoided except for event handlers. It makes error handling and testing difficult.

```csharp
// ❌ BAD: Async void method
public async void OnButtonClick()
{
    await DoSomethingAsync(); // If this fails, it's hard to know!
}
```

---

## ⏳ Task.Run() Usage

**Offload CPU-bound work**: If you have a synchronous method that is CPU-intensive, wrap it in `Task.Run()` to run it on a background thread.

```csharp
public void ProcessData()
{
    Task.Run(() =>
    {
        // CPU-intensive work here
    });
}
```

---

## 🔗 Configuring Await

### **Use `ConfigureAwait(false)`** in library or backend code
Where you don't need the context (like UI context).

### **Don't use it in UI code**
Your ViewModels and Views usually need the UI context to update bindings.

```csharp
// ✅ GOOD: Library code (Backend, SharedUI without UI updates)
public async Task<Data> LoadDataAsync()
{
    var content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
    return Parse(content);
}

// ❌ BAD: UI code (MAUI ViewModels)
public async Task LoadAsync()
{
    var data = await _service.LoadDataAsync().ConfigureAwait(false); // Don't do this!
    Items = new ObservableCollection<Item>(data); // Needs UI thread
}
```

---

## 📂 IAsyncEnumerable for Streaming Data

**Use `IAsyncEnumerable<T>` for methods that return a stream of data**: This is especially useful for paging or continuous data feeds.

```csharp
public async IAsyncEnumerable<int> GetNumbersAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    for (int i = 0; i < 10; i++)
    {
        await Task.Delay(1000, cancellationToken); // Simulate async work
        yield return i;
    }
}
```

---

## ✔️ Error Handling in Async

**Always use try-catch around await**: This is crucial for logging and handling exceptions in async methods.

**Rethrow exceptions** after logging if you want the caller to handle them.

```csharp
public async Task ConnectAsync()
{
    try
    {
        await _z21.ConnectAsync();
    }
    catch (SocketException ex)
    {
        _logger.LogError(ex, "Z21 connection failed");
        StatusText = $"Connection failed: {ex.Message}";
        throw; // Important: Rethrow the exception
    }
}
```

---

## ✔️ Best Practices Checklist

- [ ] **All I/O operations use async/await?**
- [ ] **No async void methods except for event handlers?**
- [ ] **Methods that don't need the context use ConfigureAwait(false)?**
- [ ] **No deadlocks caused by blocking on async code?**
- [ ] **Fire-and-forget tasks have exception handling?**
- [ ] **Async methods have proper naming (`Async` suffix)?**
- [ ] **IAsyncEnumerable used for streaming data when appropriate?**

---

## **Related Guidelines:**
- See [THREADING.md](THREADING.md) for UI thread dispatching
- See [ARCHITECTURE.md](../ARCHITECTURE.md) for separation of concerns
- See `.copilot-instructions.md` → **MVVM Pattern**
