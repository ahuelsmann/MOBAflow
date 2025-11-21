# Copyright Headers - Anleitung

Dieses Dokument erklärt, wie du Copyright-Header zu allen C#-Dateien hinzufügen kannst.

---

## 🚀 Schnellstart

### Option 1: Mit PowerShell-Script (Empfohlen)

1. **Öffne PowerShell** im Repository-Root:
   ```powershell
   cd C:\Repos\ahuelsmann\MOBAflow
   ```

2. **Dry-Run (Vorschau ohne Änderungen):**
   ```powershell
   .\add-copyright-headers.ps1 -WhatIf
   ```

3. **Tatsächlich ausführen:**
   ```powershell
   .\add-copyright-headers.ps1
   ```

4. **Ergebnis prüfen:**
   ```powershell
   dotnet build
   ```

---

## 📋 Was macht das Script?

### ✅ Verarbeitet:
- Alle `.cs`-Dateien im Repository
- Nur Dateien ohne bestehendes Copyright-Header

### ⏭️ Überspringt:
- Dateien in `bin/`, `obj/`, `.vs/`, `.idea/`, `packages/`, `.nuget/`
- Auto-generierte Dateien:
  - `*AssemblyInfo.cs`
  - `*GlobalUsings.g.cs`
  - `*.AssemblyAttributes.cs`
  - `*.Designer.cs`
  - `TemporaryGeneratedFile_*.cs`

### 📝 Fügt hinzu:
```csharp
// Copyright (c) 2025 Andreas Huelsmann
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Moba.Backend.Protocol;
// ... restlicher Code
```

---

## 🎯 Erwartete Ausgabe

```
🔍 Searching for C# files in: C:\Repos\ahuelsmann\MOBAflow

📊 Found 243 C# files to process

✅ ADDED: C:\Repos\ahuelsmann\MOBAflow\Backend\Z21.cs
✅ ADDED: C:\Repos\ahuelsmann\MOBAflow\Backend\Protocol\Z21Protocol.cs
⏭️  SKIP: C:\Repos\ahuelsmann\MOBAflow\Backend\obj\Debug\net10.0\Backend.AssemblyInfo.cs
...

═══════════════════════════════════════════════════════════
📊 SUMMARY
═══════════════════════════════════════════════════════════
✅ Processed: 238 files
⏭️  Skipped:   5 files (already have copyright)
❌ Errors:    0 files
📁 Total:     243 files
═══════════════════════════════════════════════════════════

✅ Done!
```

---

## 🛠️ Manuelle Bearbeitung (Optional)

Falls du einzelne Dateien manuell bearbeiten möchtest:

### In Visual Studio 2026:
1. Öffne die Datei
2. Drücke `Ctrl+K, Ctrl+D` (Format Document)
3. ReSharper/Rider fügt automatisch Header ein (wenn `.editorconfig` aktiv ist)

### In Rider:
1. `Code` → `Reformat Code` → `Update File Header`
2. Oder: `Ctrl+Alt+L` → ✅ "Update file header"

---

## 🔄 Rückgängig machen (Falls nötig)

Wenn etwas schiefgeht:

```powershell
# Git Reset (falls noch nicht committed)
git checkout .

# Oder spezifische Dateien:
git checkout Backend/Z21.cs
```

---

## ✅ Nach der Ausführung

1. **Build prüfen:**
   ```powershell
   dotnet build
   ```

2. **Tests ausführen:**
   ```powershell
   dotnet test
   ```

3. **Git Status prüfen:**
   ```powershell
   git status
   git diff
   ```

4. **Committen:**
   ```powershell
   git add .
   git commit -m "Add copyright headers to all C# files"
   ```

---

## 📊 Statistiken (Schätzung)

- **Backend**: ~60 Dateien
- **SharedUI**: ~40 Dateien
- **WinUI**: ~30 Dateien
- **MAUI**: ~25 Dateien
- **WebApp**: ~20 Dateien
- **Sound**: ~15 Dateien
- **Test**: ~50 Dateien
- **Common**: ~5 Dateien

**Gesamt**: ~245 Dateien

---

## ⚠️ Wichtig

- Das Script ändert **nur** Dateien ohne bestehendes Copyright-Header
- Auto-generierte Dateien werden **übersprungen**
- **Backup nicht nötig** (Git kann alles rückgängig machen)
- **Build erfolgreich** = Alles ist gut! ✅

---

## 🐛 Probleme?

Falls etwas nicht funktioniert:

1. **PowerShell Execution Policy:**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
   ```

2. **Pfad anpassen:**
   ```powershell
   .\add-copyright-headers.ps1 -RootPath "C:\Dein\Pfad\MOBAflow"
   ```

3. **Einzelne Datei manuell:**
   ```powershell
   # Am Anfang der Datei einfügen:
   // Copyright (c) 2025 Andreas Huelsmann
   // Licensed under the MIT License. See LICENSE file in the project root.
   ```

---

## 🎉 Fertig!

Nach der Ausführung haben alle C#-Dateien professionelle Copyright-Header! 🚀
