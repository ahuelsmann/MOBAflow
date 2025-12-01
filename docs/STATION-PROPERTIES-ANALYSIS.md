# Station Properties - Clean Architecture Refactoring Auswirkung

**Datum**: 2025-12-01  
**Status**: ✅ Korrekt vereinfacht - Properties gehören zu Platform (zukünftiges Feature)

---

## 🎯 Konzept-Klärung

### Hierarchie
```
Station (Bahnhof/Haltestelle)
├── Platform 1 (Bahnsteig/Gleis 1)
│   ├── Track: "1"
│   ├── IsExitOnLeft: true
│   └── Arrival/Departure: ...
├── Platform 2 (Bahnsteig/Gleis 2)
└── Platform 3 (Bahnsteig/Gleis 3)
```

### MVP-Strategie
- **Phase 1 (aktuell)**: Nur **Stations** - vereinfachte Ansicht ohne Platforms
- **Phase 2 (später)**: **Platforms** pro Station - erweiterte Ansicht mit Details

**Die entfernten Properties gehören zur Platform-Ebene, die noch nicht implementiert ist!**

---

## 🔍 Was ist passiert?

### Entfernte XAML-Bindings (ProjectConfigurationPage.xaml)

Folgende Properties wurden aus der **Stations-ListView** entfernt:

```xaml
<!-- ENTFERNT - Gehören zu Platform (Phase 2) -->
<TextBlock Text="{x:Bind Number, Mode=OneWay}" />        <!-- Gleis-Nummer -->
<TextBlock Text="{x:Bind Arrival, Mode=OneWay}" />       <!-- Ankunftszeit/Gleis -->
<TextBlock Text="{x:Bind Departure, Mode=OneWay}" />     <!-- Abfahrtszeit/Gleis -->
<CheckBox IsChecked="{x:Bind IsExitOnLeft, Mode=OneWay}" /> <!-- Gleis-Orientierung -->
```

### ✅ Grund für Entfernung: Korrekt!
Diese Properties gehören zur **Platform**-Ebene, nicht zur **Station**-Ebene.  
Da Phase 1 nur Stations behandelt, ist die Entfernung **korrekt**.

---

## 📊 Aktuelle Domain-Struktur (Phase 1)

### Station (Domain\Station.cs)
```csharp
public class Station
{
    public string Name { get; set; }                    // ✅ Station-Name
    public string? Description { get; set; }            // ✅ Beschreibung
    public List<Platform> Platforms { get; set; }       // ⚠️ Noch nicht verwendet (Phase 2)
    public int? FeedbackInPort { get; set; }            // ✅ Feedback InPort
    public int NumberOfLapsToStop { get; set; }         // ✅ Runden bis Halt
    public Workflow? Flow { get; set; }                 // ✅ Workflow-Referenz
}
```

**Aktuell genutzte Properties (Phase 1):**
- ✅ `Name` - Station Name
- ✅ `NumberOfLapsToStop` - Anzahl Runden bis Halt
- ✅ `Flow.Name` - Workflow Name

**Für Phase 2 vorbereitet:**
- 🔜 `Platforms` - Liste von Bahnsteigen (noch leer/ungenutzt)

### Platform (Domain\Platform.cs) - Phase 2
```csharp
public class Platform
{
    public string Name { get; set; }                    // Bahnsteig-Name
    public string? Track { get; set; }                  // 🔜 Gleis-Nummer (Number)
    public int? FeedbackInPort { get; set; }            // Feedback InPort
    public uint InPort { get; set; }                    // InPort
    public Workflow? Flow { get; set; }                 // Workflow pro Bahnsteig
    
    // Zukünftig hinzufügen (Phase 2):
    // public TimeSpan? Arrival { get; set; }          // 🔜 Ankunftszeit
    // public TimeSpan? Departure { get; set; }        // 🔜 Abfahrtszeit
    // public bool IsExitOnLeft { get; set; }          // 🔜 Ausstieg links?
}
```

---

## 💡 Korrekte Interpretation

### Phase 1: Stations-Ansicht (MVP) ✅
```
Aktuelles UI zeigt:
┌─────────────────────────────────────────────────┐
│ Name          │ Laps │ Workflow                 │
├─────────────────────────────────────────────────┤
│ Hauptbahnhof  │  3   │ Announcement             │
│ Stadtmitte    │  5   │ Stop Signal              │
│ Endstation    │  1   │ -                        │
└─────────────────────────────────────────────────┘
```

**Das ist korrekt für Phase 1!**  
Keine Gleis-Details, nur Station-Level Information.

### Phase 2: Platform-Ansicht (Zukünftig) 🔜
```
Zukünftiges UI mit Platforms:
┌─────────────────────────────────────────────────────────────────┐
│ Station: Hauptbahnhof                                            │
├─────────────────────────────────────────────────────────────────┤
│ Platform │ Track │ Arrival │ Departure │ Exit Left │ Workflow  │
├─────────────────────────────────────────────────────────────────┤
│ Gleis 1  │   1   │  12:00  │   12:05   │    ✓      │ Announce  │
│ Gleis 2  │   2   │  12:10  │   12:15   │    ✗      │ Stop      │
│ Gleis 3  │   3   │  12:20  │   12:25   │    ✓      │ -         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Aktuelle XAML ist korrekt! ✅

### Stations-ListView (ProjectConfigurationPage.xaml)
```xaml
<ListView ItemsSource="{x:Bind ViewModel.Stations}">
    <ListView.HeaderTemplate>
        <DataTemplate>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="200" />  <!-- Name -->
                    <ColumnDefinition Width="100" />  <!-- Laps to Stop -->
                    <ColumnDefinition Width="150" />  <!-- Workflow -->
                </Grid.ColumnDefinitions>
                
                <TextBlock Text="Name" />
                <TextBlock Text="Laps to Stop" />
                <TextBlock Text="Workflow" />
            </Grid>
        </DataTemplate>
    </ListView.HeaderTemplate>
    
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="vm:StationViewModel">
            <Grid>
                <TextBlock Text="{x:Bind Name, Mode=OneWay}" />
                <TextBlock Text="{x:Bind NumberOfLapsToStop, Mode=OneWay}" />
                <TextBlock Text="{x:Bind Flow.Name, Mode=OneWay}" />
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Das ist perfekt für Phase 1!** 🎉

---

## 🔄 Migration zu Phase 2 (Zukünftig)

### Wann Platform-Features hinzufügen?

#### Schritt 1: Domain erweitern
```csharp
// Domain\Platform.cs
public class Platform
{
    // Bestehende Properties...
    
    // NEU für Phase 2:
    public TimeSpan? ArrivalTime { get; set; }     // Ankunftszeit
    public TimeSpan? DepartureTime { get; set; }   // Abfahrtszeit
    public bool IsExitOnLeft { get; set; }         // Ausstieg links
}
```

#### Schritt 2: PlatformViewModel erstellen
```csharp
// SharedUI\ViewModel\PlatformViewModel.cs
public partial class PlatformViewModel : ObservableObject
{
    [ObservableProperty]
    private Platform model;
    
    public string Name => Model.Name;
    public string? Track => Model.Track;
    public TimeSpan? Arrival => Model.ArrivalTime;
    public TimeSpan? Departure => Model.DepartureTime;
    public bool IsExitOnLeft => Model.IsExitOnLeft;
    public string? WorkflowName => Model.Flow?.Name;
}
```

#### Schritt 3: StationViewModel erweitern
```csharp
// SharedUI\ViewModel\StationViewModel.cs
public partial class StationViewModel : ObservableObject
{
    // NEU: Platform-ViewModels für Phase 2
    public ObservableCollection<PlatformViewModel> PlatformViewModels { get; }
    
    public StationViewModel(Station model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
        
        // Platforms zu ViewModels konvertieren
        PlatformViewModels = new ObservableCollection<PlatformViewModel>(
            Model.Platforms.Select(p => new PlatformViewModel(p, dispatcher))
        );
    }
}
```

#### Schritt 4: Neue UI-Seite für Platforms
```xaml
<!-- WinUI\View\StationDetailPage.xaml -->
<Page>
    <Grid>
        <TextBlock Text="{x:Bind ViewModel.SelectedStation.Name}" 
                   Style="{ThemeResource TitleTextBlockStyle}" />
        
        <!-- Platforms ListView -->
        <ListView ItemsSource="{x:Bind ViewModel.SelectedStation.PlatformViewModels}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="vm:PlatformViewModel">
                    <Grid>
                        <TextBlock Text="{x:Bind Name}" />
                        <TextBlock Text="{x:Bind Track}" />
                        <TextBlock Text="{x:Bind Arrival}" />
                        <TextBlock Text="{x:Bind Departure}" />
                        <CheckBox IsChecked="{x:Bind IsExitOnLeft}" />
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</Page>
```

---

## ✅ Zusammenfassung

### Was war das Problem?
Die XAML-Bindings haben auf **Platform-Properties** verwiesen, obwohl die UI nur **Stations** anzeigen sollte (Phase 1).

### Was wurde behoben?
- ✅ Platform-spezifische Bindings entfernt
- ✅ Stations-ListView vereinfacht auf MVP-Scope
- ✅ Klare Trennung: Station (Phase 1) vs Platform (Phase 2)

### Was ist das Ergebnis?
- ✅ UI zeigt jetzt korrekt nur Station-Level Information
- ✅ Vorbereitet für Phase 2 (Platforms sind im Domain Model vorhanden)
- ✅ Clean Architecture korrekt umgesetzt

### Nächste Schritte (wenn Phase 2 gewünscht)
1. Entscheidung: Wann soll Platform-Feature implementiert werden?
2. Domain\Platform.cs erweitern (Arrival, Departure, IsExitOnLeft)
3. PlatformViewModel erstellen
4. Neue UI-Seite für Platform-Details
5. Navigation: Station-Auswahl → Platform-Details

---

## 📖 Referenz

- **Aktuelle Implementierung**: Phase 1 (Stations only) ✅
- **Domain Model**: `Domain\Station.cs`, `Domain\Platform.cs`
- **ViewModel**: `SharedUI\ViewModel\StationViewModel.cs`
- **XAML**: `WinUI\View\ProjectConfigurationPage.xaml`
- **Architecture**: `docs\CLEAN-ARCHITECTURE-FINAL-STATUS.md`

---

**Status**: ✅ KORREKT - Stations-Ansicht für Phase 1 MVP perfekt implementiert!

---

## 🔍 Was ist passiert?

### Entfernte XAML-Bindings (ProjectConfigurationPage.xaml)

Folgende Properties wurden aus der **Stations-ListView** entfernt:

```xaml
<!-- ENTFERNT -->
<TextBlock Text="{x:Bind Number, Mode=OneWay}" />
<TextBlock Text="{x:Bind Arrival, Mode=OneWay}" />
<TextBlock Text="{x:Bind Departure, Mode=OneWay}" />
<CheckBox IsChecked="{x:Bind IsExitOnLeft, Mode=OneWay}" />
```

### Grund für Entfernung
Diese Properties existieren **nicht** in der `Station`-Klasse oder im `StationViewModel`.

---

## 📊 Aktuelle Domain-Struktur

### Station (Domain\Station.cs)
```csharp
public class Station
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public List<Platform> Platforms { get; set; }  // ⭐ Station HAT mehrere Platforms
    public int? FeedbackInPort { get; set; }
    public int NumberOfLapsToStop { get; set; }
    public Workflow? Flow { get; set; }
    public Guid? WorkflowId { get; set; }
}
```

**Verfügbare Properties:**
- ✅ `Name` - Station Name
- ✅ `NumberOfLapsToStop` - Anzahl Runden bis Halt
- ✅ `Flow.Name` - Workflow Name

**NICHT verfügbar:**
- ❌ `Number` - Existiert nicht
- ❌ `Arrival` - Existiert nicht
- ❌ `Departure` - Existiert nicht
- ❌ `IsExitOnLeft` - Existiert nicht

### Platform (Domain\Platform.cs)
```csharp
public class Platform
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Track { get; set; }  // ⭐ Gleis-Nummer (war früher int, jetzt string)
    public int? FeedbackInPort { get; set; }
    public uint InPort { get; set; }
    public Workflow? Flow { get; set; }
    // ... weitere Properties
}
```

**Beziehung:** Station → Platforms (1:n)

---

## 💡 Hypothese: Was könnten die entfernten Properties bedeutet haben?

### 1. Number (Station-Nummer?)
- **Möglichkeit 1**: Eindeutige Stations-Nummer im Netz
- **Möglichkeit 2**: Sortierungs-Index
- **Lösung**: 
  - Falls benötigt: Neue Property `public int Number { get; set; }` zu Station hinzufügen
  - Oder: Index aus der Liste verwenden

### 2. Arrival (Ankunftszeit?)
- **Möglichkeit 1**: Geplante Ankunftszeit im Fahrplan
- **Möglichkeit 2**: Ankunftsgleis (Track)
- **Lösung**:
  - Falls Fahrplan: Neue Property `public TimeSpan? ArrivalTime { get; set; }`
  - Falls Gleis: Verwende `Platform.Track`

### 3. Departure (Abfahrtszeit?)
- **Möglichkeit 1**: Geplante Abfahrtszeit im Fahrplan
- **Möglichkeit 2**: Abfahrtsgleis (Track)
- **Lösung**:
  - Falls Fahrplan: Neue Property `public TimeSpan? DepartureTime { get; set; }`
  - Falls Gleis: Verwende `Platform.Track`

### 4. IsExitOnLeft (Ausstieg links?)
- **Möglichkeit**: Gleis-Orientierung (Exit auf linker oder rechter Seite)
- **Lösung**:
  - Zu `Platform` hinzufügen: `public bool IsExitOnLeft { get; set; }`
  - Platform-spezifische Eigenschaft, nicht Station-weit

---

## 🎯 Empfohlene Vorgehensweise

### Option 1: Properties waren obsolet/ungenutzt ✅
**Wenn diese Properties nie verwendet wurden:**
- ✅ Keine Aktion nötig
- ✅ XAML-Bindings korrekt entfernt
- ✅ UI ist jetzt korrek vereinfacht

### Option 2: Properties werden benötigt ⚠️
**Wenn diese Properties für Business-Logik wichtig sind:**

#### Schritt 1: Business-Anforderungen klären
```
Fragen an Product Owner / Anforderungen:
1. Brauchen wir eine Station-Nummer?
2. Brauchen wir Fahrplan-Zeiten (Ankunft/Abfahrt)?
3. Brauchen wir Gleis-Orientierung (Exit links/rechts)?
4. Sind diese Eigenschaften Station- oder Platform-spezifisch?
```

#### Schritt 2: Domain Model erweitern (falls benötigt)
```csharp
// Domain\Station.cs
public class Station
{
    // Bestehende Properties...
    
    // NEU (falls benötigt):
    public int? Number { get; set; }  // Optional: Station-Nummer
    public TimeSpan? ArrivalTime { get; set; }  // Optional: Fahrplan
    public TimeSpan? DepartureTime { get; set; }  // Optional: Fahrplan
}

// Domain\Platform.cs
public class Platform
{
    // Bestehende Properties...
    
    // NEU (falls benötigt):
    public bool IsExitOnLeft { get; set; }  // Gleis-Orientierung
}
```

#### Schritt 3: ViewModel erweitern
```csharp
// SharedUI\ViewModel\StationViewModel.cs
public partial class StationViewModel : ObservableObject
{
    // NEU Properties hinzufügen
    public int? Number
    {
        get => Model.Number;
        set => SetProperty(Model.Number, value, Model, (m, v) => m.Number = v);
    }
    
    public TimeSpan? Arrival
    {
        get => Model.ArrivalTime;
        set => SetProperty(Model.ArrivalTime, value, Model, (m, v) => m.ArrivalTime = v);
    }
    
    // ... usw.
}
```

#### Schritt 4: XAML wiederherstellen
```xaml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="200" />  <!-- Name -->
    <ColumnDefinition Width="80" />   <!-- Number -->
    <ColumnDefinition Width="100" />  <!-- Laps -->
    <ColumnDefinition Width="100" />  <!-- Arrival -->
    <ColumnDefinition Width="100" />  <!-- Departure -->
    <ColumnDefinition Width="150" />  <!-- Workflow -->
</Grid.ColumnDefinitions>

<TextBlock Text="{x:Bind Name, Mode=OneWay}" />
<TextBlock Text="{x:Bind Number, Mode=OneWay}" />
<TextBlock Text="{x:Bind NumberOfLapsToStop, Mode=OneWay}" />
<TextBlock Text="{x:Bind Arrival, Mode=OneWay}" />
<TextBlock Text="{x:Bind Departure, Mode=OneWay}" />
<TextBlock Text="{x:Bind Flow.Name, Mode=OneWay}" />
```

---

## 🔄 Clean Architecture Kontext

### Was hat sich durch das Refactoring geändert?

#### Vorher (Backend.Model)
```csharp
// Backend\Model\Station.cs (ALT)
public class Station
{
    // Möglicherweise hatte Station diese Properties:
    public int Number { get; set; }
    public string Arrival { get; set; }
    public string Departure { get; set; }
    public bool IsExitOnLeft { get; set; }
}
```

#### Nachher (Domain)
```csharp
// Domain\Station.cs (NEU)
public class Station
{
    // Pure POCO - nur essenzielle Properties
    public string Name { get; set; }
    public List<Platform> Platforms { get; set; }
    public int NumberOfLapsToStop { get; set; }
    public Workflow? Flow { get; set; }
}
```

**Grund für Änderung:**
- Domain soll **reine Daten-Objekte** (POCOs) enthalten
- Keine UI-spezifischen Properties
- Keine redundanten Properties
- Klare Trennung: Station ≠ Platform

---

## 📝 Nächste Schritte

### Sofort (Empfohlen)
1. **Business-Anforderungen klären**:
   - Kontaktieren Sie Product Owner / Requirements
   - Fragen Sie: "Brauchen wir Station-Nummer, Fahrplan-Zeiten, Gleis-Orientierung?"
   - Prüfen Sie alte Dokumentation / User Stories

2. **Alte XAML-Version prüfen** (falls vorhanden):
   ```bash
   git log --all --full-history -- "WinUI/View/ProjectConfigurationPage.xaml"
   git show <commit-hash>:WinUI/View/ProjectConfigurationPage.xaml
   ```

3. **Alte Backend.Model.Station prüfen**:
   ```bash
   git show <commit-before-refactoring>:Backend/Model/Station.cs
   ```

### Falls Properties benötigt werden
4. Domain Model erweitern (siehe Schritt 2 oben)
5. ViewModel erweitern (siehe Schritt 3 oben)
6. XAML wiederherstellen (siehe Schritt 4 oben)
7. Daten-Migration schreiben (falls gespeicherte Daten existieren)

### Falls Properties NICHT benötigt werden
4. ✅ Keine Aktion nötig - XAML ist jetzt korrekt
5. ✅ Dokumentieren in User Guide: "Vereinfachte Station-Ansicht"

---

## 🎯 Empfehlung

**Ich empfehle:** Option 1 (Properties waren obsolet)

**Begründung:**
1. Station und Platform sind klar getrennt im Domain Model
2. Fahrplan-Zeiten (Arrival/Departure) sind typischerweise Journey-spezifisch, nicht Station-spezifisch
3. IsExitOnLeft ist Platform-spezifisch (Gleis), nicht Station-weit
4. Number ist redundant (kann aus Liste berechnet werden)

**Wenn diese Eigenschaften wirklich benötigt werden:**
- Dann waren sie im alten Backend.Model falsch modelliert
- Clean Architecture hat diesen Design-Fehler aufgedeckt
- Jetzt können wir sie korrekt dem richtigen Entity zuordnen

---

## 📖 Referenz

- **Domain Model**: `Domain\Station.cs`, `Domain\Platform.cs`
- **ViewModel**: `SharedUI\ViewModel\StationViewModel.cs`
- **XAML**: `WinUI\View\ProjectConfigurationPage.xaml`
- **Clean Architecture Status**: `docs\CLEAN-ARCHITECTURE-FINAL-STATUS.md`

---

**Nächste Aktion**: Business-Anforderungen klären, dann entscheiden ob Properties wiederhergestellt werden sollen.
