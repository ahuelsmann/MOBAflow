# PropertyGrid Refactoring Lessons Learned (Dec 2025)

## 🎯 **Problem: SimplePropertyGrid (Altlast)**

### **Was war falsch?**

**SimplePropertyGrid** war eine **custom Reflection-basierte Lösung**:
```csharp
// ❌ ANTI-PATTERN: Custom PropertyGrid mit Reflection
public class SimplePropertyGrid : UserControl
{
    private void RefreshProperties()
    {
        var properties = SelectedObject?.GetType().GetProperties(); // Reflection!
        foreach (var prop in properties)
        {
            var textBox = new TextBox();
            textBox.SetBinding(...); // Generisches Binding
        }
    }
}
```

**Probleme:**
1. ❌ **Performance:** Reflection zur Laufzeit (langsam)
2. ❌ **Komplexität:** ~350 Zeilen Custom-Code für etwas, das XAML nativ kann
3. ❌ **Wartbarkeit:** Schwer anzupassen, keine Design-Time-Unterstützung
4. ❌ **Not WinUI-native:** Reinventing the wheel statt Platform-Features nutzen
5. ❌ **ClearOtherSelections-Chaos:** Komplexe Selection-Logik nötig

---

## ✅ **Lösung: ContentControl + DataTemplateSelector (WinUI 3 Standard)**

### **Die moderne WinUI 3 Lösung:**

**1. EntityTemplateSelector (Type-basiert):**
```csharp
public class EntityTemplateSelector : DataTemplateSelector
{
    public DataTemplate? JourneyTemplate { get; set; }
    public DataTemplate? StationTemplate { get; set; }
    
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            JourneyViewModel => JourneyTemplate,
            StationViewModel => StationTemplate,
            _ => DefaultTemplate
        };
    }
}
```

**2. Type-Specific Templates (Fluent Design 2):**
```xaml
<DataTemplate x:Key="JourneyTemplate" x:DataType="vm:JourneyViewModel">
    <ScrollViewer>
        <StackPanel Padding="16" Spacing="16">
            <TextBox Header="Name" Text="{x:Bind Name, Mode=TwoWay}"/>
            <NumberBox Header="InPort" Value="{x:Bind InPort, Mode=TwoWay}"
                       SpinButtonPlacementMode="Inline"/>
            <ComboBox Header="Behavior On Last Stop"
                      ItemsSource="{x:Bind BehaviorOnLastStopValues}"
                      SelectedItem="{x:Bind BehaviorOnLastStop, Mode=TwoWay}"/>
        </StackPanel>
    </ScrollViewer>
</DataTemplate>
```

**3. ContentControl in UI:**
```xaml
<ContentControl Content="{Binding CurrentSelectedObject, Mode=OneWay}"
                ContentTemplateSelector="{StaticResource EntityTemplateSelector}"
                HorizontalContentAlignment="Stretch"
                VerticalContentAlignment="Stretch" />
```

---

## 📊 **Vorher/Nachher Vergleich**

| Aspekt | SimplePropertyGrid (ALT) | ContentControl + DataTemplates (NEU) |
|--------|--------------------------|--------------------------------------|
| **Code-Menge** | ~350 Zeilen C# | ~200 Zeilen XAML |
| **Performance** | ❌ Reflection (Runtime) | ✅ Compiled Bindings (x:Bind) |
| **Flexibilität** | ❌ Alle Properties gleich | ✅ Type-spezifisch optimiert |
| **Design-Time** | ❌ Kein IntelliSense | ✅ IntelliSense + Live Preview |
| **Wartbarkeit** | ❌ Komplexer C#-Code | ✅ Deklaratives XAML |
| **WinUI-Native** | ❌ Custom Control | ✅ Platform-Standard |
| **Selection-Logik** | ❌ ClearOtherSelections nötig | ✅ Automatisch durch Template-Switch |

---

## 🧹 **Entfernte Altlasten**

### **Dateien gelöscht:**
- ❌ `WinUI/Controls/SimplePropertyGrid.xaml` (~50 Zeilen)
- ❌ `WinUI/Controls/SimplePropertyGrid.cs` (~300 Zeilen)

### **Code entfernt:**
- ❌ `ClearOtherSelections(MobaType)` (~35 Zeilen) - Unnötig mit ContentControl
- ❌ `RefreshPropertyGrid()` (~8 Zeilen) - Binding macht das automatisch
- ❌ `_clearOtherSelections` Parameter in EntitySelectionManager

### **Gesamt-Einsparung:**
- **~480 Zeilen Code entfernt** (70% Reduktion!)
- **Komplexität halbiert**

---

## 🎨 **Fluent Design 2 Best Practices**

### **Spacing & Layout:**
```xaml
<StackPanel Padding="16" Spacing="16">  <!-- Consistent 16px -->
    <TextBlock Style="{ThemeResource SubtitleTextBlockStyle}"/>  <!-- Typography -->
    <TextBox Header="Name" PlaceholderText="Enter name"/>  <!-- Accessible -->
</StackPanel>
```

### **Modern Controls:**
- ✅ **NumberBox** mit `SpinButtonPlacementMode="Inline"`
- ✅ **TimePicker** für Schedule
- ✅ **ComboBox** mit ItemsSource-Binding
- ✅ **CheckBox** statt ToggleSwitch (simple boolean)

### **Theme-Aware:**
```xaml
<TextBlock Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
```

---

## 🔍 **Warum war das vorher nicht aufgefallen?**

### **Analyse-Fehler:**
1. ❌ **Zu oberflächlich:** "Es funktioniert" ≠ "Es ist gut designed"
2. ❌ **Keine Platform-Patterns geprüft:** Was ist WinUI-Standard?
3. ❌ **Performance nicht hinterfragt:** Reflection ist ein Red Flag
4. ❌ **Custom Controls nicht kritisch betrachtet:** Warum reinventing the wheel?

---

## ✅ **Verbesserte Analyse-Checkliste**

### **Bei jedem Custom Control fragen:**
1. 📌 **Gibt es ein Platform-Äquivalent?** (z.B. ContentControl + DataTemplateSelector)
2. 📌 **Nutzt es Reflection?** → Performance-Problem
3. 📌 **Ist es >200 Zeilen?** → Wahrscheinlich zu komplex
4. 📌 **Hat es Design-Time-Support?** (IntelliSense, Live Preview)
5. 📌 **Folgt es Fluent Design 2?** (Spacing, Typography, Theme)

### **Bei jedem Helper/Manager fragen:**
1. 📌 **Könnte Platform-Binding das lösen?** (OneWay, TwoWay, UpdateSourceTrigger)
2. 📌 **Gibt es verschachtelte If-Logik?** → Simplify or refactor
3. 📌 **Mehr als 50 Zeilen?** → Splitting prüfen

### **Bei jeder Architektur-Entscheidung fragen:**
1. 📌 **Ist das MVVM-konform?** (Logic in ViewModel, nicht Code-Behind)
2. 📌 **Nutzt es WinUI 3 Features?** (x:Bind, DataTemplateSelector, etc.)
3. 📌 **Ist es testbar?** (Dependency Injection, keine static)

---

## 🚀 **Neue Best Practices für MOBAflow**

### **UI-Patterns:**
✅ **ContentControl + DataTemplateSelector** für type-basierte UI  
✅ **x:Bind statt Binding** (Performance + Type-Safety)  
✅ **Fluent Design 2** (16px Spacing, SubtitleTextBlockStyle, Theme-aware)  
✅ **Keine Custom Controls** für Standard-Szenarien  

### **Selection-Patterns:**
✅ **CurrentSelectedObject** gibt das anzuzeigende Objekt zurück  
✅ **Template-Selektor** wählt automatisch  
✅ **Keine manuelle Cleanup-Logik** (ClearOtherSelections obsolet)  

### **Code-Qualität:**
✅ **Deklarativ > Imperativ** (XAML > C# für UI)  
✅ **Platform > Custom** (WinUI Features > Reinventing)  
✅ **Simple > Complex** (weniger Code = weniger Bugs)  

---

## 📚 **Referenzen**

- **ContentControl:** https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.contentcontrol
- **DataTemplateSelector:** https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.datatemplateselector
- **Fluent Design 2:** https://fluent2.microsoft.design/
- **x:Bind:** https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension

---

**Last Updated:** 2025-12-08  
**Author:** Refactoring Session - PropertyGrid Modernization
