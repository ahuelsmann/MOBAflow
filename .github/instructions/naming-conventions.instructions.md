---
description: 'MOBAflow C# Naming Conventions - Team-wide standards for identifiers, constants, and protocol definitions'
applyTo: '**/*.cs'
---

# C# Naming Conventions

> **Wichtig:** Diese Konventionen sind in `.editorconfig` und `.sln.DotSettings` konfiguriert.

## 📋 Standard .NET Naming Rules

### Classes, Interfaces, Methods, Properties
```csharp
// ✅ PascalCase
public class TrainController { }
public interface IZ21Protocol { }
public void ConnectAsync() { }
public string Name { get; set; }
```

### Local Variables, Parameters
```csharp
// ✅ camelCase
int locomotiveAddress = 3;
void SetSpeed(int speedStep) { }
```

### Private Fields
```csharp
// ✅ _camelCase (with underscore prefix)
private readonly ILogger _logger;
private int _currentSpeed;
private bool _isConnected;
```

### Constants (Private)
```csharp
// ✅ PascalCase
private const int MaxKeepaliveFailures = 3;
private const string DefaultPort = "21105";
```

---

## 🚨 AUSNAHME: Protokoll-Konstanten

### Z21 Protocol Constants (UPPER_SNAKE_CASE)

**Dateien:**
- `Backend/Protocol/Z21Protocol.cs`
- `Backend/Protocol/Z21MessageParser.cs`

**Regel:** Protokoll-Konstanten verwenden **UPPER_SNAKE_CASE** (keine PascalCase-Konvertierung!)

```csharp
// ✅ CORRECT - Protocol constants (Z21 spec alignment)
public const byte LAN_GET_SERIAL_NUMBER = 0x10;
public const byte LAN_GET_HWINFO = 0x1A;
public const byte X_GET_VERSION = 0x21;
public const byte DCC = 0x00;
public const byte MM = 0x01;

// ❌ INCORRECT - Do NOT convert to PascalCase
public const byte LanGetSerialNumber = 0x10;  // NO!
public const byte Dcc = 0x00;                 // NO!
```

### Begründung

1. **Protokoll-Dokumentation:**
   - Z21 LAN Protocol Specification v1.13 verwendet UPPER_SNAKE_CASE
   - Direkte 1:1-Entsprechung zwischen Code und Spezifikation
   - Erleichtert Cross-Referencing bei Debugging

2. **Industry Standard:**
   - HTTP: `HTTP_OK`, `HTTP_NOT_FOUND`
   - TCP/IP: `TCP_NODELAY`, `SO_REUSEADDR`
   - DCC Protocol: `DCC`, `MM`, `RCN213`

3. **Visuelle Unterscheidung:**
   - Sofort erkennbar: "Dies ist ein Protokoll-Wert, kein Domain-Objekt"
   - Reduziert kognitive Last beim Code-Review

4. **Wartbarkeit:**
   - Protocol-Updates können direkt übernommen werden
   - Keine Übersetzung zwischen Spec-Namen und Code-Namen nötig

### Anwendungsbeispiele

```csharp
// Protocol Layer (UPPER_SNAKE_CASE)
public static class Z21Protocol
{
    public const byte LAN_X_HEADER = 0x40;
    public const byte X_GET_STATUS = 0x21;
    
    public static class HardwareType
    {
        public const int Z21_OLD = 0x00000200;
        public const int Z21_XL = 0x00000211;
    }
}

// Domain Layer (PascalCase)
public class Z21ConnectionManager
{
    private const int MaxRetries = 3;  // Domain constant
    
    public async Task<bool> GetStatusAsync()
    {
        // Protocol constant
        byte[] command = { Z21Protocol.X_GET_STATUS };
        return await SendCommandAsync(command);
    }
}
```

---

## 🔧 ReSharper Configuration

Die UPPER_SNAKE_CASE-Regel für Protokoll-Konstanten ist in `.sln.DotSettings` dokumentiert:

```xml
<!-- Protocol constants use UPPER_SNAKE_CASE (intentional) -->
<s:String x:Key="/Default/CodeStyle/Naming/CSharpNaming/Abbreviations/=DCC/@EntryIndexedValue">DCC</s:String>
<s:String x:Key="/Default/CodeStyle/Naming/CSharpNaming/Abbreviations/=MM/@EntryIndexedValue">MM</s:String>
```

**Team-Regel:**
- ReSharper-Warnungen für Protokoll-Konstanten **nicht** beheben
- Bei neuen Protokoll-Konstanten: UPPER_SNAKE_CASE verwenden
- Bei Domain-Code: Standard .NET PascalCase/camelCase verwenden

---

## 🎯 Quick Reference

| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `TrainController` |
| Interface | IPascalCase | `IZ21Protocol` |
| Method | PascalCase | `ConnectAsync()` |
| Property | PascalCase | `IsConnected` |
| Local Variable | camelCase | `locomotiveAddress` |
| Parameter | camelCase | `speedStep` |
| Private Field | _camelCase | `_logger` |
| Private Const | PascalCase | `MaxRetries` |
| **Protocol Const** | **UPPER_SNAKE_CASE** | **`LAN_GET_SERIAL_NUMBER`** |

---

## ❌ Häufige Fehler

### 1. Protokoll-Konstanten umbenennen
```csharp
// ❌ WRONG
public const byte LanGetSerialNumber = 0x10;

// ✅ CORRECT
public const byte LAN_GET_SERIAL_NUMBER = 0x10;
```

### 2. Private Fields ohne Unterstrich
```csharp
// ❌ WRONG
private ILogger logger;

// ✅ CORRECT
private readonly ILogger _logger;
```

### 3. Lokale Variablen mit Unterstrich
```csharp
// ❌ WRONG
int _currentSpeed = 0;

// ✅ CORRECT
int currentSpeed = 0;
```

---

## 🔍 Code Review Checklist

- [ ] Neue Protokoll-Konstanten verwenden UPPER_SNAKE_CASE
- [ ] Domain-Code verwendet PascalCase/camelCase
- [ ] Private Fields haben `_`-Präfix
- [ ] Lokale Variablen haben KEIN `_`-Präfix
- [ ] ReSharper-Warnungen für Protokoll-Konstanten ignoriert

---

## 📚 Weitere Informationen

- `.editorconfig` - IDE-unabhängige Code-Style-Regeln
- `.sln.DotSettings` - ReSharper-spezifische Konfiguration
- `Backend/Protocol/Z21Protocol.cs` - Protokoll-Konstanten-Beispiel
