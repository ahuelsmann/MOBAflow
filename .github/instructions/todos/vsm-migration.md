---
description: 'VisualStateManager migration guidelines, best practices, and page status'
applyTo: '**/*.xaml'
---

# 🎨 VSM-Migration Guidelines

> **Ziel:** Responsive Layouts mit VisualStateManager
> **Status:** AKTIV - TrainControlPage & TrackPlanEditorPage fertig
> **Letztes Update:** 2026-01-18

---

## MOBAflow Breakpoints

| Breakpoint | Fensterbreite | Typischer Einsatz |
|------------|---------------|-------------------|
| **Compact** | 0-640px | Mobile, kleine Fenster |
| **Medium** | 641-1199px | Tablet, Landscape |
| **Wide** | 1200px+ | Desktop, großer Monitor |

---

## Page-Status

| Page | Status | Breakpoints |
|------|--------|-------------|
| **TrainControlPage.xaml** | ✅ FERTIG (2026-01-17) | Wide: 3 Spalten \| Medium: 2 Spalten \| Compact: Stack |
| **TrackPlanEditorPage.xaml** | ✅ FERTIG (2026-01-18) | Wide: 3 Spalten \| Medium: Canvas+Properties \| Compact: Canvas only |
| **SignalBoxPage.cs** | ⏳ NÄCHSTE | Code-Behind → XAML + VSM |
| **MainWindow.xaml** | ⏳ Geplant | Wide: Rail \| Compact: Hamburger |
| **WorkflowsPage.xaml** | ⏳ Geplant | Wide: Split \| Compact: Modal |
| **MonitorPage.xaml** | ✅ OK | Horizontal Logs |

---

## 🎯 Goldene Regel

```
┌──────────────────────────────────────────────────────────────────┐
│                  VSM = LAYOUT NUR (Separation!)                  │
│                                                                  │
│  ✅ VSM Setters dürfen ändern:                                  │
│     • Control.Visibility (Show/Hide Panels)                     │
│     • Grid.ColumnDefinitions Width (Spalten-Größe)             │
│     • Margin/Padding (kleine Spacing-Anpassungen)              │
│                                                                  │
│  ❌ VSM Setters dürfen NICHT ändern:                            │
│     • Style (NIEMALS!)                                          │
│     • Height/Width inline (gehört in Style!)                   │
│     • CornerRadius inline (gehört in Style!)                   │
│     • FontSize/FontWeight inline (gehört in Style!)            │
│                                                                  │
│  → UI/UX Polishing bleibt in Controls/Styles/Converter         │
│  → VSM macht nur Responsive-Layout                              │
└──────────────────────────────────────────────────────────────────┘
```

---

## Best Practice: Style + Binding Pattern

**✅ RICHTIG:**
```xaml
<ToggleButton
    Style="{StaticResource BacklightToggleButtonStyle}"
    Background="{x:Bind ViewModel.IsF0On, Mode=OneWay, 
                 Converter={StaticResource BacklightConverter}}"
    Command="{x:Bind ViewModel.ToggleF0Command}"
    Content="F0" />
```

**❌ FALSCH (verliert VisualStates!):**
```xaml
<ToggleButton
    Height="40"
    CornerRadius="6"
    FontSize="12"
    Background="{x:Bind ...}"
    Content="F0" />
```

---

## PRE-MIGRATION Checkliste

- [ ] Custom Styles identifizieren (ControlStyles.xaml)
- [ ] Converter dokumentieren (Page.Resources oder App.xaml)
- [ ] VisualStates in Custom Styles prüfen
- [ ] Layout-Planung für 3 Breakpoints skizzieren
- [ ] Test-Plan erstellen

## DURING-MIGRATION Checkliste

- [ ] NIEMALS Styles entfernen/ersetzen
- [ ] NIEMALS Bindings/Converter entfernen
- [ ] Grid.Row/Grid.Column nicht anfassen
- [ ] VSM Setters minimal halten
- [ ] Build nach jeder Änderung prüfen

## POST-MIGRATION Checkliste

- [ ] Build erfolgreich
- [ ] Visual Regression Test auf 3 Breakpoints
- [ ] Hover/Press/Disabled States funktional
- [ ] Alle Buttons klickbar
- [ ] Keine visuellen Artefakte

---

## Lessons Learned (2026-01-17)

**Issue:** Commit 2a22af7 ersetzte BacklightToggleButtonStyle mit inline Properties
- ❌ VisualStates (Hover, Press, Disabled) gingen verloren
- ✅ Fix: Alle F0-F20 Buttons zurück zu BacklightToggleButtonStyle

**Regel:** NIEMALS Custom Styles durch inline Properties ersetzen!

---

*Teil von: [.copilot-todos.md](../.copilot-todos.md)*
