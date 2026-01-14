---
description: 'Detaillierte VSM-Anleitung mit Before/After Beispielen, Setter-Syntax und häufigen Fehlern'
applyTo: 'WinUI/**/*.xaml'
---

# WinUI 3 VisualStateManager - Detailed Guide

> Eine tiefgehende Anleitung mit realen Codebeispielen, Syntax-Details und häufigen Fehlern

## 🎯 PART 1: Setter-Syntax (Die Grundlagen)

### Was ist ein Setter im VSM?

Ein **Setter** ist eine Anweisung, die einer bestimmten **Property eines Elementes** einen **neuen Wert** zuweist, wenn ein bestimmter VisualState aktiv wird.

**Syntax:**
```xaml
<Setter Target="ElementName.PropertyName" Value="NewValue" />
```

---

### 🔴 BEISPIEL 1: Sichtbarkeit ändern (Visibility)

#### ❌ OHNE VSM (Hardcoded):

```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="260" />  <!-- Settings Panel - IMMER 260px! -->
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="100" />  <!-- Functions Panel - IMMER 100px! -->
    </Grid.ColumnDefinitions>
    
    <!-- Settings Panel (sichtbar auf Desktop, aber zu breit auf Tablet!) -->
    <StackPanel x:Name="SettingsPanel" Grid.Column="0">
        <TextBlock Text="Settings" />
        <!-- ... viele Settings Controls ... -->
    </StackPanel>
    
    <!-- Main Content -->
    <Canvas Grid.Column="1" />
    
    <!-- Functions Panel (sichtbar auf Desktop, aber nimmt zu viel Platz auf Tablet!) -->
    <StackPanel x:Name="FunctionsPanel" Grid.Column="2">
        <Button Content="F0" />
        <Button Content="F1" />
    </StackPanel>
</Grid>
```

**Problem:** 
- Desktop (1920px): OK ✅
- Tablet (768px): Spalten überlappen, alles wird gequetscht ❌
- Nutzer kann nicht scrollen → Inhalt unlesbar 💥

---

#### ✅ MIT VSM (Responsive):

```xaml
<Page>
    <Grid x:Name="RootGrid">
        <!-- SCHRITT 1: VisualStateManager OBEN definieren! -->
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="AdaptiveStates">
                
                <!-- STATE: Wide Screen (1200px+) -->
                <VisualState x:Name="WideState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="1200" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <!-- Bei Wide: beide Panels sichtbar -->
                        <Setter Target="SettingsPanel.Visibility" Value="Visible" />
                        <Setter Target="FunctionsPanel.Visibility" Value="Visible" />
                    </VisualState.Setters>
                </VisualState>
                
                <!-- STATE: Compact (0-1199px) -->
                <VisualState x:Name="CompactState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="0" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <!-- Bei Compact: beide Panels verborgen -->
                        <Setter Target="SettingsPanel.Visibility" Value="Collapsed" />
                        <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
                    </VisualState.Setters>
                </VisualState>
                
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
        
        <!-- SCHRITT 2: Layout bleibt GLEICH -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="260" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="100" />
            </Grid.ColumnDefinitions>
            
            <!-- Settings Panel (wird durch Visibility-Setter verborgen bei Compact) -->
            <StackPanel x:Name="SettingsPanel" Grid.Column="0">
                <TextBlock Text="Settings" />
            </StackPanel>
            
            <!-- Main Content (wird größer wenn Panels verborgen sind) -->
            <Canvas Grid.Column="1" />
            
            <!-- Functions Panel (wird durch Visibility-Setter verborgen bei Compact) -->
            <StackPanel x:Name="FunctionsPanel" Grid.Column="2">
                <Button Content="F0" />
                <Button Content="F1" />
            </StackPanel>
        </Grid>
    </Grid>
</Page>
```

**Resultat:**
- Desktop (1920px): Settings + Canvas + Functions sichtbar ✅
- Tablet (768px): Nur Canvas sichtbar (Settings/Functions `Collapsed`) ✅
- Nutzer sieht sauberes, lesbares Layout ✅

---

### 🟢 BEISPIEL 2: Column-Breiten dynamisch ändern

#### ❌ OHNE VSM:

```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="260" />  <!-- FEST! -->
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="100" />  <!-- FEST! -->
    </Grid.ColumnDefinitions>
    
    <StackPanel Grid.Column="0" />
    <Canvas Grid.Column="1" />
    <StackPanel Grid.Column="2" />
</Grid>

<!-- Problem: Spaltenbreiten passen sich NICHT an Fensterbreite an! -->
```

---

#### ✅ MIT VSM:

```xaml
<Grid x:Name="ControlGrid">
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="LayoutStates">
            
            <!-- WIDE: 4 Spalten optimal -->
            <VisualState x:Name="WideState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="1200" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <!-- Alle 4 Spalten sichtbar, jede mit ihrer eigenen Breite -->
                    <Setter Target="ControlGrid.ColumnDefinitions" 
                            Value="260,*,100,200" />
                </VisualState.Setters>
            </VisualState>
            
            <!-- MEDIUM: 2 Spalten (Settings versteckt) -->
            <VisualState x:Name="MediumState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="641" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <!-- Nur noch 2 Spalten: Settings + Hauptinhalt -->
                    <Setter Target="ControlGrid.ColumnDefinitions" 
                            Value="250,*" />
                    <!-- Verstecke die rechten Panels -->
                    <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
                    <Setter Target="SpeedPanel.Visibility" Value="Collapsed" />
                </VisualState.Setters>
            </VisualState>
            
            <!-- COMPACT: 1 Spalte (alles übereinander) -->
            <VisualState x:Name="CompactState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="0" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <!-- Nur eine Spalte! -->
                    <Setter Target="ControlGrid.ColumnDefinitions" 
                            Value="*" />
                    <!-- Verstecke alle Panels außer Tachometer -->
                    <Setter Target="SettingsPanel.Visibility" Value="Collapsed" />
                    <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
                    <Setter Target="SpeedPanel.Visibility" Value="Collapsed" />
                    <!-- Optional: Add spacing -->
                    <Setter Target="SettingsPanel.Margin" Value="0,10,0,0" />
                </VisualState.Setters>
            </VisualState>
            
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
    
    <!-- Die ColumnDefinitions können mit "Auto" starten, VSM überschreibt sie! -->
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="260" x:Name="Col0" />
        <ColumnDefinition Width="*" x:Name="Col1" />
        <ColumnDefinition Width="100" x:Name="Col2" />
        <ColumnDefinition Width="200" x:Name="Col3" />
    </Grid.ColumnDefinitions>
    
    <StackPanel x:Name="SettingsPanel" Grid.Column="0" />
    <Canvas Grid.Column="1" />
    <StackPanel x:Name="FunctionsPanel" Grid.Column="2" />
    <StackPanel x:Name="SpeedPanel" Grid.Column="3" />
</Grid>
```

**Wie Setter für ColumnDefinitions funktionieren:**

```
Value="260,*,100,200" bedeutet:
  Spalte 0: 260 Pixel (fest)
  Spalte 1: * (verfügbarer Platz, flexibel)
  Spalte 2: 100 Pixel (fest)
  Spalte 3: 200 Pixel (fest)

Value="250,*" bedeutet:
  Spalte 0: 250 Pixel
  Spalte 1: * (verfügbarer Platz)
  → Spalten 2 und 3 werden "verborgen" (nicht vorhanden!)

Value="*" bedeutet:
  Spalte 0: * (ganzer verfügbarer Platz)
  → Nur noch 1 Spalte!
```

---

## 🟡 BEISPIEL 3: Mehrere Properties in einem Setter

#### ❌ FALSCH:

```xaml
<!-- ❌ Jeder Setter ist nur FÜR EINE Property! -->
<Setter Target="SettingsPanel.Visibility, SettingsPanel.Margin" 
        Value="Collapsed, 0,0,0,0" />
<!-- Das funktioniert NICHT! Jede Property braucht einen eigenen Setter! -->
```

---

#### ✅ RICHTIG:

```xaml
<!-- ✅ Mehrere Setters für verschiedene Properties -->
<VisualState.Setters>
    <Setter Target="SettingsPanel.Visibility" Value="Collapsed" />
    <Setter Target="SettingsPanel.Margin" Value="0" />
    <Setter Target="SettingsPanel.Opacity" Value="0.5" />
    <!-- Jeder Setter = eine Property! -->
</VisualState.Setters>
```

---

## 🔵 BEISPIEL 4: Margin & Padding anpassen

```xaml
<VisualState x:Name="WideState">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="1200" />
    </VisualState.StateTriggers>
    <VisualState.Setters>
        <!-- Großer Margin auf Desktop -->
        <Setter Target="MainPanel.Margin" Value="32,24,32,24" />
        <Setter Target="MainPanel.Padding" Value="20" />
    </VisualState.Setters>
</VisualState>

<VisualState x:Name="CompactState">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="0" />
    </VisualState.StateTriggers>
    <VisualState.Setters>
        <!-- Kleiner Margin auf Mobile (weniger Platz verschwenden) -->
        <Setter Target="MainPanel.Margin" Value="8,4,8,4" />
        <Setter Target="MainPanel.Padding" Value="8" />
    </VisualState.Setters>
</VisualState>
```

---

## ⚫ Häufige FEHLER & wie man sie vermeidet

### ❌ FEHLER 1: VisualStateManager am falschen Ort

```xaml
<!-- ❌ FALSCH: VSM steht NICHT direkt unter dem Element -->
<Grid x:Name="MyGrid">
    <TextBlock Text="Title" />
    <VisualStateManager.VisualStateGroups>  <!-- ❌ Zu spät! -->
        ...
    </VisualStateManager.VisualStateGroups>
</Grid>

<!-- ✅ RICHTIG: VSM steht ganz OBEN, vor dem Content! -->
<Grid x:Name="MyGrid">
    <VisualStateManager.VisualStateGroups>  <!-- ✅ Ganz oben! -->
        ...
    </VisualStateManager.VisualStateGroups>
    <TextBlock Text="Title" />
</Grid>
```

---

### ❌ FEHLER 2: Target Path falsch geschrieben

```xaml
<!-- ❌ FALSCH: Element-Name nicht gefunden -->
<Setter Target="SettingsPanel.Visibility" Value="Collapsed" />
<!-- Aber SettingsPanel ist mit x:Name="SettingPanel" definiert! -->

<!-- ✅ RICHTIG: Exakter Name! -->
<Setter Target="SettingPanel.Visibility" Value="Collapsed" />
```

---

### ❌ FEHLER 3: Ungültige Property Namen

```xaml
<!-- ❌ FALSCH: Property-Name existiert nicht -->
<Setter Target="Grid.ColumnDefinitions" Value="260,*" />
<!-- ColumnDefinitions ist ReadOnly! Kann nicht mit Setter geändert werden! -->

<!-- ✅ RICHTIG: Mit x:Name auf Grid beziehen -->
<Grid x:Name="MyGrid">
    ...
</Grid>
<!-- Und dann: -->
<Setter Target="MyGrid.ColumnDefinitions" Value="260,*" />
```

**Hinweis:** Manche Properties sind ReadOnly und können nicht mit VSM gesetzt werden:
- ❌ `Children` (ReadOnly)
- ❌ `ColumnDefinitions` (ReadOnly, muss anders gemacht werden)
- ✅ `Visibility` (geht!)
- ✅ `Margin` (geht!)
- ✅ `Width` (geht!)
- ✅ `Height` (geht!)

---

### ❌ FEHLER 4: ColumnDefinitions mit Setter ändern (geht NICHT direkt!)

```xaml
<!-- ❌ FALSCH: ColumnDefinitions ist ReadOnly -->
<Setter Target="Grid.ColumnDefinitions" Value="260,*,100" />

<!-- ✅ WORKAROUND: Einzelne ColumnDefinition Breiten ändern -->
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="260" x:Name="Col0" />
    <ColumnDefinition Width="*" x:Name="Col1" />
    <ColumnDefinition Width="100" x:Name="Col2" />
</Grid.ColumnDefinitions>

<!-- Und dann nur die Breiten ändern: -->
<Setter Target="Col0.Width" Value="Auto" />
<Setter Target="Col1.Width" Value="150" />
```

**ODER:** ColumnDefinitions über UniformGrid oder ähnlich verwenden.

---

### ❌ FEHLER 5: Property-Typ falsch

```xaml
<!-- ❌ FALSCH: Width sollte eine Größe sein, nicht Text -->
<Setter Target="SettingsPanel.Width" Value="Das sollte 260 sein" />

<!-- ✅ RICHTIG: Korrekte Einheit -->
<Setter Target="SettingsPanel.Width" Value="260" />
<!-- oder -->
<Setter Target="SettingsPanel.Width" Value="Auto" />
```

---

### ❌ FEHLER 6: AdaptiveTrigger Reihenfolge

```xaml
<!-- ❌ FALSCH: Trigger in aufsteigender Reihenfolge -->
<VisualState x:Name="CompactState">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="0" />  <!-- Matcht ALLES -->
    </VisualState.StateTriggers>
</VisualState>
<VisualState x:Name="WideState">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="1200" />  <!-- Wird nie erreicht! -->
    </VisualState.StateTriggers>
</VisualState>

<!-- ✅ RICHTIG: Trigger in absteigender Reihenfolge (höchste zuerst) -->
<VisualState x:Name="WideState">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="1200" />  <!-- Check zuerst -->
    </VisualState.StateTriggers>
</VisualState>
<VisualState x:Name="CompactState">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="0" />  <!-- Fallback -->
    </VisualState.StateTriggers>
</VisualState>
```

**Warum?** Weil VSM den **ersten** passenden State nimmt! Wenn CompactState (MinWindowWidth="0") zuerst kommt, wird es immer gewählt.

---

## 💡 BEST PRACTICES

### Best Practice 1: Naming Convention

```xaml
<!-- ✅ Klare Namen für x:Name Attribute -->
<StackPanel x:Name="SettingsPanel" />  <!-- Panel, nicht "SP" oder "s" -->
<Canvas x:Name="TachometerCanvas" />   <!-- Canvas, nicht "c" -->
<StackPanel x:Name="FunctionsPanel" /> <!-- Panel, nicht "fp" -->
```

### Best Practice 2: Breakpoints dokumentieren

```xaml
<!-- ✅ Kommentare für Breakpoints -->
<VisualStateGroup x:Name="AdaptiveStates">
    <!-- Wide: 1200px+ (Desktop) -->
    <VisualState x:Name="WideState">
        <VisualState.StateTriggers>
            <AdaptiveTrigger MinWindowWidth="1200" />
        </VisualState.StateTriggers>
    </VisualState>
    
    <!-- Medium: 640-1199px (Tablet) -->
    <VisualState x:Name="MediumState">
        <VisualState.StateTriggers>
            <AdaptiveTrigger MinWindowWidth="640" />
        </VisualState.StateTriggers>
    </VisualState>
    
    <!-- Compact: 0-639px (Mobile) -->
    <VisualState x:Name="CompactState">
        <VisualState.StateTriggers>
            <AdaptiveTrigger MinWindowWidth="0" />
        </VisualState.StateTriggers>
    </VisualState>
</VisualStateGroup>
```

### Best Practice 3: Redundante States vermeiden

```xaml
<!-- ❌ ZU VIELE States (wartbar!) -->
<VisualState x:Name="Ultrawide2560" MinWindowWidth="2560" />
<VisualState x:Name="Wide1920" MinWindowWidth="1920" />
<VisualState x:Name="Desktop1600" MinWindowWidth="1600" />
<VisualState x:Name="Laptop1200" MinWindowWidth="1200" />
<!-- ... zu viele ... -->

<!-- ✅ 3 STATES reichen (wartbar) -->
<VisualState x:Name="WideState">
    <AdaptiveTrigger MinWindowWidth="1200" />
</VisualState>
<VisualState x:Name="MediumState">
    <AdaptiveTrigger MinWindowWidth="640" />
</VisualState>
<VisualState x:Name="CompactState">
    <AdaptiveTrigger MinWindowWidth="0" />
</VisualState>
```

---

## 🧪 Debugging VSM

### Problem: VSM wird nicht aktiviert

```xaml
<!-- Debugging-Tipps: -->

1. Starte die App und öffne "Debug" → "Windows" → "Live Visual Tree"
   → Suche dein Grid und prüfe die Größe (ActualWidth)

2. Prüfe Visual Studio Output Fenster auf XAML-Parser Fehler
   → Oft sind Target-Pfade falsch geschrieben

3. Mache "Document Outline" auf und suche nach deinen x:Name Elements
   → Wenn x:Name nicht sichtbar ist, ist die Verbindung falsch
```

### Test: Fensterbreite prüfen

```csharp
// In MainWindow.xaml.cs:
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Logging für Debugging
        SizeChanged += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine(
                $"Window size: {e.NewSize.Width}x{e.NewSize.Height}");
        };
    }
}
```

---

## 📋 VSM Implementation Checkliste

Bevor du VSM implementierst, überprüfe diese Punkte:

- [ ] **VisualStateManager am Anfang des Grids** (nicht am Ende)
- [ ] **Alle States definiert** (Wide, Medium, Compact)
- [ ] **AdaptiveTrigger in absteigender Reihenfolge** (höchste zuerst)
- [ ] **Alle x:Name Attribute korrekt** (Namen sind eindeutig)
- [ ] **Target-Pfade überprüft** (z.B. "SettingsPanel.Visibility" nicht "SettingPanel.Visibility")
- [ ] **Property-Typen korrekt** (z.B. "Collapsed" für Visibility, nicht "False")
- [ ] **Keine ReadOnly Properties** (wie ColumnDefinitions, Children)
- [ ] **Kommentare hinzufügt** (welche Breite ist welcher State)
- [ ] **Getestet mit Fenster-Resize** (F5 drücken, Fenster resizen)
- [ ] **Live Visual Tree geprüft** (Debug Tools)

---

## 🚀 Nächste Schritte

Jetzt, da du diese Details verstehst:

1. **Öffne TrainControlPage.xaml** und folge dem Pattern hier
2. **Definiere die 3 States** (Wide/Medium/Compact)
3. **Schreibe die Setters** für Visibility und Margin
4. **Teste mit Fenster-Resize** (sollte smooth sein)
5. **Wiederhole für TrackPlanEditorPage**

