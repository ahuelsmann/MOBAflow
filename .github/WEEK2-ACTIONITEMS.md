# 📋 Week 2 Action Items - Übersicht

> **Session:** 2026-01-22  
> **Status:** Planning Complete ✅  
> **Next:** Execute Tasks

---

## 🎯 What We Discussed

### 1️⃣ **Week 2 Aufgaben (Domain Tests)**

**Ziel:** 100% Dokumentation + 100% Test Coverage für Domain Layer

**Zu erledigen:**

```
DOKUMENTATION (XML-Comments):
├── 11 Domain Enums         (SignalAspect, SwitchPosition, ...)
├── WorkflowAction.cs       (<param> tags)
└── Project.cs              (Methoden-Docs)

TESTS (Unit Tests schreiben):
├── JourneyTests.cs
├── StationTests.cs
├── WorkflowTests.cs
├── TrainTests.cs
├── ProjectTests.cs
└── EnumTests.cs
```

📊 **Reference:** `docs/TEST-COVERAGE.md` (zum Tracking)

---

### 2️⃣ **VSM Coverage Plan (Responsive Layouts)**

**Ziel:** Alle Pages responsive machen (mobil → desktop)

**Status-Matrix:**

| Page | Status | Aktion |
|------|--------|--------|
| TrainControlPage | ✅ DONE | - |
| TrackPlanEditorPage | ✅ DONE | - |
| **WorkflowsPage** | ❌ TODO | **PRIORITY: HIGH - Next!** |
| JourneysPage | ❓ AUDIT | Check if needed |
| TrainsPage | ❓ AUDIT | Check if needed |
| SettingsPage | ❓ AUDIT | Check if needed |

📚 **Reference:** `docs/VSM-COVERAGE.md` (neu erstellt!)

**WorkflowsPage Spec:**
- **Wide (1200px+):** List (25%) + Editor (75%) side-by-side
- **Medium (641-1199px):** List full, Editor modal
- **Compact (0-640px):** List full, swipe to edit

---

### 3️⃣ **Skin Integration (Consolidation)**

**Problem:**
```
JETZT (Duplikation):
├── TrainControlPage   (alt)
├── TrainControlPage2  (neu mit Skins)
├── SignalBoxPage      (alt)
└── SignalBoxPage2     (neu mit Skins)
```

**SPÄTER (Konsolidiert):**
```
├── TrainControl       (= Page2, mit "Original" als extra Skin)
├── SignalBox          (= Page2, mit "Original" als extra Skin)
   └── Layout Selector:
       ├── Original    (← von alter Page)
       ├── Modern      (← aktuell)
       ├── ESU
       └── Märklin
```

**Strategie (6 Phasen):**

1. Create "Original" Theme in `ThemeResourceBuilder.cs`
2. Add Layout Enums (`TrainControlLayout`, `SignalBoxLayout`)
3. Refactor Page2 classes (dual selector + layout rendering)
4. Update NavigationRegistry (point to Page2)
5. Update DI registrations
6. Delete old pages + cleanup

📚 **Reference:** `docs/SKIN-INTEGRATION-ROADMAP.md` (neu erstellt!)

---

## 📅 Priorisierung für Week 2

```
PRIORITY 1 (Sofort):
└── Refactor WorkflowsPage mit VSM
    └── Task: Create VSM structure (Compact/Medium/Wide states)
    └── Estimated: 2-3 Stunden

PRIORITY 2 (Parallel):
└── Domain Tests (Week 2 requirement)
    └── Docs: 11 Enums + 2 Klassen
    └── Tests: 6 Test-Dateien schreiben
    └── Estimated: 4-6 Stunden

PRIORITY 3 (Week 3):
└── Skin Integration (optional)
    └── Phase 1-3: Refactoring Page2 classes
    └── Phase 4-6: Cleanup + deletion
    └── Estimated: 8-12 Stunden
```

---

## 📊 Dokumentation Neu Erstellt

| Datei | Zweck | Status |
|-------|-------|--------|
| `docs/VSM-COVERAGE.md` | All Pages audit + VSM status | ✅ DONE |
| `docs/SKIN-INTEGRATION-ROADMAP.md` | Consolidation strategy | ✅ DONE |
| `.github/instructions/.copilot-todos.md` | (existierend, wird aktualisiert) | 📝 TODO |

---

## 🔗 Quick Links

1. **VSM Dokumentation:** `docs/VSM-COVERAGE.md`
   - Aktuelle Status aller Pages
   - Responsive Breakpoints
   - Copy-Paste Template

2. **Skin Roadmap:** `docs/SKIN-INTEGRATION-ROADMAP.md`
   - 6-phasiger Plan
   - Code Beispiele
   - Checklist

3. **WinUI Best Practices:** `.github/instructions/winui.instructions.md`
   - VisualStateManager Pattern
   - Responsive Layout Beispiele
   - CommandBar Overflow Strategie

---

## ✅ Nächste Schritte

### Sofort (diese Session):
- [ ] Review `docs/VSM-COVERAGE.md` + `docs/SKIN-INTEGRATION-ROADMAP.md`
- [ ] Entscheidung: WorkflowsPage jetzt refactoren?
- [ ] Entscheidung: Skin Integration starten?

### Week 2 Kick-Off:
- [ ] Domain Tests schreiben (11 Enums + 6 Klassen)
- [ ] WorkflowsPage VSM implementieren
- [ ] Parallel: Skin Integration Phase 1-2 starten?

### Tracking:
- [ ] `.copilot-todos.md` updaten mit neuen Details
- [ ] Test Coverage Report updaten (`docs/TEST-COVERAGE.md`)

---

## 💡 Key Insights

✅ **VSM ist gut dokumentiert** → Copy-Paste Template vorhanden  
✅ **Skin Integration ist machbar** → 6-phasiger Plan vorhanden  
✅ **Keine blockers** → Kann sofort starten  
❓ **Frage:** WorkflowsPage VSM jetzt oder nach Domain Tests?  
❓ **Frage:** Skin Integration in Week 2 oder Week 3+?

---

**Status:** 🟢 Ready to execute  
**Generated:** 2026-01-22  
**Modified by:** Copilot
