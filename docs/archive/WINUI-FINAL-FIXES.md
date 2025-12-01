# 🔧 WinUI Final UI Fixes - Properties, Splitter & Navigation

**Datum**: 2025-11-28  
**Status**: ✅ **IMPLEMENTED - TESTING NEEDED**

---

## 🐛 Verbleibende Probleme behoben

### **1. Properties werden nicht angezeigt**

**Problem**: ItemsControl zeigt keine Properties an wenn TreeView Node ausgewählt wird

**Mögliche Ursachen**:
1. ViewModel.Properties ist leer (OnNodeSelected nicht aufgerufen)
2. ItemTemplateSelector funktioniert nicht
3. Bindings sind falsch

**Implementierte Fixes**:

#### A) Debug Counter hinzugefügt
```xaml
<StackPanel Spacing="8">
    <!--  Debug: Show count  -->
    <TextBlock 
        Text="{x:Bind ViewModel.Properties.Count, Mode=OneWay}" 
        Foreground="Yellow"
        FontWeight="Bold" />
    
    <!--  Properties List  -->
    <ItemsControl 
        ItemsSource="{x:Bind ViewModel.Properties, Mode=OneWay}"
        ItemTemplateSelector="{StaticResource PropertyTemplateSelector}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Spacing="8" />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
    </ItemsControl>
</StackPanel>
```

**Was der Debug Counter zeigt**:
- **0** = `OnNodeSelected` wurde nicht aufgerufen oder Node hat keine Properties
- **>0** = Properties sind da, aber ItemsControl zeigt sie nicht an → TemplateSelector Problem

#### B) ItemsPanel hinzugefügt
```xaml
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <StackPanel Spacing="8" />
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

Ohne ItemsPanel kann ItemsControl die Items nicht korrekt darstellen.

---

### **2. Properties/Cities Konflikt - Beide gleichzeitig sehen**

**Problem**: Entweder Properties ODER Cities, aber nicht beide gleichzeitig

**Vorheriges Layout**:
```
Grid.Column="2"
├── Row 0: Properties Header
├── Row 1: Properties Content (*)
└── Row 2: Cities (Auto) ← Nimmt vollen Platz!
```

**Neues Layout mit Splitter**:
```xaml
<Grid Grid.Column="2">
    <Grid.RowDefinitions>
        <!--  Properties (Resizable)  -->
        <RowDefinition Height="*" MinHeight="200" />
        
        <!--  Horizontal Splitter  -->
        <RowDefinition Height="8" />
        
        <!--  Cities (300px default)  -->
        <RowDefinition Height="300" MinHeight="150" />
    </Grid.RowDefinitions>

    <!--  Properties Section  -->
    <Grid Grid.Row="0">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" /> <!-- Header -->
            <RowDefinition Height="*" />    <!-- Content -->
        </Grid.RowDefinitions>
        
        <Border Grid.Row="0"><!-- Header --></Border>
        <ScrollViewer Grid.Row="1"><!-- Content --></ScrollViewer>
    </Grid>

    <!--  Visual Splitter  -->
    <Border Grid.Row="1" Background="{StaticResource RailwaySecondaryBrush}" Opacity="0.3">
        <Rectangle Fill="{StaticResource RailwaySecondaryBrush}" Height="2" />
    </Border>

    <!--  Cities Section  -->
    <Grid Grid.Row="2">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" /> <!-- Header -->
            <RowDefinition Height="*" />    <!-- List -->
        </Grid.RowDefinitions>
        
        <Border Grid.Row="0"><!-- Header + Load Button --></Border>
        <ListView Grid.Row="1"><!-- Cities List --></ListView>
    </Grid>
</Grid>
```

**Ergebnis**:
```
┌────────────────────────────────────┐
│ 🛠️ Properties                     │
├────────────────────────────────────┤
│                                    │
│ [Property 1]                       │
│ [Property 2]                       │
│ [Property 3]                       │
│                                    │
├════════════════════════════════════┤ ← Orange Splitter
│ 🌍 Cities              [Load]     │
├────────────────────────────────────┤
│ • City 1                           │
│ • City 2                           │
│ • City 3                           │
└────────────────────────────────────┘
```

**Vorteile**:
- ✅ Properties **immer sichtbar** (oben)
- ✅ Cities **immer sichtbar** (unten)
- ✅ Visueller Splitter (8px orange)
- ✅ MinHeight verhindert Kollaps
- ✅ Beide Bereiche scrollbar

---

### **3. Überlappung oben links - NavigationView**

**Problem**: Text "MOBAflow" und "Railway Control" überlappen sich

**Ursache**: 
```xaml
<NavigationView
    PaneTitle="MOBAflow"  ← Standard WinUI Title
    ...>
    <NavigationView.PaneHeader>  ← Custom Header
        <TextBlock Text="Railway Control" />
    </NavigationView.PaneHeader>
</NavigationView>
```

**Beide wurden gleichzeitig angezeigt!**

**Fix**:
```xaml
<NavigationView
    PaneDisplayMode="Left"
    IsPaneToggleButtonVisible="True">
    
    <!--  Nur PaneHeader, kein PaneTitle!  -->
    <NavigationView.PaneHeader>
        <Grid Padding="16,8">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <FontIcon 
                    Glyph="&#xE81D;" 
                    FontSize="24" 
                    Foreground="{StaticResource RailwayPrimaryBrush}" />
                <TextBlock 
                    Text="MOBAflow" 
                    Style="{StaticResource TitleTextBlockStyle}" 
                    VerticalAlignment="Center" 
                    FontWeight="SemiBold" />
            </StackPanel>
        </Grid>
    </NavigationView.PaneHeader>
</NavigationView>
```

**Änderungen**:
- ❌ `PaneTitle` entfernt
- ✅ Nur `PaneHeader` verwendet
- ✅ Größere Schrift (`TitleTextBlockStyle`)
- ✅ Fett (`FontWeight="SemiBold"`)
- ✅ Icon + Text nebeneinander

---

## 📊 Layout-Übersicht

### **ExplorerPage - Vollständiges Layout**

```
┌──────────────┬─┬──────────────────────────────┐
│              │▓│  🛠️ Properties              │
│              │▓├──────────────────────────────┤
│  📁 Solution │▓│  Count: 5 (Debug)           │
│  Explorer    │▓│                              │
│              │▓│  [Property 1: TextBox]      │
│  • Project   │▓│  [Property 2: CheckBox]     │
│    • Journey │▓│  [Property 3: ComboBox]     │
│    • Train   │▓│                              │
│              │▓╞══════════════════════════════╡
│              │▓│  🌍 Cities      [Load]      │
│              │▓├──────────────────────────────┤
│              │▓│  • Vienna                   │
│              │▓│  • Berlin                   │
│  (300px)     │▓│  • Prague                   │
│              │▓│                              │
└──────────────┴─┴──────────────────────────────┘
   ↑           ↑              ↑
TreeView    Splitter   Properties/Cities
(resizable)   (8px)      (split 50/50)
```

### **MainWindow - NavigationView**

```
┌────────────────────────────────────────────────┐
│ 🚂 MOBAflow                                   │ ← PaneHeader (kein Overlap!)
├────────────────────────────────────────────────┤
│ 📁 Overview                                    │
│ 📂 Explorer     ← Selected                     │
│ ✏️ Editor                                      │
│ ⚙️ Configuration                               │
└────────────────────────────────────────────────┘
```

---

## 🔍 Debugging - Properties nicht sichtbar

### **Schritt 1: Debug Counter prüfen**

Nach Neustart der App und Auswahl eines TreeView Nodes:

**Fall A: Counter zeigt "0"**
```
Properties
Count: 0
```

**Bedeutung**: `ViewModel.Properties` ist leer

**Ursachen**:
1. `OnNodeSelected` wird nicht aufgerufen
2. Node hat keinen DataContext
3. Node.DataType ist null

**Lösung**: In `ExplorerPage.xaml.cs` prüfen:
```csharp
private void SolutionTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
{
    if (args.AddedItems.Count > 0 && args.AddedItems[0] is TreeViewItem item && item.Tag is TreeNodeViewModel node)
    {
        System.Diagnostics.Debug.WriteLine($"🔍 Node selected: {node.DisplayName}");
        System.Diagnostics.Debug.WriteLine($"   DataContext: {node.DataContext != null}");
        System.Diagnostics.Debug.WriteLine($"   DataType: {node.DataType?.Name}");
        
        ViewModel.OnNodeSelected(node);
        
        System.Diagnostics.Debug.WriteLine($"   Properties Count: {ViewModel.Properties.Count}");
    }
}
```

**Fall B: Counter zeigt ">0" (z.B. "5")**
```
Properties
Count: 5
[empty space - keine Items sichtbar]
```

**Bedeutung**: Properties sind da, aber ItemsControl zeigt sie nicht

**Ursachen**:
1. ItemTemplateSelector wählt kein Template
2. Templates sind falsch definiert
3. DataType-Matching funktioniert nicht

**Lösung**: PropertyTemplateSelector Debugging:
```csharp
// In PropertyDataTemplateSelector.cs
protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
{
    if (item is not PropertyViewModel prop)
    {
        Debug.WriteLine($"❌ SelectTemplate: Item is not PropertyViewModel!");
        return null;
    }

    Debug.WriteLine($"🔍 SelectTemplate for: {prop.Name}");
    Debug.WriteLine($"   PropertyType: {prop.PropertyType}");
    Debug.WriteLine($"   IsEnum: {prop.IsEnum}");
    Debug.WriteLine($"   IsReference: {prop.IsReference}");

    // Template selection logic...
}
```

---

## ✅ Checkliste zum Testen

### **1. Navigation & Overlap**
- [ ] WinUI App starten
- [ ] NavigationView oben links prüfen
- [ ] ✅ "MOBAflow" Text sichtbar ohne Overlap
- [ ] ✅ Icon (🚂) neben Text
- [ ] ✅ Kein doppelter/überlappender Text

### **2. Explorer Layout**
- [ ] Zu "Explorer" navigieren
- [ ] ✅ TreeView links sichtbar
- [ ] ✅ Properties Bereich oben rechts sichtbar
- [ ] ✅ Cities Bereich unten rechts sichtbar
- [ ] ✅ Oranger Splitter zwischen Properties/Cities
- [ ] ✅ Beide Bereiche gleichzeitig sichtbar

### **3. Properties Display**
- [ ] TreeView Node auswählen (z.B. Journey, Train)
- [ ] Properties Bereich prüfen:
  - [ ] ✅ "Count: X" wird angezeigt (X > 0)
  - [ ] ✅ Property Felder werden angezeigt
  - [ ] ✅ TextBox für String Properties
  - [ ] ✅ CheckBox für Bool Properties
  - [ ] ✅ ComboBox für Enum Properties

**Wenn Count = 0**:
- Debug-Ausgabe in Visual Studio Output prüfen
- `OnNodeSelected` wird aufgerufen?
- Node hat DataContext?

**Wenn Count > 0 aber keine Items**:
- PropertyTemplateSelector Problem
- Debug-Ausgabe prüfen
- Template-Matching funktioniert?

### **4. Cities**
- [ ] "Load" Button klicken
- [ ] ✅ Cities werden geladen
- [ ] ✅ ListView zeigt Cities an
- [ ] ✅ Icon + Name für jede City

### **5. Connection Flow**
- [ ] "Connect Z21" in Toolbar klicken
- [ ] ✅ Connection Status ändert sich
- [ ] ✅ Nur EIN Click nötig!
- [ ] ✅ Track Power wird enabled

---

## 🔧 Weitere Fixes falls Properties nicht funktionieren

### **Option A: Fallback zu StackPanel mit x:Bind**

Wenn ItemsControl + TemplateSelector nicht funktioniert:

```xaml
<ScrollViewer Grid.Row="1" Padding="16">
    <StackPanel Spacing="16">
        <!--  Manual Property Rendering  -->
        <ItemsControl ItemsSource="{x:Bind ViewModel.Properties, Mode=OneWay}">
            <ItemsControl.ItemTemplate>
                <DataTemplate x:DataType="vm:PropertyViewModel">
                    <StackPanel Spacing="4" Margin="0,0,0,12">
                        <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold" />
                        <TextBox Text="{x:Bind Value, Mode=TwoWay}" 
                                 Visibility="{x:Bind IsTextBox}" />
                        <CheckBox Content="{x:Bind Name}" 
                                  IsChecked="{x:Bind BoolValue, Mode=TwoWay}"
                                  Visibility="{x:Bind IsCheckBox}" />
                        <ComboBox ItemsSource="{x:Bind EnumValues}"
                                  SelectedItem="{x:Bind EnumValue, Mode=TwoWay}"
                                  Visibility="{x:Bind IsComboBox}" />
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </StackPanel>
</ScrollViewer>
```

Benötigt zusätzliche Properties in PropertyViewModel:
```csharp
public bool IsTextBox => !IsEnum && !IsReference && PropertyType != typeof(bool);
public bool IsCheckBox => PropertyType == typeof(bool);
public bool IsComboBox => IsEnum;
```

### **Option B: ItemsRepeater statt ItemsControl**

```xaml
<ItemsRepeater ItemsSource="{x:Bind ViewModel.Properties, Mode=OneWay}">
    <ItemsRepeater.Layout>
        <StackLayout Spacing="8" />
    </ItemsRepeater.Layout>
    <ItemsRepeater.ItemTemplate>
        <DataTemplate x:DataType="vm:PropertyViewModel">
            <ContentControl 
                Content="{x:Bind}"
                ContentTemplateSelector="{StaticResource PropertyTemplateSelector}" />
        </DataTemplate>
    </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

---

## 📝 Zusammenfassung der Änderungen

### **ExplorerPage.xaml**
```diff
  <Grid Grid.Column="2">
      <Grid.RowDefinitions>
-         <RowDefinition Height="Auto" />
-         <RowDefinition Height="*" />
-         <RowDefinition Height="Auto" />
+         <RowDefinition Height="*" MinHeight="200" />     ← Properties (resizable)
+         <RowDefinition Height="8" />                     ← Splitter
+         <RowDefinition Height="300" MinHeight="150" />   ← Cities (300px default)
      </Grid.RowDefinitions>

+     <!--  Properties (Row 0)  -->
+     <!--  Splitter (Row 1)  -->
+     <!--  Cities (Row 2)  -->
  </Grid>
```

### **MainWindow.xaml**
```diff
  <NavigationView
-     PaneTitle="MOBAflow"
      PaneDisplayMode="Left">
      
      <NavigationView.PaneHeader>
          <StackPanel Orientation="Horizontal">
              <FontIcon Glyph="&#xE81D;" />
-             <TextBlock Text="Railway Control" />
+             <TextBlock Text="MOBAflow" Style="{StaticResource TitleTextBlockStyle}" FontWeight="SemiBold" />
          </StackPanel>
      </NavigationView.PaneHeader>
  </NavigationView>
```

---

## 🎯 Erwartetes Ergebnis

Nach den Fixes sollte:

1. ✅ **Navigation** - Kein Overlap oben links
2. ✅ **Properties** - Werden angezeigt wenn Node ausgewählt (Debug Counter zeigt Anzahl)
3. ✅ **Cities** - Immer sichtbar im unteren Bereich
4. ✅ **Splitter** - Oranger horizontaler Splitter zwischen Properties/Cities
5. ✅ **Layout** - Beide Bereiche gleichzeitig nutzbar

**Wenn Properties immer noch leer sind**, bitte:
- Debug Counter Wert mitteilen
- Visual Studio Output Window Logs teilen
- Screenshot vom ExplorerPage Layout

Dann können wir gezielt das richtige Problem beheben!
