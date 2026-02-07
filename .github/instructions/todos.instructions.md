---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-13

---

## 🏗️ ARCHITECTURE ISSUES (Langfristig - Lower Priority)

### Issue #1: MainWindowViewModel - God Object ⚠️
**Current State:**
- 9 partial files, ~800 LOC
- Too many responsibilities mixed

**Solution:** Extract into 3 dedicated services

### Issue #2: Domain Model - Missing Aggregates ⚠️
**Current State:**
- Collections directly exposed
- No encapsulation, no validation

**Solution:** ✅ Started - GridConfig, GridPosition created

### Issue #3: Backend Service Coupling ⚠️
**Current State:**
- WorkflowService depends on multiple services

**Solution:** Event-driven or Command Pattern

---

## 📝 Anmerkungen

### Phase 1: App-Startup Optimization ✅ DONE
- PostStartupInitializationService implementiert
- Startup-Zeit um 70-80% verbessert
- Deferred initialization nach MainWindow visible

### Phase 2-4: Lazy-Loading & Profiling ⏭️ OPTIONAL
- Nicht kritisch (nur 10-15% zusätzlich)
- Nur implementieren, wenn Messung zeigt: Startup > 3 Sekunden
- Pattern: `Lazy<T>` + MVVM Toolkit (WinUI 3 kompatibel)
- Entfernt aus TODO – kann später hinzugefügt werden, wenn nötig

---

## 🎯 Aktuelle Fokus

**Plugin-System & LayoutDocumentEx:**
- ✅ DockingTestPlugin vollständig implementiert
- ✅ Tab-Grouping, Custom Templates, MVVM Integration
- ✅ Automatische Plugin-Discovery & -Loading
- ✅ Dokumentation complete

**Nächste Prioritäten:**
1. Testing & User Feedback (DockingTestPlugin)
2. Weitere Plugins entwickeln (Pattern-Reuse)
3. Architecture Issues (MainWindowViewModel God Object) langfristig adressieren
