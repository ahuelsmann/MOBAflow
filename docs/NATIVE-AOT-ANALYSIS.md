# Native AOT Readiness Analyse - MOBAflow WinUI

## Zusammenfassung

Diese Analyse bewertet die Bereitschaft der MOBAflow-Codebasis für Native AOT-Kompilierung mit .NET 10 und Windows App SDK 2.0.1.

---

## Blocker: Hohes Risiko

### 1. Win2D (Microsoft.Graphics.Win2D)
**Status:** ⚠️ Blocker für vollständige AOT-Unterstützung

- **Verwendung:** `TrackPlanPage.xaml.cs`, `SignalBoxCanvasControl.xaml.cs`, `PathToCanvasGeometryConverter.cs`
- **Problem:** Win2D verwendet COM-Interop und native Direct2D-Bindings, die nicht AOT-kompatibel sind
- **Impact:** TrackPlan-Editor und SignalBox-Visualisierung würden nicht funktionieren

**Empfohlene Lösungsansätze:**
1. Win2D auf Version 1.2.0+ aktualisieren (verbesserte AOT-Unterstützung)
2. Alternative: SkiaSharp für AOT-fähige 2D-Grafiken evaluieren
3. Conditional Compilation: `#if AOT` für alternative Rendering-Implementierung

---

## Warnungen: Mittleres Risiko

### 2. JSON Serialisierung
**Status:** ⚠️ Konfiguration erforderlich

- **Verwendung:** `MasterDataStore.cs`, `Solution.cs`, `AppSettings.cs`
- **Aktueller Stand:** `System.Text.Json` wird verwendet

**Erforderliche Änderungen:**
```csharp
// Source Generators für JSON Serialisierung aktivieren
[JsonSerializable(typeof(MasterDataStore))]
[JsonSerializable(typeof(Solution))]
internal partial class MobaJsonContext : JsonSerializerContext { }
```

**Dateien zu prüfen:**
- `Backend/Data/MasterDataStore.cs` - Zeile 7: `System.Text.Json`
- `Common/Configuration/AppSettings.cs`

### 3. Reflection Usage
**Status:** ⚠️ Manuelle Prüfung erforderlich

**Kritische Patterns:**
- `Activator.CreateInstance<T>()` - muss durch `new()` constraints ersetzt werden
- `Assembly.GetCallingAssembly()` - nicht AOT-kompatibel
- `Type.GetType()` mit string-Namen - erfordert dynamische Typen

**Suchmuster im Code:**
```bash
# Suche nach Reflection-Usage
grep -r "Activator\." --include="*.cs" src/
grep -r "Assembly\." --include="*.cs" src/
grep -r "Type\.GetType" --include="*.cs" src/
```

### 4. Dynamische Code-Generierung
**Status:** ⚠️ Prüfung erforderlich

**Bereiche zu untersuchen:**
- XAML-Code-Behind Generierung (vom Compiler generiert, normalerweise sicher)
- CommunityToolkit.Mvvm Source Generators (bereits AOT-kompatibel)
- EventBus Delegate-Handling

---

## Erfolgreich: Geringes Risiko

### 5. Windows App SDK 2.0.1 APIs
**Status:** ✅ Vollständig AOT-kompatibel

Alle implementierten Refactorings sind AOT-kompatibel:
- `TitleBar.PreferredHeightOption` ✅
- `InputActivationListener` ✅
- `DragUIOverride` ✅
- `DispatcherQueuePriority` ✅
- `AppWindow.Close()` ✅

### 6. CommunityToolkit.Mvvm
**Status:** ✅ Native AOT unterstützt

- Source Generators statt Reflection
- `[ObservableProperty]`, `[RelayCommand]` sind AOT-fähig

### 7. Dependency Injection
**Status:** ✅ Microsoft.Extensions.DependencyInjection

- `Microsoft.Extensions.DependencyInjection` 9.0+ unterstützt AOT
- Konstruktor-Injection ist AOT-freundlich

---

## Empfohlene Build-Konfiguration

### Projekt-Datei Anpassungen (MOBAflow.csproj)

```xml
<PropertyGroup>
  <!-- Native AOT für Windows x64 -->
  <PublishAot>true</PublishAot>
  <PublishTrimmed>true</PublishTrimmed>
  
  <!-- Trim-Modus: partial für bessere Kompatibilität -->
  <TrimMode>partial</TrimMode>
  
  <!-- IL-Trimming Warnungen als Fehler behandeln -->
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
  <TrimmerSingleWarn>false</TrimmerSingleWarn>
  
  <!-- Win2D-spezifische Workarounds -->
  <EnableUnsafeBinaryFormatterSerialization>false</EnableUnsafeBinaryFormatterSerialization>
</PropertyGroup>

<ItemGroup>
  <!-- Source Generators für JSON -->
  <PackageReference Include="System.Text.Json" Version="9.0.0">
    <IsTrimmable>false</IsTrimmable>
  </PackageReference>
</ItemGroup>
```

---

## Test-Plan für AOT-Validierung

### Phase 1: Trim-Warnungen analysieren
```bash
dotnet publish MOBAflow/MOBAflow.csproj \
  -c Release \
  -r win-x64 \
  -p:PublishTrimmed=true \
  -p:SuppressTrimAnalysisWarnings=false
```

### Phase 2: Smoke-Tests
| Feature | Test | Erwartet |
|---------|------|----------|
| Z21 Verbindung | Connect/Disconnect | ✅ Funktioniert |
| TrackPlan Editor | Öffnen/Speichern | ⚠️ Win2D-Probleme |
| JSON Laden/Speichern | Solution laden | ✅ Mit Source Generators |
| Drag/Drop | JourneysPage | ✅ Funktioniert |
| TitleBar | Höhe/Subtitle | ✅ Funktioniert |

### Phase 3: Performance-Benchmarks
- Startup-Zeit messen (Ziel: <500ms mit AOT)
- Memory-Footprint vergleichen
- Binary-Größe: 30-50% kleiner als JIT

---

## Roadmap-Empfehlung

### Kurzfristig (dieser Sprint)
1. ✅ Windows App SDK 2.0.1 APIs implementieren (abgeschlossen)
2. JSON Source Generators hinzufügen
3. Trim-Analyse durchführen

### Mittelfristig (nächster Sprint)
1. Win2D-AOT-Kompatibilität evaluieren
2. Fallback-Rendering ohne Win2D implementieren
3. AOT-Build-Pipeline in CI/CD integrieren

### Langfristig
1. Vollständige Native AOT-Unterstützung
2. Optional: SkiaSharp-Migration für TrackPlan
3. Publish-Single-File mit AOT

---

## Zusammenfassung der Risiken

| Kategorie | Risiko | Impact | Aufwand |
|-----------|--------|--------|---------|
| Win2D | 🔴 Hoch | TrackPlan nicht funktional | 2-3 Wochen |
| JSON | 🟡 Mittel | Laufzeitfehler möglich | 2-3 Tage |
| Reflection | 🟡 Mittel | Potentielle Crashes | 1 Woche |
| WinAppSDK APIs | 🟢 Niedrig | Keine Probleme | ✅ Abgeschlossen |

---

*Analyse erstellt: Mai 2026*
*Windows App SDK Version: 2.0.1*
*.NET Version: 10.0.300-preview*
