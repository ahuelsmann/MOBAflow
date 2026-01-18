---
description: 'Minimale TODO-Liste fuer MOBAflow'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2026-01-23 (SignalBoxPage Stellwerk-Funktionalität)

---

## 🎯 AKTUELLE SESSION - SignalBoxPage Stellwerk

### ✅ HEUTE ERLEDIGT (2026-01-23)

**Gleisdarstellung:**
- ✅ Vereinfachte Gleislinien (keine Schwellen, einfache Linien/Bögen)
- ✅ Kurven mit kubischen Bezier-Kurven für exakte Endpunkte
- ✅ Verbindungspunkte standardisiert: (0,30), (60,30), (30,0), (30,60)
- ✅ 90°-Kurve: `M 0,30 C 0,13 13,0 30,0`
- ✅ 45°-Kurve: `M 0,30 C 30,30 30,0 60,0`

**Weichen:**
- ✅ Dreiwege-Weiche (SwitchThreeWay) hinzugefügt
- ✅ SwitchPosition Enum erweitert (Straight, DivergingLeft, DivergingRight)
- ✅ Alle Weichen zeigen aktive Stellung in Grün
- ✅ Doppelklick auf Weiche schaltet Stellung um
- ✅ Properties-Panel mit 3 Buttons (Gerade/Links/Rechts)

**Signale:**
- ✅ KsSignalScreen Blink-Animation für Ks1Blink und Zs1
- ✅ Doppelklick auf Signal schaltet Aspekt (Hp0→Ks1→Ks2→Hp0)

**Drag & Drop:**
- ✅ Bug behoben: Drag Gleis → Drop Signal
- ✅ Element-Drag nur wenn selektiert (verhindert Konflikt mit Toolbox)
- ✅ Debug-Logging für Drop-Events

**Instructions:**
- ✅ implementation-workflow.instructions.md erstellt (5-Schritte-Workflow)

### 🔧 NOCH OFFEN

**Kurven-Darstellung (Screenshot zeigt Problem):**
- [ ] Kurven-Endpunkte prüfen - verbinden sich noch nicht korrekt
- [ ] Rotation der Kurven testen (nach 90° Rotation korrekt?)

**DWW Drag & Drop:**
- [ ] Dreiwege-Weiche Drag & Drop testen (User meldete Problem)

---

## 🎯 PHASE 3: XAML REFACTORING + GRAPHICS UPGRADE (Week 3-5)

> **Current Status:** 🟡 **PAUSED - SignalBoxPage Funktionalität Priorität**

### Phase 0: KsSignalScreen Control Fix ✅ COMPLETE
- ✅ Fixed grid layout (Added Height="Auto" and Width="Auto")
- ✅ Increased circle size (20 → 24)
- ✅ Reduced spacing (16 → 12)
- ✅ KsSignalScreen now renders complete signal matrix correctly
- ✅ Blink-Animation für Ks1Blink und Zs1 hinzugefügt

### Phase 1-8: Siehe ursprüngliche Planung
*Zurückgestellt bis Stellwerk-Funktionalität stabil*

---

## 📊 LEGACY TASKS (Week 2 - Quality)

### Week 2: Quality Tasks ✅ PHASE 1-2 COMPLETE

**✅ COMPLETED (2026-01-22):**
- ✅ WorkflowsPage VSM refactoring (3 states)
- ✅ Header/Title color support with theme switching
- ✅ Fluent Design System implementation (spacing, cards, headers)
- ✅ Skin selector button on TrainControlPage and SignalBoxPage
- ✅ KsSignalScreen control layout fixed

**📌 NEXT (Quality):**
- [ ] Domain-Enums documentation (11 types)
- [ ] Test coverage improvements
- [ ] Other Pages VSM audit (JourneysPage, TrainsPage, SettingsPage)

---

## 🏗️ ARCHITECTURE DECISIONS

### C# vs XAML Trade-offs
- **HelpPage/InfoPage**: Pure XAML (simple content)
- **SignalBoxPageBase**: Hybrid (complex canvas operations stay in C#, layout in XAML)
- **SignalBoxPage**: Hybrid (header/toolbox in XAML, canvas logic in C#)
- **TrainControlPage**: Already XAML (best practices established)

### Skin Support Strategy
- Apply `HeaderBackgroundBrush` to page headers
- Apply `HeaderForegroundBrush` to titles
- Theme colors update automatically via `ApplyThemeColors()` in code-behind
- Use ThemeResources for standard controls

### Responsive Design
- VisualStateManager with 3 states: Compact (0-640px), Medium (641-1199px), Wide (1200px+)
- Grid columns adjust dynamically
- Toolbox/Properties panels collapse on Compact

---

## 📋 REGELN

1. XAML zuerst schreiben → Code-Behind Logik
2. Fluent Design System konsistent anwenden
3. Skin-Support auf allen neuen Pages
4. Responsive Layout mit VisualStateManager
5. Keine hardcodierten Farben (Theme-Resources verwenden)
6. Moderne SVG-basierte Grafiken für Symbole

---

## 🎉 ERFOLGSKRITERIEN

- ✅ Alle Pages verwenden XAML + Code-Behind
- ✅ Konsistente Fluent Design System Implementierung
- ✅ Skin-Switching funktioniert auf allen Pages
- ✅ Responsive Layout (VisualStateManager) aktiv
- ✅ KsSignalScreen und andere Grafiken professionell
- ✅ Ks-Signal Konfiguration vollständig funktional

