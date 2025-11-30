# Z21 Freeze Issue - Investigation Status

**Datum**: 2025-11-30  
**Status**: 🔍 **Investigation Paused - Awaiting Hardware Test**

---

## ✅ **Was ausgeschlossen wurde:**

### ❌ **NICHT die Ursache:**

1. **SimulateFeedback** ❌
   - Sendet **KEIN** UDP-Paket
   - Triggert nur lokales `Received` Event
   - Erhöht nur Counter
   - Code bestätigt: Keine `SendAsync` Aufrufe

2. **Workflows mit Commands** ❌
   - Aktuell nur **Dummy-Daten**
   - Workflows enthalten **keine** Z21-Commands
   - Nur Audio/Announcement Actions
   - Beispiel: `"Arrival Main Station"` → nur TextToSpeak

3. **Feedback-Loop** ❌
   - Workflows triggern keine neuen Feedbacks
   - Keine Command Actions vorhanden
   - Kein Endlos-Schleife-Potenzial

---

## 🎯 **Verdacht: Connect/Disconnect oder Keepalive**

### **Warum dieser Verdacht?**

| Aspekt | Beobachtung |
|--------|-------------|
| **UDP Traffic** | Minimal (nur Connect + Keepalive) |
| **Connect Pakete** | Handshake (4 bytes) + BroadcastFlags (7 bytes) |
| **Keepalive** | Alle 30 Sekunden → `GetStatusAsync()` |
| **User Action** | Connect → Simulate → Disconnect |

### **Mögliche Szenarien:**

#### 1. **Connect wird mehrfach aufgerufen**
```csharp
User klickt "Connect" schnell mehrfach
  → Mehrere UdpClient-Instanzen
  → Mehrere Keepalive-Timer
  → Z21 bekommt 2-3× Keepalive alle 30s
  → Überlastung → Freeze
```

#### 2. **Disconnect nicht sauber**
```csharp
Connect → Disconnect (aber Timer läuft weiter)
  → Reconnect → NEUER Timer
  → Jetzt 2 Timer aktiv
  → Nach 5× Connect/Disconnect → 5 Timer!
  → Z21: 5× Keepalive alle 30s → Freeze
```

#### 3. **Keepalive-Fehler triggert Retry-Loop**
```csharp
Keepalive sendet → Z21 antwortet nicht
  → UdpWrapper Retry: 3× mit Backoff
  → Nächster Keepalive (30s später)
  → Wieder Retry...
  → Nach 10 Min: 20+ Keepalives in Retry-Queue
  → Z21 Freeze
```

---

## 📊 **Implementiertes Logging**

### **Was geloggt wird:**

#### **UDP Performance Metrics:**
```log
[13:45:11 INF] 📊 UDP Performance: 
  - 23 total sends
  - 5 retries
  - 2.3 sends/sec
  - 0.5 retries/sec
  - 18 receives
```

#### **Jeder Send:**
```log
[13:45:01 DBG] 📤 Sending 4 bytes: 04 00 30 00
[13:45:01 DBG] ✅ Send successful in 2ms
```

#### **Jeder Retry:**
```log
[13:45:01 WRN] ⚠️ Send attempt 2/3 failed: Network unreachable. Retrying in 100ms
```

#### **Jeder Receive:**
```log
[13:45:01 DBG] 📥 Received 8 bytes from 192.168.0.111: 0F 00 80 00...
```

#### **Stats beim Stop:**
```log
[13:50:00 INF] 🛑 UDP Receiver loop stopped. Stats: 23 sends, 5 retries, 18 receives
```

### **Log-Dateien:**
- **Pfad:** `WinUI/bin/Debug/logs/mobaflow-YYYY-MM-DD.log`
- **Retention:** 7 Tage
- **Format:** Timestamp + Level + Kontext + Message

---

## 🧪 **Test-Plan (mit echter Z21)**

### **Szenario 1: Normal Connect/Disconnect**
1. Connect → 10s warten → Disconnect
2. **Erwartung:** 2 Sends (Handshake + BroadcastFlags), 0 Retries
3. **Log prüfen:** Sends/Sec sollte ~0.2 sein

### **Szenario 2: Mehrfach Connect**
1. Connect → Disconnect → Connect → Disconnect (5×)
2. **Erwartung:** 10 Sends, 0 Retries
3. **Verdacht:** Wenn Z21 freezt → Keepalive-Timer nicht gestoppt?

### **Szenario 3: Keepalive während Connect**
1. Connect → 60s warten (2 Keepalives)
2. **Erwartung:** 4 Sends (2× Handshake, 2× Keepalive)
3. **Verdacht:** Wenn mehr Sends → Keepalive-Duplikate?

### **Szenario 4: Simulate während Connected**
1. Connect → Simulate 5× → 10s warten
2. **Erwartung:** 2 Sends (nur Connect), 0 von Simulate
3. **Bestätigung:** Simulate sendet KEIN UDP

---

## 🔍 **Was die Logs zeigen sollen:**

### ✅ **Normal (Z21 OK):**
```
[13:45:00] 📤 Sending 4 bytes: 04 00 30 00  ← Handshake
[13:45:00] ✅ Send successful in 2ms
[13:45:00] 📤 Sending 7 bytes: 08 00 50 00... ← BroadcastFlags
[13:45:00] ✅ Send successful in 3ms
[13:45:30] 📤 Sending 7 bytes: 07 00 40 00... ← Keepalive #1
[13:45:30] ✅ Send successful in 2ms
[13:46:00] 📤 Sending 7 bytes: 07 00 40 00... ← Keepalive #2
[13:46:00] ✅ Send successful in 2ms
[13:46:10] 📊 UDP Performance: 4 sends, 0 retries, 0.06 sends/sec ← OK!
```

### 🔴 **Problem (Z21 Freeze):**

**Option A: Zu viele Sends**
```
[13:45:00] 📊 UDP Performance: 50 sends, 0 retries, 5.0 sends/sec ← PROBLEM!
```
→ **Diagnose:** Keepalive oder Connect wird zu oft aufgerufen

**Option B: Viele Retries**
```
[13:45:00] 📊 UDP Performance: 20 sends, 15 retries, 2.0 sends/sec ← PROBLEM!
```
→ **Diagnose:** Netzwerk-Problem oder Z21 antwortet nicht

**Option C: Keine Receives**
```
[13:45:00] 📊 UDP Performance: 10 sends, 0 retries, 1.0 sends/sec, 0 receives ← PROBLEM!
```
→ **Diagnose:** Z21 ist bereits eingefroren

---

## 📝 **Nächste Schritte:**

### **Wenn Sie zuhause sind:**

1. ✅ **App starten** (Debug Mode)
2. ✅ **Connect zur Z21**
3. ✅ **Test-Szenarien durchführen** (siehe oben)
4. ✅ **Warten bis Z21 freezt** (falls es passiert)
5. ✅ **Log-Datei öffnen:**
   ```
   notepad "C:\Repo\ahuelsmann\MOBAflow\WinUI\bin\Debug\logs\mobaflow-*.log"
   ```
6. ✅ **Letzte 50 Zeilen** kopieren und mir schicken

### **Was ich dann mache:**

1. 🔍 **Log analysieren** - Sends/Sec, Retries, Timing
2. 🎯 **Root Cause identifizieren** - Connect? Keepalive? Retry?
3. 🛠️ **Fix implementieren:**
   - Rate Limiting (max 10 commands/sec)
   - Circuit Breaker (stop bei Fehler)
   - Keepalive-Guard (nur 1 Timer)
   - Connect-Dedupe (prevent double connect)
4. ✅ **Erneut testen**

---

## 📚 **Verwandte Dateien:**

- `Backend/Z21.cs` - Keepalive-Timer, Connect/Disconnect
- `Backend/Network/UdpWrapper.cs` - Send/Receive mit Logging
- `WinUI/App.xaml.cs` - Serilog-Konfiguration
- `SharedUI/ViewModel/MainWindowViewModel.cs` - Connect/Disconnect Commands

---

## 💡 **Technische Details:**

### **Keepalive-Implementation:**
```csharp
// In Z21.cs
private void StartKeepaliveTimer()
{
    _keepaliveTimer = new Timer(
        async _ => await SendKeepaliveAsync(),
        null,
        TimeSpan.FromSeconds(30),  // First after 30s
        TimeSpan.FromSeconds(30)); // Every 30s
}

private async Task SendKeepaliveAsync()
{
    await GetStatusAsync(_cancellationTokenSource.Token);
}
```

**Potenzielle Bugs:**
- ❓ Timer wird nicht gestoppt bei Disconnect?
- ❓ Mehrere Timer bei mehrfachem Connect?
- ❓ Timer sendet auch wenn nicht connected?

### **Connect-Flow:**
```csharp
// In Z21.cs
public async Task ConnectAsync(IPAddress address, int port = 21105)
{
    await _udp.ConnectAsync(address, port);           // 1. UDP Connect
    await SendHandshakeAsync();                       // 2. Send 4 bytes
    await SetBroadcastFlagsAsync();                   // 3. Send 7 bytes
    StartKeepaliveTimer();                            // 4. Start Timer
}
```

**Potenzielle Bugs:**
- ❓ Was wenn ConnectAsync 2× aufgerufen wird?
- ❓ Wird alter Timer gestoppt?
- ❓ Wird alte UDP-Connection geschlossen?

---

## ✅ **Zusammenfassung:**

| Status | Item |
|--------|------|
| ✅ | Serilog Logging implementiert |
| ✅ | Performance-Metriken hinzugefügt |
| ✅ | SimulateFeedback ausgeschlossen |
| ✅ | Workflow-Commands ausgeschlossen |
| 🔍 | **Verdacht: Connect/Disconnect oder Keepalive** |
| ⏳ | Warte auf Hardware-Test mit echter Z21 |

**Das Logging ist bereit - wir warten nur noch auf die Test-Ergebnisse!** 🚀
