---
description: 'Plan-Completion Guidelines - Validierung, Build-Überprüfung, offene Aufgaben'
applyTo: '**'
---

# Plan-Completion Guidelines

> **CRITICAL:** Jeden Plan mit einer Final Validation Phase abschließen. Unvollständige Implementierungen explizit dokumentieren.

## 📋 Plan-Struktur Anforderungen

### Immer enthalten:
1. **Main Steps (1-10):** Atomic work units with clear deliverables
2. **Substeps (optional):** Breakdown details for complex steps (NOT tracked individually)
3. **Final Validation Step:** Explizite Abschlussprüfung vor finish_plan()
4. **Potential Blockers Section:** Bekannte Risiken oder Abhängigkeiten

### Beispiel-Plan:
```markdown
# Feature: Dashboard Redesign
## Steps
1. Create DashboardViewModel
2. Build XAML UI layout
3. Add data bindings
4. Implement refresh logic
5. Validate build and UI

## Potential Blockers
- DashboardService API not yet implemented
- Design assets in progress
- Test resources need update
```

---

## 🔍 Final Validation Checklist

**Vor `finish_plan()` IMMER überprüfen:**

### ✅ Completion Criteria
- [ ] Alle Main Steps `completed` oder `skipped` (kein `pending` / `in-progress`)
- [ ] Kein `failed` Step ohne Anpassung und Dokumentation
- [ ] Build ohne kritische Fehler
- [ ] Tests (falls relevant) bestanden
- [ ] Code Reviews durchgeführt (bei Multi-File Changes)

### ✅ Documentation
- [ ] Neue public APIs dokumentiert
- [ ] Breaking Changes im Plan notiert
- [ ] README.md aktualisiert (falls relevant)
- [ ] Todos.instructions.md aktualisiert (für Session-Übergaben)

### ✅ Code Quality
- [ ] Code folgt MOBAflow Conventions (naming, SOLID, async patterns)
- [ ] Keine TODOs oder FIXME-Comments ohne Kontext
- [ ] ReSharper Warnings behoben oder dokumentiert
- [ ] Performance-kritische Paths überprüft (falls relevant)

---

## 🔧 Build Verification (CRITICAL)

**AM ENDE JEDES PLANS:**

```powershell
# Full build required
dotnet clean
dotnet build

# If tests exist
dotnet test --no-build --verbosity normal

# Verify no new warnings
```

### Akzeptable Ergebnisse:
- ✅ `Build: 0 erfolgreich, 0 Fehler, 0 Warnungen` → Proceed to finish_plan()
- ✅ `Build: 0 erfolgreich, 0 Fehler, [N] Warnungen` → Nur wenn pre-existing
- ❌ `Build: 0 erfolgreich, [N] Fehler` → STOP - fix before closing

---

## ⚠️ Handling Incomplete Work

### Szenario 1: Task in Plan nicht abgeschlossen
**Beispiel:** Step 5 (UI Styling) kann nicht wegen fehlender Design-Assets implementiert werden

**Aktion:**
1. `record_observation()`
2. `update_plan_progress(stepId, skipped, "Waiting for design assets from @user")`
3. Dokumentiere in `todos.instructions.md`:
   ```markdown
   ## 🚀 SESSION X+1 READY: UI Styling Implementation
   
   **Blocked:** Design assets not available yet
   - [ ] Obtain color palette from designer
   - [ ] Obtain icon set (.svg)
   - [ ] Obtain typography spec
   
   **Location:** DashboardPage.xaml → Update ControlStyles.xaml
   **Estimated:** 2-3 hours
   ```
4. `finish_plan()` mit SKIPPED Step

### Szenario 2: Unerwarteter Build-Fehler am Ende
**Beispiel:** Final build zeigt neue CS0000 Fehler

**Aktion:**
1. `record_observation("ERROR: ...")`
2. `run_build()` → diagnostizieren
3. Quick-Fix oder `adapt_plan()` je nach Umfang
4. Re-build
5. Erst dann `finish_plan()`

### Szenario 3: Step erfolgreich, aber mit Technical Debt
**Beispiel:** Feature implementiert, aber Performance könnte besser sein

**Aktion:**
1. Dokumentiere in `todos.instructions.md`:
   ```markdown
   ## 📊 Technical Debt: Performance Optimization
   
   **Location:** Backend/Z21.cs → OnUdpReceived()
   **Issue:** Event publishing in Publish loop → potential delay
   **Solution:** Consider async event publishing pattern
   **Priority:** LOW (functional but not optimal)
   **Session:** Consider for SESSION X+2
   ```
2. Step mit `completed` markieren
3. `finish_plan()` normal abschließen

---

## 📝 Todos.instructions.md Integration

**Nach jedem Session-Plan IMMER hier eintragen:**

```markdown
## ✅ SESSION X COMPLETED (2026-02-DD)

### What was implemented
- [x] Feature A (ABC.cs, DEF.cs)
- [x] Feature B (GHI.cs)

### Issues resolved
- [x] Build Error: CS0246 in Multiplex namespace
- [x] XAML Cache not clearing automatically

### Technical Debt Identified
- [ ] Z21.OnUdpReceived needs async refactor (low priority)
- [ ] Dashboard performance (high priority for next session)

### Status
- Build: ✅ Successful
- Tests: ✅ All passing
- Code Review: ⏳ Pending

---

## 🚀 SESSION X+1 READY: [Feature Name]

**Prerequisites:**
- [ ] Design assets from team
- [ ] Backend API endpoint deployed

**Files to Modify:** ...
**New Files:** ...
**Estimated Effort:** 4-6 hours
```

---

## 🎯 Pre-finish_plan() Checklist

Dieses Checklist **IMMER** vor `finish_plan()` durchlaufen:

```
□ Alle Main Steps status = completed | skipped | failed
□ Keine steps mehr in "pending" oder "in-progress"
□ run_build() erfolgreich (0 Fehler)
□ Neue public APIs dokumentiert
□ README.md / Todos.instructions.md aktualisiert
□ Code-Qualität überprüft (Naming, Conventions, no TODOs)
□ Falls Incomplete: todos.instructions.md mit Next-Steps aktualisiert
□ Alle Observations documented in plan oder TODO-file
□ Git status clean (wenn relevant)
```

---

## ✅ BEST PRACTICE: Plan Summary Template

Zum Abschließen eines Plans, verwende diese Summary:

```
## Plan Complete ✅

**Completed:**
- ✅ Feature X implemented in classes A, B, C
- ✅ 5 new test cases added
- ✅ Build successful (0 errors, 0 warnings)

**Incomplete / Deferred:**
- ⏳ Performance optimization (→ Session X+1)
- ⏳ UI Styling (blocked on assets)

**Files Modified:** 7 files
**Build Status:** ✅ Clean
**Next Steps:** See todos.instructions.md [SESSION X+1 READY]
```

---

## ❌ NEVER DO

- ❌ `finish_plan()` ohne final `run_build()`
- ❌ Offene Aufgaben in plan als "completed" markieren
- ❌ Build-Fehler ignorieren und "hoffen" es geht
- ❌ Incomplete work nicht in todos.instructions.md dokumentieren
- ❌ Session-Info in diese Datei (copilot-instructions.md) schreiben

---

## 📚 Related Files

- `todos.instructions.md` – Session history and upcoming work
- `copilot-instructions.md` – Permanent rules (this is enforcement layer)
- Plan logs in `.github/` (auto-archived)
