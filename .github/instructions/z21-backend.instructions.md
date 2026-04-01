---

description: 'Z21 Backend Implementation - Critical Rules & Lessons Learned'
applyTo: 'Backend/Z21.cs, Backend/Protocol/Z21*.cs, Backend/Interface/IZ21.cs'
---

# Z21 Backend Best Practices

> **CRITICAL:** Diese Regeln verhindern Connection-Failures und Traffic-Überflutung!

---

## 🚨 KRITISCH: OnUdpReceived Paket-Parsing-Struktur

### ✅ PFLICHT: Flache if-Block-Struktur

**Jeder Paket-Typ MUSS einen separaten if-Block mit `return` haben:**

```csharp
private void OnUdpReceived(object? sender, UdpReceivedEventArgs e)
{
    var content = e.Buffer;
    
    // 1. LAN_X Header (0x40) - XBus protocol
    if (Z21MessageParser.IsLanXHeader(content))
    {
        // Parse XBusStatus, LocoInfo
        SetConnectedIfNotAlready();
        return;  // ← PFLICHT!
    }
    
    // 2. SystemState (0x84) - EIGENER BLOCK!
    if (Z21MessageParser.IsSystemState(content))
    {
        // Parse MainCurrent, Voltage, Temperature
        SetConnectedIfNotAlready();
        return;  // ← PFLICHT!
    }
    
    // 3. RBusFeedback (0x80) - SEPARATER BLOCK!
    if (Z21MessageParser.IsRBusFeedback(content))
    {
        // Parse occupancy detection
        return;  // ← PFLICHT!
    }
    
    // 4. SerialNumber, HwInfo, etc.
    // ...
}
```

### ❌ VERBOTEN: Verschachtelte Paket-Typ-Checks

```csharp
// ❌ NIEMALS SO:
if (Z21MessageParser.IsRBusFeedback(content))  // Prüft auf 0x80
{
    // ...
    
    // ❌ FEHLER: SystemState (0x84) INNERHALB von RBusFeedback (0x80)!
    if (Z21MessageParser.TryParseSystemState(content, ...))
    {
        // Das wird NIE ausgeführt! IsRBusFeedback(0x84) = false!
        SetConnectedIfNotAlready();  // ← WIRD NIE ERREICHT!
    }
}
```

**Warum ist das kritisch?**

- `IsRBusFeedback` prüft auf Header **0x80**
- `IsSystemState` prüft auf Header **0x84**
- **0x84 ≠ 0x80** → SystemState-Block wird NIE erreicht!
- **`SetConnectedIfNotAlready()` wird nie aufgerufen** → `IsConnected` bleibt `false`!
- **Connection erscheint tot**, obwohl Z21 antwortet!

---

## 🚦 KRITISCH: Traffic-Optimierung

### ✅ PFLICHT: Broadcasts > Polling

```csharp
// ✅ RICHTIG: Nur Broadcasts (effizient)
private int _systemStatePollingIntervalSeconds = 0;  // Default: disabled

// BroadcastFlags setzen (Z21 sendet automatisch bei Änderungen)
public const uint Basic = Rbus | SystemState;  // 0x0002 | 0x0100 = 0x0102
```

### ❌ VERBOTEN: Aggressives Polling

```csharp
// ❌ NIEMALS: Polling bei aktiven Broadcasts!
private int _systemStatePollingIntervalSeconds = 5;  // ← DOPPELTER TRAFFIC!

// Resultat:
// - Z21 sendet SystemState als Broadcast (Flag 0x0100)
// - UND zusätzlich alle 5s Polling
// - DOPPELTE Pakete → Z21 überlastet!
```

**Regel:**

- **Default:** `SystemStatePollingInterval = 0` (nur Broadcasts)
- **Optional:** `1-30` für Redundanz (z.B. bei instabiler Verbindung)

---

## 🔄 KRITISCH: Timer-Start-Reihenfolge

### ✅ RICHTIG: Timer erst NACH erster Z21-Response

```csharp
public async Task ConnectAsync(...)
{
    await SendHandshakeAsync();
    await SetBroadcastFlagsAsync();
    await GetStatusAsync();
    
    StartKeepaliveTimer();  // ✅ OK - sendet nur alle 30s
    
    // ❌ NICHT hier: StartSystemStatePollingTimer();
    // ✅ Wird automatisch in SetConnectedIfNotAlready() gestartet
}

private void SetConnectedIfNotAlready()
{
    if (_isConnected) return;
    _isConnected = true;
    
    // ✅ Jetzt starten - Z21 hat geantwortet!
    if (_systemStatePollingIntervalSeconds > 0)
    {
        StartSystemStatePollingTimer();
    }
    
    OnConnectedChanged?.Invoke(true);
}
```

**Warum?**

- Verhindert Request-Flut **BEVOR** Z21 überhaupt antwortet
- Z21 kann während ConnectAsync bereits überfordert sein
- Erst nach erster Response ist klar, dass Z21 bereit ist

---

## 📝 PFLICHT: Bei Edits von Backend/Z21.cs

### **Vor jedem Edit von `OnUdpReceived`:**

1. ✅ **Vollständigen Handler lesen** (alle Paket-Typ-Blocks!)

   ```text
   get_file(Backend/Z21.cs, startLine=640, endLine=765)
   ```

2. ✅ **Struktur verstehen:**

   - Wie viele `if (IsXyz)` Blocks gibt es?
   - Wo ist der richtige Einfügepunkt?
   - Hat jeder Block ein `return`?

3. ✅ **Präzise editieren:**

   ```csharp
   // ...existing IsLanXHeader block...
   
   // NEW: SystemState parsing
   if (Z21MessageParser.IsSystemState(content))
   {
       // ...
       return;
   }
   
   // ...existing IsRBusFeedback block...
   ```

4. ✅ **Sofort validieren:**

   - `run_build`
   - **Manuelle Verbindung testen!**

### **Bei großen Dateien (>500 Zeilen):**

❌ **NICHT:** Blind mit `// ...existing code...` editieren

✅ **STATTDESSEN:**

- Mehrere `get_file`-Calls für vollen Kontext
- Kleinere, präzise Edits
- Nach jedem Edit: Build + Test

---

## 🛡️ Z21 Safety Rules

### **Speed-Persistierung:**

```csharp
// ✅ RICHTIG: Immer bei 0 starten
private void ApplyCurrentPreset()
{
    Speed = 0;           // IMMER 0 (Sicherheit!)
    IsForward = true;    // IMMER vorwärts
    LocoAddress = preset.DccAddress;  // Nur Adresse laden
}

// ❌ NIEMALS: Speed aus Settings laden
private void ApplyCurrentPreset()
{
    Speed = preset.Speed;  // ← Gefährlich! Lok könnte losfahren!
}
```

**Warum?**

- Verhindert unerwartete Lokbewegung beim App-Start
- Decoder könnte noch alte Speed-Befehle gespeichert haben
- Sicherheit > Convenience

---

## 🎯 Property-Change-Notifications

### **Berechnete Properties MÜSSEN vollständige Abhängigkeiten deklarieren:**

```csharp
// ✅ KORREKT: Alle Abhängigkeiten gelistet
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(MaxSpeedStep))]

// Speed → NotifyPropertyChangedFor(SpeedKmh) ✅
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(SpeedKmh))]
private int speed;

// SpeedSteps → MaxSpeedStep UND SpeedKmh! ✅
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(MaxSpeedStep))]
[NotifyPropertyChangedFor(nameof(SpeedKmh))]  // ← PFLICHT!
private DccSpeedSteps speedSteps;

// SelectedVmax → SpeedKmh ✅
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(SpeedKmh))]
private int selectedVmax = 200;

// Berechnung:
public int SpeedKmh => (Speed / MaxSpeedStep) * SelectedVmax;
```

**Fehlt eine Notification → XAML zeigt veraltete Werte!**

## 🔍 Debugging Guidelines

### **Bei Connection-Problemen:**

```csharp
// 1. Check: Werden Pakete empfangen?
_logger?.LogDebug("UDP received {Length} bytes: {Payload}", 
    content.Length, Z21Protocol.ToHex(content));

// 2. Check: Welcher Paket-Typ?
_logger?.LogDebug("Packet type: {Type}, Header: 0x{Header:X2}", 
    packetType, content[2]);

// 3. Check: Wird SetConnectedIfNotAlready() aufgerufen?
_logger?.LogInformation("✅ Z21 is responding - connection confirmed");
```

### **Bei Traffic-Problemen:**

```csharp
// 4. Check: Wie oft wird SendAsync() aufgerufen?
_trafficMonitor?.LogSentPacket(data, packetType, details);

// 5. Check: Sind Timer aktiv?
_logger?.LogDebug("SystemState polling: {State}, Interval: {Interval}s",
    _systemStatePollingTimer != null ? "ACTIVE" : "INACTIVE",
    _systemStatePollingIntervalSeconds);
```

---

## 📚 Z21-Spezifische Regeln

### **Paket-Header (niemals verwechseln!):**

- **`LAN_X_HEADER`**
  - **Header:** `0x40`
  - **Beschreibung:** X-Bus protocol (Drive, Functions, Status)
- **`LAN_RMBUS_DATACHANGED`**
  - **Header:** `0x80`
  - **Beschreibung:** R-Bus Feedback (Occupancy)
- **`LAN_SYSTEMSTATE`**
  - **Header:** `0x84`
  - **Beschreibung:** System State (Current, Voltage, Temp)
- **`LAN_RAILCOM_DATACHANGED`**
  - **Header:** `0x88`
  - **Beschreibung:** RailCom (Lok-spezifische Daten)
- **`LAN_GET_SERIAL_NUMBER`**
  - **Header:** `0x10`
  - **Beschreibung:** Serial Number Request
- **`LAN_GET_HWINFO`**
  - **Header:** `0x1A`
  - **Beschreibung:** Hardware Info Request

**Merke:** 0x80 ≠ 0x84! **Jeder Header = eigener Block!**

---

## 🎓 Lessons Learned (2026-02-03)

### **Incident: Z21 Connection Failed After Amperemeter Implementation**

**Was passierte:**

- `edit_file` mit limited context editierte `OnUdpReceived`
- SystemState-Code wurde **fälschlicherweise IN IsRBusFeedback-Block** eingefügt
- SystemState-Pakete (0x84) wurden als Unknown behandelt
- `SetConnectedIfNotAlready()` wurde NIE aufgerufen
- Connection schien "tot"

**Root Cause:**

- **NICHT** die RailCom-Erweiterung (war inaktiv!)
- **Fehlerhafte Edit-Strategie:** Zu wenig Kontext gelesen

**Prevention:**

- ✅ Bei kritischen Event-Handlern: **VOLLEN Kontext** lesen (100+ Zeilen)
- ✅ Nach Edit: **Sofort Build + Connection-Test**
- ✅ Git-Historie nutzen bei Regression

---

## 🔧 Testing-Checklist

Nach **JEDER** Änderung an Z21.cs:

- [ ] `run_build` erfolgreich
- [ ] App starten
- [ ] Z21-Verbindung herstellen (Monitor Page oder Auto-Connect)
- [ ] Logs prüfen: "✅ Z21 is responding - connection confirmed"
- [ ] Amperemeter zeigt Live-Werte (wenn implementiert)
- [ ] Speedometer reagiert auf Speed-Änderungen
- [ ] Track Power ON/OFF funktioniert

**Wenn Connection fehlschlägt:**

1. Output Window → Debug Logs prüfen
2. Suche nach: "Unknown message"
3. Check Header-Byte: `content[2]`
4. Validiere: Passender `if (IsXyz)` Block existiert?

---

## 📦 Z21-Paket-Traffic-Budget

**Aktuell (optimiert):**

```text
ConnectAsync (einmalig):
├─ Handshake (0x85)           → 1x
├─ BroadcastFlags (0x50)      → 1x
├─ GetStatus (0x40/0x21/0x24) → 1x
└─ VersionInfo (0x10 + 0x1A)  → 2x
TOTAL: 5 Pakete

Laufend:
├─ Keepalive (alle 30s)       → GetStatus
└─ SystemState (Broadcast)    → Z21 sendet bei Änderungen
TOTAL: ~2-4 Pakete/Minute ✅

MAX: ~10 Pakete/Minute (safe für Z21)
```

**VERBOTEN (überlastet Z21):**

```text
❌ SystemState Polling = 5s  → +12 Pakete/Minute
❌ BroadcastFlags = 0xFFFFFFFF → +100 Pakete/Minute
❌ Keepalive = 5s → +12 Pakete/Minute
TOTAL: ~130 Pakete/Minute ❌ Z21 CRASH!
```

---

## 🎯 PropertyChanged-Ketten

**Für berechnete Properties:**

```csharp
// SpeedKmh hängt ab von: Speed, MaxSpeedStep, SelectedVmax

// Speed → NotifyPropertyChangedFor(SpeedKmh) ✅
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(SpeedKmh))]
private int speed;

// SpeedSteps → MaxSpeedStep UND SpeedKmh! ✅
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(MaxSpeedStep))]
[NotifyPropertyChangedFor(nameof(SpeedKmh))]  // ← PFLICHT!
private DccSpeedSteps speedSteps;

// SelectedVmax → SpeedKmh ✅
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(SpeedKmh))]
private int selectedVmax = 200;

// Berechnung:
public int SpeedKmh => (Speed / MaxSpeedStep) * SelectedVmax;
```

**Fehlt eine Notification → XAML zeigt veraltete Werte!**

---

## 🔐 Security & Safety

### **Locomotive Control:**

```csharp
// ✅ IMMER: Safety Defaults
private void ApplyCurrentPreset()
{
    Speed = 0;           // NIEMALS aus Settings laden!
    IsForward = true;    // Vorhersehbares Verhalten
    LocoAddress = preset.DccAddress;
    // Functions: OK zu laden
}

private void SaveCurrentStateToPreset()
{
    preset.DccAddress = LocoAddress;
    // Speed wird NICHT gespeichert!
    // IsForward wird NICHT gespeichert!
    SaveFunctionStates(preset);
}
```

**Begründung:**

- App-Start mit Speed > 0 → Lok fährt los → **Unfallgefahr!**
- Decoder könnte alte Speed-Werte haben
- IMMER manueller Start durch Benutzer erzwingen

---

## 🛠️ Edit-Strategie für große Backend-Dateien

### **Z21.cs (900+ Zeilen):**

**BEFORE editing OnUdpReceived:**

```python
# 1. Kompletten Handler lesen
get_file("Backend/Z21.cs", startLine=640, endLine=765)

# 2. Alle Paket-Typ-Blocks identifizieren:
#    - IsLanXHeader
#    - IsSystemState    ← WICHTIG!
#    - IsRBusFeedback   ← WICHTIG!
#    - IsSerialNumber
#    - IsHwInfo

# 3. Struktur verstehen
#    - Jeder Block hat return?
#    - Reihenfolge korrekt?

# 4. Präzise editieren mit VOLLEN Kontext-Kommentaren
edit_file(...,
    code="""
    // ...existing IsLanXHeader block...
    
    // SystemState (0x84) - separate from RBusFeedback!
    if (Z21MessageParser.IsSystemState(content))
    {
        // ...
    }
    
    // ...existing IsRBusFeedback block...
    """
)

# 5. SOFORT testen
run_build()
# Dann: Manuelle Z21-Verbindung testen!
```

### **Anti-Pattern (was schiefging):**

```python
# ❌ FEHLER: Limited Context
edit_file(...,
    code="""
    // ...existing code...  ← Unklar WAS genau!
    if (Z21MessageParser.IsRBusFeedback(content))
    {
        // ...
        if (TryParseSystemState(...))  ← FALSCH EINGEFÜGT!
    }
    """
)
```

---

## 🎯 Z21-Backend Testing Checklist

Bei **JEDEM** Commit an Backend/Z21.cs:

**Build:**

- [ ] `dotnet build Backend/Backend.csproj` erfolgreich
- [ ] Keine Warnings in Z21.cs

**Unit Tests:**

- [ ] Z21MessageParser Tests laufen durch
- [ ] Z21Protocol Tests OK

**Integration Tests:**

- [ ] Z21 Verbindung erfolgreich
- [ ] SystemState-Event wird gefeuert
- [ ] LocoInfo-Event wird gefeuert
- [ ] Track Power Commands funktionieren

**Manual Testing:**

- [ ] App starten
- [ ] Monitor Page: Z21 verbindet
- [ ] Train Control Page: Speed-Commands funktionieren
- [ ] Amperemeter zeigt Live-Werte

---

## 📚 Referenzen

- **Z21 Protokoll:** `docs/z21-lan-protokoll.pdf`
- **Traffic Monitor:** `Backend/Service/Z21Monitor.cs`
- **Message Parser:** `Backend/Protocol/Z21MessageParser.cs`
- **Unit Tests:** `Test/Backend/Z21UnitTests.cs`
