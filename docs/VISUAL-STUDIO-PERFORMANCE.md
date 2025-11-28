# Visual Studio Performance-Einstellungen für MAUI

## 🚀 Sofort-Optimierungen

### 1. Parallelität erhöhen

**Tools → Options → Projects and Solutions → Build and Run**

```
Maximum number of parallel project builds: 8
```
→ Nutzt alle CPU-Cores

### 2. Hot Reload aktivieren

**Tools → Options → Debugging → Hot Reload**

```
☑ Enable Hot Reload and Edit and Continue when debugging
☑ Enable Hot Reload when starting without debugging
☑ Apply changes to source code while app is running
☑ Show XAML Hot Reload UI in XAML editor
```

### 3. Analyzer-Performance

**Tools → Options → Text Editor → C# → Advanced**

```
☐ Run background code analysis (nur bei Bedarf)
☑ Show live code issues
☑ Enable full solution analysis (nur bei kleinen Solutions!)
```

**Für große Projekte:**
```
☐ Enable full solution analysis
→ Spart RAM und CPU
```

### 4. MAUI-Specific

**Tools → Options → MAUI**

```
☑ Enable XAML Hot Reload
☑ Enable XAML IntelliSense
☐ Enable XAML designer (langsam, oft nicht nötig)
```

### 5. Android Deployment

**Projekt → MAUI Properties → Android Options → Advanced**

```
Fast deployment: ☑ Enabled
Use shared runtime: ☑ Enabled (nur Debug!)
Package format: APK (Debug) / AAB (Release)
```

### 6. NuGet Performance

**Tools → Options → NuGet Package Manager**

```
☑ Allow NuGet to download missing packages
☑ Automatically check for missing packages during build
Clear All NuGet Cache(s): [Button] ← Bei Problemen
```

---

## 💡 Workflow-Optimierungen

### Während der Entwicklung:

1. **Nutze Hot Reload** (Strg+Alt+F5)
   - XAML-Änderungen werden sofort angewendet
   - Kein Rebuild nötig

2. **Build nur betroffene Projekte**
   - Solution Explorer → Rechtsklick → Build (statt Solution Build)

3. **Incremental Build** (F6 statt Shift+Ctrl+B)
   - Baut nur geänderte Dateien

4. **Fast Deployment**
   - Shared Runtime (nur Debug!)
   - Kleinere APK, schnelleres Deployment

### Bei Problemen:

```powershell
# PowerShell im Repository-Root

# 1. Clean nur betroffenes Projekt
dotnet clean MAUI\MAUI.csproj

# 2. NuGet-Restore
dotnet restore

# 3. Rebuild
dotnet build MAUI\MAUI.csproj
```

---

## 🎯 Erwartete Verbesserungen

| Optimierung | Build-Zeit | Deploy-Zeit | RAM-Nutzung |
|-------------|------------|-------------|-------------|
| Parallele Builds | **-30%** | - | - |
| Hot Reload | **-90%** | **-95%** | - |
| Analyzer aus | **-15%** | - | **-20%** |
| Fast Deployment | - | **-50%** | - |
| Incremental Build | **-70%** | **-60%** | - |

**Gesamt bei idealem Setup:**
- Full Build: ~80s → ~50s (**37% schneller**)
- Incremental: ~45s → ~10s (**78% schneller**)
- Hot Reload: ~30s → ~3s (**90% schneller**)

---

## 📝 Checkliste: Visual Studio Setup

### Performance:
- [ ] Parallele Builds: 8 (oder CPU-Core-Anzahl)
- [ ] Hot Reload aktiviert
- [ ] Full Solution Analysis deaktiviert (große Projekte)
- [ ] Background Code Analysis optional
- [ ] XAML Designer deaktiviert (falls nicht benötigt)

### MAUI Android:
- [ ] Fast Deployment aktiviert
- [ ] Shared Runtime aktiviert (Debug)
- [ ] Single ABI (android-arm64)
- [ ] D8/R8 Dex-Compiler aktiviert
- [ ] Incremental Build aktiviert

### NuGet:
- [ ] Auto-Download aktiviert
- [ ] Cache-Pfad auf SSD
- [ ] Bei Problemen: Cache leeren

---

## 🔧 PowerShell-Hilfsskript

Speichern als `build-fast.ps1`:

```powershell
# Schneller MAUI-Build mit Optimierungen

param(
    [switch]$Clean,
    [switch]$Deploy
)

$project = "MAUI\MAUI.csproj"
$config = "Debug"
$framework = "net10.0-android36.0"

Write-Host "🚀 Fast MAUI Build" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "🧹 Cleaning..." -ForegroundColor Yellow
    dotnet clean $project -f $framework
}

Write-Host "🔨 Building..." -ForegroundColor Yellow
$buildTime = Measure-Command {
    msbuild $project /t:Build /p:Configuration=$config /p:TargetFramework=$framework /v:minimal /m
}

Write-Host "✅ Build completed in $($buildTime.TotalSeconds) seconds" -ForegroundColor Green

if ($Deploy) {
    Write-Host "📱 Deploying..." -ForegroundColor Yellow
    $deployTime = Measure-Command {
        msbuild $project /t:Install /p:Configuration=$config /p:TargetFramework=$framework /v:minimal
    }
    Write-Host "✅ Deploy completed in $($deployTime.TotalSeconds) seconds" -ForegroundColor Green
}

Write-Host "🎉 Total: $($buildTime.TotalSeconds + $deployTime.TotalSeconds) seconds" -ForegroundColor Cyan
```

**Verwendung:**
```powershell
# Schneller Build
.\build-fast.ps1

# Mit Clean
.\build-fast.ps1 -Clean

# Mit Deployment
.\build-fast.ps1 -Deploy

# Alles
.\build-fast.ps1 -Clean -Deploy
```

---

## 📚 Siehe auch

- `docs/MAUI-BUILD-PERFORMANCE.md` - Detaillierte Build-Optimierungen
- `MAUI/MAUI.csproj` - Build-Konfiguration
- [Visual Studio Performance Tips](https://learn.microsoft.com/en-us/visualstudio/ide/optimize-visual-studio-performance)
