# Best Practices Quick Reference

## ✅ Top 10 Rules

1. **Self-explanatory names** - No comments needed
2. **Small functions** - One task per method
3. **Guard clauses** - Early return instead of nesting
4. **Consistent naming** - Follow conventions
5. **DI everywhere** - No `new` for services
6. **Tests = Documentation** - AAA pattern
7. **Records for DTOs** - Immutable data
8. **Pattern matching** - Instead of casting
9. **Extension methods** - For reusability
10. **ILogger** - No Console.WriteLine

---

## 🎯 Code Examples

### Guard Clauses
```csharp
// ✅ Good
public void ProcessOrder(Order order)
{
    if (order == null) throw new ArgumentNullException(nameof(order));
    if (order.Total <= 0) throw new ArgumentException("Invalid total");
    
    // Logic here - flat structure
}
```

### Extension Methods
```csharp
// StringExtensions.cs
public static bool IsNullOrEmpty(this string? value)
    => string.IsNullOrWhiteSpace(value);

// Usage
if (userName.IsNullOrEmpty()) return;
```

### Result<T> Pattern
```csharp
public Result<User> GetUser(int userId)
{
    if (userId <= 0)
        return Result<User>.Failure("Invalid ID");
    
    var user = _repo.FindById(userId);
    return user != null
        ? Result<User>.Success(user)
        : Result<User>.Failure("Not found");
}
```

### Structured Logging
```csharp
// ✅ Good - Use LoggingExtensions for dual logging
this.Log("🔊 Processing order 123");
this.LogError("Failed to process", exception);
this.LogWarning("Resource not found");

// Alternative: Direct dual logging in services with ILogger
Console.WriteLine($"🔊 Processing order {orderId}");
_logger.LogInformation("Processing order {OrderId} for customer {CustomerId}", 
    orderId, customerId);
```

**LoggingExtensions - Available everywhere:**
```csharp
using Moba.SharedUI.Extensions;

// In any class:
this.Log("✅ Operation successful");
this.LogError("Operation failed", exception);
this.LogWarning("Check configuration");
```

**Why LoggingExtensions?**
- **Extension Method**: Available on any object via `this.Log(...)`
- **Dual Logging**: Writes to Console + Debug automatically
- **No Dependencies**: No ILogger injection needed
- **Emoji Support**: Visual indicators for better readability 🔊✅❌⚠️

---

## 📝 Naming Conventions

| Type | Example |
|------|---------|
| Class | `UserService` |
| Interface | `IUserRepository` |
| Method | `GetUserAsync` |
| Private Field | `_logger` |
| Property | `FirstName` |
| Variable | `userName` |
