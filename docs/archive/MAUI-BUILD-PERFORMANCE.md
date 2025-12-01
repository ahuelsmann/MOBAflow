# MAUI Build Performance Optimization

## Aktuelle Optimierungen

### Debug Build (Entwicklung)
```xml
<!-- ⚡ Deaktiviert in Debug (spart 10-30% Build-Zeit) -->
<RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
<RunAnalyzersDuringLiveAnalysis>false</RunAnalyzersDuringLiveAnalysis>
<GenerateDocumentationFile>false</GenerateDocumentationFile>

<!-- ⚡ Paralleles Build (nutzt alle CPU-Cores) -->
<MaxCpuCount>0</MaxCpuCount>

<!-- ⚡ Fast Deployment (Shared Runtime) -->
<AndroidUseSharedRuntime>true</AndroidUseSharedRuntime>
<EmbedAssembliesIntoApk>false</EmbedAssembliesIntoApk>

<!-- ⚡ Nur eine ABI (android-arm64 statt alle) -->
<RuntimeIdentifiers>android-arm64</RuntimeIdentifiers>

<!-- ⚡ D8/R8 (schnellerer Dex-Compiler) -->
<AndroidDexTool>d8</AndroidDexTool>
<AndroidEnableD8R8>true</AndroidEnableD8R8>

<!-- ⚡ Incremental Build -->
<AndroidUseIncrementalNativeLibraryBuild>true</AndroidUseIncrementalNativeLibraryBuild>
<AndroidUseIncrementalManifestMerge>true</AndroidUseIncrementalManifestMerge>
```

---

## 🚀 Weitere Optimierungen

### 1. MSBuild Binary Log (Build-Analyse)

Finden Sie heraus, welche Tasks langsam sind:

```powershell
# Build mit Binary Log
msbuild MAUI\MAUI.csproj /t:Build /bl:build.binlog /p:Configuration=Debug /p:TargetFramework=net10.0-android36.0

# Analysieren mit MSBuild Structured Log Viewer
# Download: https://msbuildlog.com/
# Zeigt detailliert, welche Tasks Zeit kosten
```

### 2. NuGet Package Cache optimieren

```powershell
# NuGet-Cache löschen (manchmal beschleunigt das Restores)
dotnet nuget locals all --clear

# Packages im Projekt cachen
dotnet restore --force
```

### 3. Visual Studio Optionen

**Tools → Options → Debugging:**
- ☑ Enable Just My Code
- ☑ Suppress JIT optimization on module load (only for debugging)
- ☐ Enable Diagnostic Tools while debugging

**Tools → Options → Projects and Solutions → Build and Run:**
- Maximum number of parallel project builds: **8** (oder Anzahl CPU-Cores)
- Only build startup projects and dependencies on Run: **☑**

**Tools → Options → MAUI:**
- Enable Hot Reload: **☑** (schnellere Iterationen ohne Rebuild)
- Enable XAML Hot Reload: **☑**

### 4. Android Emulator vs. Physisches Gerät

**Physisches Gerät (empfohlen):**
- ✅ 2-3x schnelleres Deployment
- ✅ Realistischere Performance-Tests
- ✅ Weniger Overhead

**Emulator:**
- ❌ Langsamer beim Deployment
- ❌ Mehr RAM-Verbrauch
- ✅ Gut für UI-Tests verschiedener Geräte

### 5. Fast Deployment aktivieren (Visual Studio)

**Projekt → [MAUI] Properties → Android Options:**
- Fast Deployment: **☑ Enabled**
- Use Shared Runtime: **☑ Enabled** (nur Debug!)
- Link: **None** (Debug) / **SDK Assemblies Only** (Release)

### 6. Git Performance

```powershell
# Git LFS für große Binärdateien (falls vorhanden)
git lfs track "*.dll" "*.exe" "*.apk" "*.aab"

# Git sparse-checkout (nur relevante Dateien)
git config core.sparseCheckout true
```

### 7. SSD statt HDD

- ✅ **NVMe SSD** empfohlen
- ✅ Projekt auf SSD (nicht Netzlaufwerk!)
- ✅ NuGet-Cache auf SSD

### 8. Incremental Build prüfen

```powershell
# Clean nur wenn nötig (nicht jedes Mal!)
# Incremental Build ist 5-10x schneller

# Nur bei Problemen cleanen:
dotnet clean MAUI\MAUI.csproj
```

---

## 📊 Erwartete Build-Zeiten

| Szenario | Ohne Optimierung | Mit Optimierung | Verbesserung |
|----------|------------------|-----------------|--------------|
| **Full Build** | ~120s | ~80s | **~33% schneller** |
| **Incremental** | ~45s | ~15s | **~67% schneller** |
| **Hot Reload** | ~30s | ~5s | **~83% schneller** |
| **Deployment** | ~40s | ~20s | **~50% schneller** |

---

## 🎯 Sofort-Tipps für den Alltag

### Während der Entwicklung:

1. **Hot Reload nutzen** statt Rebuild
   - XAML-Änderungen → Strg+S (automatisch aktualisiert)
   - C#-Änderungen → Oft auch Hot Reload möglich

2. **Incremental Build** (nicht Clean!)
   - Nur `Build` statt `Rebuild`
   - Clean nur bei Problemen

3. **Nur ein ABI** (android-arm64)
   - Nicht alle ABIs bauen in Debug

4. **Physisches Gerät** verwenden
   - Schnelleres Deployment als Emulator

5. **Fast Deployment** aktiviert lassen
   - Shared Runtime in Debug

### Bei Problemen:

```powershell
# 1. NuGet-Cache leeren
dotnet nuget locals all --clear

# 2. Obj/Bin-Ordner löschen
Remove-Item -Recurse -Force MAUI\obj, MAUI\bin

# 3. Rebuild
msbuild MAUI\MAUI.csproj /t:Rebuild /p:Configuration=Debug
```

---

## 🔍 Performance-Analyse Tools

1. **MSBuild Structured Log Viewer**
   - Download: https://msbuildlog.com/
   - Zeigt detailliert, wo Zeit verloren geht

2. **Visual Studio Build Timing**
   - View → Output → Build
   - Zeigt Build-Dauer pro Projekt

3. **Android Profiler**
   - View → Other Windows → Android Device Monitor
   - Zeigt Deployment-Performance

---

## 📚 Siehe auch

- [MAUI Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/maui/deployment/performance)
- [Android Build Performance](https://learn.microsoft.com/en-us/xamarin/android/deploy-test/building-apps/build-performance)
- `MAUI.csproj` - Aktuelle Build-Konfiguration
