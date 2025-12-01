# TODO - Finale Schritte vor VS-Neustart

**Datum**: 2025-01-01  
**Priorität**: HOCH

---

## ✅ Bereits erledigt

- ✅ Domain-Projekt erstellt (31 POCOs, keine Abhängigkeiten)
- ✅ Alle `Backend.Model.*` → `Moba.Domain.*` ersetzt
- ✅ MAUI MauiProgram.cs korrigiert (`Domain.Solution`)
- ✅ WinUI App.xaml.cs neu erstellt mit DI
- ✅ WinUI IoService.cs FileDialog-API aktualisiert
- ✅ Dokumentation erstellt

---

## 🔧 TODO nach VS-Neustart

### 1️⃣ Clean Build (ZUERST!)

```powershell
# Visual Studio SCHLIESSEN (alle Instanzen)

# Build-Cache löschen
Get-ChildItem -Path "C:\Repos\ahuelsmann\MOBAflow" -Include bin,obj -Recurse -Force | Remove-Item -Recurse -Force

# Visual Studio NEU ÖFFNEN
```

---

### 2️⃣ Test-Dateien korrigieren

**3 Dateien** ändern:
- `Test\Backend\StationManagerTests.cs`
- `Test\Backend\WorkflowTests.cs`
- `Test\Backend\WorkflowManagerTests.cs`

**Änderung**: Suchen & Ersetzen
```
Suchen:    Moba.Domain.Action.
Ersetzen:  Moba.Backend.Action.
```

**Warum?**: Action-Klassen sind in Backend, nicht in Domain!

---

### 3️⃣ WinUI App.xaml.cs korrigieren

**Datei**: `WinUI\App.xaml.cs`  
**Zeilen**: 43-51

**Problem**: Interface-Namespaces sind falsch

**ALT** (Zeilen 43-51):
```csharp
services.AddSingleton<Backend.IZ21, Backend.Z21>();
services.AddSingleton<Backend.IUdpClientWrapper, Backend.UdpClientWrapper>();
services.AddSingleton<Domain.Solution>();

services.AddSingleton<Service.IIoService, Service.IoService>();
services.AddSingleton<Service.INotificationService, Service.NotificationService>();
services.AddSingleton<Service.IPreferencesService, Service.PreferencesService>();
services.AddSingleton<Service.IUiDispatcher, Service.UiDispatcher>();
services.AddSingleton<Service.HealthCheckService>();
```

**NEU**:
```csharp
// Backend Services
services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
services.AddSingleton<Backend.Interface.IUdpClientWrapper, Backend.UdpClientWrapper>();
services.AddSingleton<Domain.Solution>();

// WinUI Services (Interfaces sind in SharedUI!)
services.AddSingleton<SharedUI.Service.IIoService, Service.IoService>();
services.AddSingleton<SharedUI.Service.INotificationService, Service.NotificationService>();
services.AddSingleton<SharedUI.Service.IPreferencesService, Service.PreferencesService>();
services.AddSingleton<SharedUI.Service.IUiDispatcher, Service.UiDispatcher>();
services.AddSingleton<Service.HealthCheckService>();
```

---

### 4️⃣ Rebuild All

Nach den Korrekturen:
1. **Build** → **Rebuild Solution**
2. Prüfen auf Fehler

**Erwartung**: ✅ Build erfolgreich (außer 1x mt.exe Warnung)

---

## 📚 Optionales Cleanup

### Dokumentation aufräumen

**Datei**: `docs/DOCS-CLEANUP-RECOMMENDATIONS.md`

**Empfehlung**: 38 alte Session-Reports & abgeschlossene Tasks nach `docs/archive/` verschieben

**Vorher**: 57 Markdown-Dateien  
**Nachher**: 19 Kern-Dokumentations-Dateien

---

## 🎯 Checkliste

- [ ] VS geschlossen
- [ ] Build-Cache gelöscht (`bin/obj`)
- [ ] VS neu geöffnet
- [ ] Test-Dateien korrigiert (`Domain.Action` → `Backend.Action`)
- [ ] WinUI App.xaml.cs korrigiert (Interface-Namespaces)
- [ ] Rebuild All erfolgreich ✅
- [ ] Optional: Docs aufgeräumt

---

## 📖 Referenz-Dokumentation

- **Architecture**: `docs/CLEAN-ARCHITECTURE-FINAL-STATUS.md`
- **Build-Status**: `docs/BUILD-ERRORS-STATUS.md`
- **Docs Cleanup**: `docs/DOCS-CLEANUP-RECOMMENDATIONS.md`
- **Copilot Instructions**: `.github/copilot-instructions.md`

---

**Nach erfolgreicher Korrektur**: Projekt ist bereit für Development! 🚀
