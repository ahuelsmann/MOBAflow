# Konzept: F0–F31 Command-Refactor (symbol-only Mapping)

Design-Konzept, um die fünffache 32-fach-Duplikation im `TrainControlViewModel` durch eine collection-basierte Struktur zu ersetzen. Das pro-Lok-Mapping beschränkt sich bewusst auf das **Symbol** je Funktion — was F0–F31 tatsächlich auslösen, bestimmt der Decoder in der Lok.

## 1. Hintergrund & Problem

- **DCC-Funktionen F0–F31** sind Befehle an eine Lok-Adresse. Was eine Funktion auslöst, **entscheidet der Decoder in der Lok** — MOBAflow muss diese Bedeutung nicht kennen.
- **Pro-Lok-Bedarf = nur das richtige Symbol.** Das Datenmodell dafür existiert bereits: `Domain.Locomotive.FunctionSymbols` (SVG-Dateinamen je Index). Es braucht **kein** neues Domain-Feld (kein Label, kein Tastentyp).
- Das eigentliche Problem ist reine **Wartbarkeit**: Im `TrainControlViewModel` ist die Funktionslogik fünffach dupliziert (siehe unten), je 32-fach von Hand ausgeschrieben.

### Bestehende Duplikation (`SharedUI/ViewModel/TrainControlViewModel.cs`)
| Block | Form | Zeilen (ca.) |
| ----- | ---- | ------------ |
| `IsF0On`..`IsF31On` | 32x `[ObservableProperty]` | ~338–470 |
| `ToggleF0Async`..`ToggleF31Async` | 32x `[RelayCommand]` | ~1731–1828 |
| `[NotifyCanExecuteChangedFor]` auf `LocoAddress` | 32x Attribut | ~76–107 |
| `Function0Glyph`..`Function31Glyph` | 32x Property | ~551–613 |
| `GetFunctionState`/`SetFunctionState` | 2x 32-Wege-`switch` | ~1859–1933 |

### Relevante bestehende Bausteine
- `@/Users/.../Domain/Locomotive.cs` → `FunctionSymbols: List<string>?` (Icon je Index, persistiert).
- `@/Users/.../Common/Configuration/LocomotivePreset.cs` → `FunctionStates: uint` Bitmaske + `Get/SetFunction(int)`.
- `@/Users/.../MOBAflow/View/TrainControlPage.xaml` → 32 ToggleButtons mit `IsF#On` + `ToggleF#Command` + `Function#Glyph`.

## 2. Ziele / Nicht-Ziele

**Ziele**
- Collection-basierte ViewModel-Struktur statt fünffacher 32-fach-Duplikation.
- Symbol pro Lok bleibt erhalten (Quelle: bestehendes `Locomotive.FunctionSymbols`).
- Verhalten bleibt identisch (gleiche Befehle an die Z21, gleiche Persistenz).
- Plattformneutral (View-ViewModel in `SharedUI`), testbar in `Test/`.

**Nicht-Ziele**
- **Kein neues Domain-Modell** — `FunctionSymbols` reicht (symbol-only).
- Keine Labels/Beschreibungen, kein Tastentyp (Toggle/Momentary).
- Keine Änderung am Z21-/DCC-Protokoll.
- Keine Persistenz-Änderung (`FunctionSymbols` + `LocomotivePreset.FunctionStates` bleiben).

## 3. Datenmodell

**Kein neues Domain-Feld nötig.** Das Symbol pro Lok liegt bereits in:

```csharp
// Domain/Locomotive.cs (unverändert)
public List<string>? FunctionSymbols { get; set; }   // Index 0..31 → SVG-Dateiname
```

Die heutige Default-/Validierungslogik (`DefaultFunctionAssets`, `IsValidAssetReference`,
`GetFunctionGlyph`, `SetFunctionSymbol`) im `TrainControlViewModel` bleibt erhalten und wird
lediglich von der neuen Collection genutzt statt von 32 Einzel-Properties.

## 4. ViewModel-Refactor (collection-basiert)

Neuer Item-ViewModel + Collection ersetzen die fünf duplizierten Blöcke:

```csharp
// SharedUI/ViewModel/FunctionButtonViewModel.cs
public sealed partial class FunctionButtonViewModel : ObservableObject
{
    public int Index { get; }                 // 0..31
    public string Label { get; }              // "F0".. "F31" (nur Anzeige der Tastennummer)
    public string BacklightColorHex { get; }  // pro Taste fixe Hintergrundfarbe (aus XAML migriert)
    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private string _iconAsset;  // SVG, aus FunctionSymbols/Defaults
}
```

`Label` ("F0") und `BacklightColorHex` sind die heute fest in der XAML kodierten Konstanten —
sie wandern in das Item, damit die View datengetrieben werden kann.

Im `TrainControlViewModel`:
- `public ObservableCollection<FunctionButtonViewModel> Functions { get; }` mit 32 Items (Index 0–31).
- **Ein** parametrisierter Command statt 32:
  ```csharp
  [RelayCommand(CanExecute = nameof(CanExecuteLocoCommand))]
  private Task ToggleFunctionAsync(int index) => ToggleFunctionCoreAsync(index);
  ```
- `GetFunctionState/SetFunctionState`-Switches entfallen → Zugriff über `Functions[index].IsOn`.
- `Function#Glyph`-Properties entfallen → `Functions[index].IconAsset` (gespeist aus `GetFunctionGlyph`).
- `NotifyCanExecuteChangedFor`-Block entfällt → bei `LocoAddress`-Change einmal `ToggleFunctionCommand.NotifyCanExecuteChanged()`.
- Preset-Sync (`ApplyCurrentPreset`/`SaveCurrentStateToPreset`/Runtime-Apply) iteriert über `Functions` statt 0..31-Switch — bleibt mit `LocomotivePreset.FunctionStates` (Bitmaske) kompatibel.

**Erwartete Reduktion:** ~250–300 Zeilen Boilerplate entfallen.

## 5. XAML-Auswirkung (`TrainControlPage.xaml`)

- 32 fest verdrahtete ToggleButtons → **`ItemsRepeater`** (2-spaltiges `UniformGridLayout`) über `Functions` mit DataTemplate:
  - `IsChecked="{x:Bind IsOn, Mode=TwoWay}"`
  - `Command="{Binding DataContext.ToggleFunctionCommand, ElementName=...}" CommandParameter="{x:Bind Index}"`
  - Icon aus `IconAsset`, Beschriftung aus `Label`.
  - Hintergrund per `x:Bind`-Funktionsbinding `BacklightBrush(IsOn, BacklightColorHex)` (Brush-Erzeugung im WinUI-Page-Code, da `SharedUI` keine UI-Typen kennen darf).
- `CommandParameter` ist ein echter `int` (kein String-Konvertierungsproblem).
- Code-behind `FunctionButton_RightTapped` liest den Index künftig aus dem Item statt aus `Tag`.
- **Blast-Radius:** Nur `TrainControlPage.xaml` (WinUI). **Kein MAUI** bindet diese Properties (verifiziert). Nur auf Windows baubar.

## 6. Persistenz

- **Unverändert.** `Locomotive.FunctionSymbols` (Symbole) und `LocomotivePreset.FunctionStates` (Schaltzustand-Bitmaske) bleiben wie heute. Keine Schema-Änderung, keine Migration.

## 7. Umsetzungsschritte

1. **SharedUI:** `FunctionButtonViewModel` (Index, Label, BacklightColorHex, IsOn, IconAsset).
2. **SharedUI:** `TrainControlViewModel` auf `Functions`-Collection + ein `ToggleFunctionCommand(int)` umstellen; 5 Duplikat-Blöcke entfernen; Preset-/Runtime-Sync auf Collection. Tests anpassen/ergänzen.
3. **WinUI XAML:** `TrainControlPage.xaml` auf `ItemsRepeater` umstellen, `FunctionButton_RightTapped` anpassen. Windows-Build verifizieren.

## 8. Tests

- `TrainControlViewModel`: `ToggleFunctionAsync(i)` setzt `Functions[i].IsOn` und sendet `SetLocomotiveFunctionAsync(addr, i, state)`.
- Preset-Bitmaske bleibt nach Toggle konsistent.
- Default-Symbol (F0 Licht, F1 Sound, sonst leer) korrekt in `Functions[i].IconAsset`.
- `CanExecute` toggelt korrekt bei gültiger/ungültiger `LocoAddress`.

## 9. Risiken

- **XAML-Umbau** ist der Hauptaufwand, nur auf Windows (WinUI) verifizierbar.
- Visuelle Regression im 2-spaltigen Layout (UniformGridLayout vs. heutiges Grid) → per Build + Sichtprüfung kontrollieren.

---

**Empfehlung:** VM-Refactor (Schritt 1–2) und XAML (Schritt 3) gehören in **einen** Change, da die XAML direkt an die heutigen 32 Properties bindet und sonst nicht kompiliert.
