# TrackPlan Color & Theme System Analysis

> Überprüfung aller Farben für Fluent Design, Light/Dark Theme Unterstützung, und Kontrast

---

## Current Color Palette

### Theme-Aware Colors (✅ Correct)
| Farbe | Quelle | Light Mode | Dark Mode | Verwendung |
|-------|--------|-----------|-----------|-----------|
| **Accent** | `SystemAccentColor` | Blue (#0078D4) | Blue (#0078D4) | Selected Tracks, Highlights |
| **Text Primary** | `TextFillColorPrimary` | Black | White | Normal Tracks |
| **Text Secondary** | `TextFillColorSecondary` | Gray | LightGray | Grid Lines |

### Static Colors (⚠️ Problematisch für Dark Mode)
| Farbe | Wert | Verwendung | Dark Mode Issue |
|-------|------|-----------|-----------------|
| **Orange (Warning)** | #FF8C00 | Ports (Open), Snap Preview | OK aber könnte lebendiger sein |
| **Green (Success)** | #22B14C | Ports (Connected) | OK but needs saturation check |
| **Red (Error)** | #C42B1C | Violations, Errors | ✅ Gut in beiden Modes |

### Ghost-Track (⚠️ Opacity-basiert)
```csharp
GhostOpacity = 0.75  // Halbtransparent
Farbe: _snapPreviewBrush.Color (Warning Orange)
```
**Problem**: Wenn Orange zu dunkel in Dark Mode, wird Ghost schlecht sichtbar!

### Grid Lines
```csharp
Opacity = 255 * 0.15 = 38.25 ≈ 38 (15% Opacity)
```
**Problem**: Bei TextSecondaryColor (Grey) ergibt 15% vielleicht zu wenig Kontrast

---

## Fluent Design Compliance

### ✅ Already Good
- System Accent Color für Primary Actions
- TextFillColorPrimary für Haupttext
- Proper Opacity für subtile Elemente

### ⚠️ Needs Improvement
1. **Port Colors** - Hardcoded RGB, nicht theme-aware
   - Orange und Green sollten SystemColor-basiert sein
2. **Snap Preview** - Nutzt Warning Orange, könnte aber SystemAccent sein
3. **Ghost Opacity** - 0.75 könnte in Dark Mode zu niedrig sein

### ❌ Fehler
1. **Foregrounds Property Error** (Line 1376, 1402) - Typo `Foregrounds` statt `Foreground`

---

## Recommended Improvements

### 1. Port Colors (Theme-Aware)
```csharp
// ALT (Hardcoded):
var attentionColor = Color.FromArgb(255, 255, 140, 0);

// NEU (Theme-Aware):
var warningColor = GetColorResource("SystemFillColorCaution", Color.FromArgb(255, 255, 140, 0));
var successColor = GetColorResource("SystemFillColorPositive", Color.FromArgb(255, 34, 177, 76));
```

### 2. Snap Preview (Better Visibility)
```csharp
// ALT: Orange (Attention)
_snapPreviewBrush = new SolidColorBrush(attentionColor);

// NEU: Accent + Glow Effect
_snapPreviewBrush = new SolidColorBrush(accentColor);  // Lebendiger in beiden Modes
```

### 3. Ghost Track (Higher Opacity in Dark Mode)
```csharp
// Dynamische Opacity basierend auf Theme
double ghostOpacity = ActualTheme == ElementTheme.Dark ? 0.85 : 0.75;
```

### 4. Grid Lines (Better Contrast)
```csharp
// ALT: 15% Opacity
_gridBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.15), gridColor.R, gridColor.G, gridColor.B));

// NEU: 25% für besseren Kontrast
_gridBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.25), gridColor.R, gridColor.G, gridColor.B));
```

---

## Contrast Ratios (WCAG AA)

| Element | Light Mode | Dark Mode | WCAG AA (4.5:1) |
|---------|-----------|-----------|-----------------|
| Text on Track | Black/Gray | White/LightGray | ✅ Pass |
| Port (Open) on Grid | Orange on White | Orange on Black | ⚠️ Check |
| Port (Connected) on Grid | Green on White | Green on Black | ✅ Pass |
| Ghost Track | Semi-Transparent | Semi-Transparent | ⚠️ Border Check |

---

## Action Items

1. [ ] Fix `Foregrounds` → `Foreground` Typo
2. [ ] Implement Theme-Aware Port Colors
3. [ ] Change Snap Preview to System Accent
4. [ ] Adjust Ghost Opacity for Dark Mode
5. [ ] Increase Grid Line Opacity to 25%
6. [ ] Test Light/Dark Theme Toggle
7. [ ] Verify WCAG Contrast Ratios
