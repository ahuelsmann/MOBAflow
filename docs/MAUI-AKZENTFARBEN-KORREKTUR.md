# MAUI MainPage.xaml - Akzentfarben Korrektur-Anleitung

## 🎨 **Was muss korrigiert werden:**

### **1. Lap Time - Orange Akzentfarbe** 🟠

**Zeile 461:** `LastLapTimeFormatted` Label

**❌ AKTUELL:**
```xaml
<Label
    FontAttributes="Bold"
    FontSize="10"
    Text="{Binding LastLapTimeFormatted}"
    TextColor="{DynamicResource RailwaySecondary}" />
```

**✅ KORRIGIERT:**
```xaml
<Label
    FontAttributes="Bold"
    FontSize="10"
    Text="{Binding LastLapTimeFormatted}"
    TextColor="{DynamicResource RailwayWarning}" />
```

---

### **2. Counter Badge - Blaue Akzentfarbe** 🔵

**Zeile 234-247:** `CountOfFeedbackPoints` Counter Badge  
**Zeile 285-298:** `GlobalTargetLapCount` Counter Badge

**❌ AKTUELL:**
```xaml
<Border
    Padding="10,4"
    BackgroundColor="{DynamicResource SurfaceDark}"  <!-- ❌ Neutral -->
    StrokeShape="RoundRectangle 4"
    StrokeThickness="0"
    WidthRequest="44">
    <Label
        FontAttributes="Bold"
        FontSize="15"
        HorizontalOptions="Center"
        Text="{Binding CountOfFeedbackPoints}"
        TextColor="{DynamicResource TextPrimary}"  <!-- ❌ Neutral -->
        VerticalOptions="Center" />
</Border>
```

**✅ KORRIGIERT:**
```xaml
<Border
    Padding="10,4"
    BackgroundColor="{DynamicResource RailwayAccent}"  <!-- ✅ Blau -->
    StrokeShape="RoundRectangle 4"
    StrokeThickness="0"
    WidthRequest="44">
    <Label
        FontAttributes="Bold"
        FontSize="15"
        HorizontalOptions="Center"
        Text="{Binding CountOfFeedbackPoints}"
        TextColor="White"  <!-- ✅ Weiß auf blauem Hintergrund -->
        VerticalOptions="Center" />
</Border>
```

---

### **3. Counter-Labels OBEN platzieren** 📐

**Zeile 211-260:** Tracks Counter  
**Zeile 262-311:** Target Counter

**❌ AKTUELL:** Labels sind LINKS vom Counter (Grid Layout)
**✅ GEWÜNSCHT:** Labels sind OBEN über dem Counter (VerticalStackLayout)

**Struktur:**
```
AKTUELL (Grid):                    GEWÜNSCHT (VerticalStackLayout):
┌─────────────────────────┐       ┌─────────────────────────┐
│ Tracks  [− 42 +]       │       │       Tracks            │
└─────────────────────────┘       │     [− 42 +]           │
                                   └─────────────────────────┘
```

**✅ KORRIGIERT:** (Zeile 206-252)
```xaml
<VerticalStackLayout Grid.Column="0" Spacing="6">
    <!-- Label OBEN -->
    <Label
        FontSize="11"
        Text="Tracks"
        TextColor="{DynamicResource TextSecondary}"
        HorizontalOptions="Center" />
    
    <!-- Counter UNTEN -->
    <Border
        Padding="10,8"
        BackgroundColor="{DynamicResource SurfaceDark}"
        StrokeShape="RoundRectangle 6"
        StrokeThickness="0">
        <HorizontalStackLayout Spacing="6" HorizontalOptions="Center">
            <Button ... Text="−" />
            <Border BackgroundColor="{DynamicResource RailwayAccent}">
                <Label Text="{Binding CountOfFeedbackPoints}" TextColor="White" />
            </Border>
            <Button ... Text="+" />
        </HorizontalStackLayout>
    </Border>
</VerticalStackLayout>
```

---

## 📋 **Zusammenfassung aller Änderungen:**

| Element | Zeile | Eigenschaft | Von | Nach |
|---------|-------|-------------|-----|------|
| Lap Time Label | 461 | TextColor | `RailwaySecondary` | `RailwayWarning` 🟠 |
| Tracks Counter Badge | 234-247 | BackgroundColor | `SurfaceDark` | `RailwayAccent` 🔵 |
| Tracks Counter Badge | 240-246 | TextColor | `TextPrimary` | `White` |
| Target Counter Badge | 285-298 | BackgroundColor | `SurfaceDark` | `RailwayAccent` 🔵 |
| Target Counter Badge | 291-297 | TextColor | `TextPrimary` | `White` |
| Tracks Layout | 211-260 | Grid → VerticalStackLayout | Label links | Label oben |
| Target Layout | 262-311 | Grid → VerticalStackLayout | Label links | Label oben |

---

## ✅ **Switches (bereits korrekt!):**

- **Connection Switch (Zeile 90):** `OnColor="{DynamicResource RailwayAccent}"` 🟢 ✅
- **Track Power Switch (Zeile 117):** `OnColor="{DynamicResource RailwayWarning}"` 🟡 ✅

---

## 🔧 **Manuelle Schritte:**

1. Öffne `MAUI/MainPage.xaml` in Visual Studio
2. **Zeile 461:** Ändere `RailwaySecondary` → `RailwayWarning`
3. **Zeile 211-260:** Ersetze Grid mit VerticalStackLayout, Label oben, Counter unten
4. **Zeile 234-247:** Counter Badge Background → `RailwayAccent`, TextColor → `White`
5. **Zeile 262-311:** Wiederhole für Target Counter
6. **Zeile 285-298:** Counter Badge Background → `RailwayAccent`, TextColor → `White`
7. Build testen

---

**Grund für manuelle Änderung:** `edit_file` Tool funktioniert nicht bei langen XAML-Dateien (Bug).
