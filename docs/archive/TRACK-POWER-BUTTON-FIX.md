# 🔧 Track Power Button & Temperature Display Fix

**Datum**: 2025-11-28  
**Status**: ⚠️ **IN PROGRESS**

---

## 🐛 **Probleme:**

### **1. Track Power Button funktioniert nicht**

**Ursache**: Der Button **existiert nicht** in der UI!

**Lösung**: Button zur OverviewPage.xaml hinzufügen mit:
- Command Binding zu `SetTrackPowerCommand`
- CommandParameter für ON/OFF
- Visual Feedback für aktuellen Status (IsTrackPowerOn)
- ToggleButton oder zwei separate Buttons

### **2. Temperatur zeigt falsche Zeichen**

**Problem**: Nach "25" erscheint weiterhin "Ã,Â°C" oder ähnlich

**Ursache**: 
- Entweder in der OverviewPage.xaml selbst
- Oder in SystemStateCard.razor (Blazor)
- UTF-8 Encoding Problem mit Grad-Symbol (°)

**Lösung**: 
- Überall nur "C" statt "°C" verwenden
- ODER: Unicode Escape-Sequenz verwenden: `\u00B0C`

---

## 📋 **Notwendige Änderungen:**

### **A) Track Power Button hinzufügen**

**Datei**: `WinUI/View/OverviewPage.xaml`

**Position**: Im Connection Status Bereich (neben Connect/Disconnect)

**Optionen:**

#### **Option 1: Toggle Button (empfohlen)**
```xaml
<ToggleButton
    Content="{x:Bind ViewModel.IsTrackPowerOn, Mode=OneWay, Converter={StaticResource PowerButtonTextConverter}}"
    IsChecked="{x:Bind ViewModel.IsTrackPowerOn, Mode=TwoWay}"
    IsEnabled="{x:Bind ViewModel.IsZ21Connected, Mode=OneWay}"
    Command="{x:Bind ViewModel.SetTrackPowerCommand}"
    CommandParameter="{Binding IsChecked, RelativeSource={RelativeSource Self}}"
    Padding="16,8"
    MinWidth="120">
    <ToggleButton.Resources>
        <SolidColorBrush x:Key="ToggleButtonBackgroundChecked" Color="#4CAF50" />
        <SolidColorBrush x:Key="ToggleButtonForegroundChecked" Color="White" />
    </ToggleButton.Resources>
</ToggleButton>
```

#### **Option 2: Zwei separate Buttons**
```xaml
<Button
    Content="Power ON"
    Command="{x:Bind ViewModel.SetTrackPowerCommand}"
    CommandParameter="{x:Bind sys:Boolean.TrueString}"
    IsEnabled="{x:Bind ViewModel.IsZ21Connected, Mode=OneWay}"
    Visibility="{x:Bind ViewModel.IsTrackPowerOn, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}"
    Background="#4CAF50"
    Foreground="White"
    Padding="16,8" />

<Button
    Content="Power OFF"
    Command="{x:Bind ViewModel.SetTrackPowerCommand}"
    CommandParameter="{x:Bind sys:Boolean.FalseString}"
    IsEnabled="{x:Bind ViewModel.IsZ21Connected, Mode=OneWay}"
    Visibility="{x:Bind ViewModel.IsTrackPowerOn, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}"
    Background="#F44336"
    Foreground="White"
    Padding="16,8" />
```

### **B) Value Converter erstellen (Option 1)**

**Datei**: `WinUI/Converters/PowerButtonTextConverter.cs`

```csharp
public class PowerButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? "Power OFF" : "Power ON";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
```

### **C) Temperatur-Anzeige fixen**

**Mehrere Dateien betroffen:**

1. **MainWindowViewModel.cs** ✅ (bereits gefixt - nur "C")
2. **OverviewPage.xaml** - prüfen ob dort auch Temperatur angezeigt wird
3. **SystemStateCard.razor** (Blazor) - falls vorhanden

**Überall ersetzen**:
```csharp
// ❌ FALSCH
$"{temperature}°C"

// ✅ RICHTIG (Option 1: Einfach)
$"{temperature}C"

// ✅ RICHTIG (Option 2: Unicode)
$"{temperature}\u00B0C"
```

---

## 🔍 **Nächste Schritte:**

1. [ ] OverviewPage.xaml öffnen und Struktur analysieren
2. [ ] Track Power Button an passender Stelle einfügen
3. [ ] Alle Temperatur-Anzeigen finden und fixen
4. [ ] Converter erstellen (falls Toggle Button)
5. [ ] Testen mit echter Z21 Hardware

---

## 📊 **Erwartetes Ergebnis:**

### **Track Power Button:**
```
[Disconnected]
  Connect  [disabled: Power ON]

[Connected]
  Disconnect  [Power ON ← grün]

[Connected, Power ON]
  Disconnect  [Power OFF ← rot]
```

### **Temperatur:**
```
❌ VORHER: Temperature: 25 Ã,Â°C
✅ NACHHER: Temperature: 25 C
```

---

## 🚀 **Implementation:**

Warten auf User-Bestätigung welche Button-Option gewünscht ist.
