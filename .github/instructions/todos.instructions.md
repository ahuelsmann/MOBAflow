---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-05 (Pre-GitHub Feature Completion Phase)

---

## ⚠️ PRE-GITHUB FEATURE COMPLETION (CRITICAL)

**Status:** 🚧 In Progress - Must complete before GitHub launch

### Core Features (Must-Have for v0.1.0)

#### 1. TrackPlanPage (Track Plan Editor)
- [x] Basic track placement (drag & drop from toolbox)
- [x] Snap-to-connect functionality
- [x] Grid alignment and rotation
- [x] Piko A-Gleis track library (basic templates)
- [x] **Save/Load track plans** (SignalBoxPlan wird mit Solution.json gespeichert/geladen)
- [ ] **Complete Piko A-Gleis library** (all article codes from catalog)
- [ ] **Feedback point assignment UI** (assign InPort to detector elements)
- [ ] **Route definition** (define routes between signals)
- [ ] **Validation constraints** (detect invalid track connections)
- [ ] **Track plan editing UI** (move, rotate, delete elements via mouse/touch)
- [ ] **Undo/Redo functionality**
- [ ] **Copy/Paste track elements**
- [ ] **Track plan export** (PNG/SVG for documentation)

**Priority:** 🔥 HIGH - Core feature for GitHub showcase

**Note:** TrackPlan (SignalBoxPlan) ist bereits im Domain-Model integriert und wird automatisch mit `Project.SignalBoxPlan` im Solution-JSON gespeichert/geladen. Die JSON-Serialisierung erfolgt über `System.Text.Json` mit Polymorphie-Support (`$type` discriminator für Track-Elemente).
