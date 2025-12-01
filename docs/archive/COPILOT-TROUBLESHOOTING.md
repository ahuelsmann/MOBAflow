# 🔧 Copilot Troubleshooting Guide for MOBAflow

## ❌ **Häufige Probleme & Lösungen**

### 1. **"Datei nicht gefunden" Fehler**

#### **Problem:**
- `get_file` schlägt fehl mit: "File not found or more than one file exist"
- Betrifft oft: `MainWindowViewModel.cs`, `Z21.cs`, usw.

#### **Ursachen:**
1. ✅ **Mehrere Dateien mit gleichem Namen** existieren:
   - `SharedUI\ViewModel\MainWindowViewModel.cs`
   - `SharedUI\ViewModel\WinUI\MainWindowViewModel.cs`
2. ❌ **Backslash vs Forward-Slash** - `get_file` akzeptiert nur `/`
3. ❌ **Relativer vs Absoluter Pfad** - Tool bevorzugt relative Pfade vom Solution-Root

#### **Lösungen für Copilot:**

```markdown
✅ **Richtige Strategie:**

1. Verwende `file_search` zuerst, um alle Vorkommen zu finden
2. Wenn mehrere Dateien existieren, verwende `run_command_in_terminal`:
   ```powershell
   Get-Content "SharedUI\ViewModel\MainWindowViewModel.cs"
   ```
3. Falls get_file nötig ist, verwende Forward-Slash:
   ```
   SharedUI/ViewModel/MainWindowViewModel.cs
   ```

❌ **Vermeide:**
- `get_file("Backend\Z21.cs")` → schlägt fehl
- Direkter Zugriff ohne vorherige file_search
```

---

### 2. **PowerShell Syntax-Fehler**

#### **Problem:**
- Befehle schlagen fehl: "Command failed", "Syntax error"
- Besonders bei Regex, Pipes, mehrzeiligen Befehlen

#### **Ursachen:**
1. ❌ **Komplexe Regex-Patterns** mit `-Pattern` funktionieren nicht zuverlässig
2. ❌ **Mehrzeilige Regex** mit `\n` in PowerShell
3. ❌ **Nested Pipes** `| ForEach-Object { $_ | Select-String }`

#### **Lösungen für Copilot:**

```markdown
✅ **Einfache, robuste Befehle:**

# Datei lesen (einfach)
Get-Content "Backend\Z21.cs"

# Bestimmte Zeilen
Get-Content "Backend\Z21.cs" | Select-Object -Skip 10 -First 20

# Nach Text suchen (einfach)
Select-String -Path "Backend\Z21.cs" -Pattern "ConnectAsync" -Context 5,5

# Zeilennummer finden
Select-String -Path "Backend\Z21.cs" -Pattern "class Z21" | Select-Object -First 1

❌ **Vermeide komplexe Regex:**
# BAD: Mehrzeilige Regex
$content = Get-Content "file.cs" -Raw
$content -match "class.*\n.*public"  # ❌ Fehleranfällig

# GOOD: Mehrere einfache Befehle
Get-Content "file.cs" | Select-String "class" -Context 0,5
```

---

### 3. **Build-Fehler: Fehlende `using`-Statements**

#### **Problem:**
- Nach Code-Änderungen: `CS0246: The type or namespace name 'X' could not be found`
- Fehlende `using`-Direktiven

#### **Ursachen:**
1. ❌ **Copilot fügt nur Code hinzu**, prüft nicht vorhandene Usings
2. ❌ **Neue Typen** aus anderen Namespaces werden verwendet
3. ❌ **Kein automatisches `using`-Management** in edit_file

#### **Lösungen für Copilot:**

```markdown
✅ **Checkliste vor edit_file:**

1. **Prüfe vorhandene usings:**
   ```powershell
   Get-Content "File.cs" | Select-Object -First 15 | Select-String "using"
   ```

2. **Identifiziere neue Typen** im Code:
   - `IZ21` → `using Moba.Backend.Interface;`
   - `Solution` → `using Moba.Backend.Model;`
   - `ObservableObject` → `using CommunityToolkit.Mvvm.ComponentModel;`

3. **Füge fehlende usings hinzu** BEVOR du Code änderst:
   ```csharp
   using Moba.Backend.Interface;
   using Moba.Backend.Model;
   using System.Threading.Tasks;
   
   // ...existing code...
   ```

4. **Nach edit_file: run_build** sofort ausführen!

✅ **Standard-Usings für MOBAflow:**

| Typ | Using |
|-----|-------|
| `IZ21`, `IJourneyManagerFactory` | `using Moba.Backend.Interface;` |
| `Solution`, `Project`, `Journey` | `using Moba.Backend.Model;` |
| `ObservableObject`, `RelayCommand` | `using CommunityToolkit.Mvvm.ComponentModel;` |
| `IIoService` | `using Moba.SharedUI.Service;` |
| `IUiDispatcher` | `using Moba.SharedUI.Service;` |
| `Task`, `CancellationToken` | `using System.Threading.Tasks;` |
```

---

### 4. **Unvollständige Implementierungen**

#### **Problem:**
- Code wird nur teilweise implementiert
- Methoden fehlen, Properties fehlen, EventHandler fehlt

#### **Ursachen:**
1. ❌ **Schrittweise Implementierung** ohne Gesamtbild
2. ❌ **Fehlende Synchronisation** zwischen XAML und Code-Behind
3. ❌ **Interface-Änderungen** werden nicht in allen Implementierungen nachgezogen

#### **Lösungen für Copilot:**

```markdown
✅ **Vollständige Implementierung in einem Schritt:**

**Bei Interface-Änderungen:**
1. ✅ Interface ändern (z.B. `IZ21.cs`)
2. ✅ Implementierung anpassen (z.B. `Z21.cs`)
3. ✅ ALLE Test-Stubs anpassen (z.B. `CounterViewModelTests.cs`)
4. ✅ Build ausführen → Fehler finden → alle beheben

**Bei neuen Commands:**
1. ✅ Command-Method im ViewModel
2. ✅ CanExecute-Method im ViewModel
3. ✅ NotifyCanExecuteChanged() Aufrufe
4. ✅ UI-Binding (XAML oder Razor)
5. ✅ Event-Handler (falls nötig)
6. ✅ Build + Test

**Bei UI-Änderungen:**
1. ✅ XAML/Razor ändern
2. ✅ Code-Behind anpassen (falls Event-Handler nötig)
3. ✅ ViewModel-Property/Command hinzufügen
4. ✅ Build ausführen
```

---

## ✅ **Best Practices für Copilot**

### **Workflow für Änderungen:**

```markdown
1️⃣ **Verstehen** - Existierende Struktur analysieren
   - file_search für relevante Dateien
   - get_file oder Get-Content für Inhalt
   - Abhängigkeiten identifizieren

2️⃣ **Planen** - Vollständige Änderungen definieren
   - Welche Dateien betroffen?
   - Welche Usings nötig?
   - Welche Tests anpassen?

3️⃣ **Implementieren** - Alle Änderungen auf einmal
   - Alle betroffenen Dateien editieren
   - Usings hinzufügen
   - Tests anpassen

4️⃣ **Validieren** - Sofort nach jeder Änderung
   - run_build ausführen
   - Fehler beheben
   - Erst dann weitermachen
```

### **Vermeidbare Fehler:**

| ❌ Fehler | ✅ Lösung |
|-----------|-----------|
| `get_file("Backend\Z21.cs")` | `Get-Content "Backend\Z21.cs"` |
| Komplexe Regex in PowerShell | Mehrere einfache Select-String |
| Interface ändern, Stubs vergessen | Checkliste: Interface → Impl → Stubs |
| Code ohne Usings hinzufügen | Usings VOR edit_file prüfen |
| Mehrere Änderungen ohne Build | Nach JEDER Datei → run_build |

---

## 📋 **Checkliste für jede Implementierung**

```markdown
Vor edit_file:
☐ Vorhandene Usings geprüft?
☐ Neue Usings identifiziert?
☐ Abhängige Dateien gefunden?

Nach edit_file:
☐ run_build ausgeführt?
☐ Compiler-Fehler behoben?
☐ Tests angepasst?

Vor finish_plan:
☐ Alle Dateien konsistent?
☐ Build erfolgreich?
☐ Keine Warnungen?
```

---

## 🔄 **Wenn ein Tool fehlschlägt:**

### **get_file schlägt fehl:**
```powershell
# Fallback 1: run_command_in_terminal
Get-Content "Pfad\Zur\Datei.cs"

# Fallback 2: file_search + Get-Content
# 1. Finde die Datei
file_search(["Dateiname.cs"])
# 2. Lese mit Get-Content
Get-Content "Gefundener\Pfad.cs"
```

### **PowerShell Befehl schlägt fehl:**
```powershell
# Vereinfachen:
# Statt: komplexer Pipe mit Regex
Get-Content "file.cs" -Raw | Select-String -Pattern "complex.*\nregex"

# Besser: Einfacher Befehl
Get-Content "file.cs" | Select-String "complex" -Context 5,5
```

### **Build schlägt fehl:**
```markdown
1. get_errors(["Pfad/Zur/Datei.cs"]) ausführen
2. Fehler analysieren:
   - CS0246 = Fehlende using
   - CS1003 = Syntax-Fehler (z.B. fehlendes Komma)
   - CS0535 = Interface nicht vollständig implementiert
3. Fehler beheben
4. Erneut run_build
```

---

## 📌 **Zusammenfassung**

### **Die 5 goldenen Regeln:**

1. ✅ **Verwende Get-Content statt get_file** bei Problemen
2. ✅ **Einfache PowerShell-Befehle** statt komplexer Regex
3. ✅ **Prüfe Usings VOR edit_file**
4. ✅ **run_build NACH JEDER Änderung**
5. ✅ **Vollständige Implementierung** (Interface + Impl + Stubs)

### **Wenn du unsicher bist:**

```markdown
1. file_search verwenden
2. Get-Content verwenden
3. Einfache Befehle verwenden
4. Nach jeder Änderung builden
5. Fehler sofort beheben
```
