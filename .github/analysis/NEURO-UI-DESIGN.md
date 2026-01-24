# Neuro-UI Design für TrackPlanPage

> Neuroscience-Based UX: Gehirngerechte Designentscheidungen für bessere Usability

---

## 🧠 Grundlagen: Neuro-UI Design

**Definition:** Designansatz, der Erkenntnisse aus Neurowissenschaft + Kognitionspsychologie nutzt, um intuitive, natürliche, gehirngerechte UIs zu schaffen.

### Wissenschaftliche Basis

| Konzept | Neurowissenschaft | Anwendung in TrackPlan |
|---------|-------------------|----------------------|
| **Kognitive Belastung** | Working Memory = 7±2 Items | Nicht alle Tracks gleichzeitig hervorheben |
| **Pattern Recognition** | Gehirn erkennt wiederkehrende Muster extrem schnell | Gleiche Farben/Symbole für Gleich-Typen |
| **Affordances** | Visuelles Design kommuniziert Funktion | Ports glühen = "hier kann ich snappen" |
| **Farbpsychologie** | Farben triggern unmittelbare emotionale Reaktionen | Orange=Warnung, Grün=OK |
| **Chunking** | Gehirn gruppiert Information in "Chunks" | Dimme Nicht-Relevantes während Drag |
| **Predictive Coding** | Gehirn erwartet vorhersagbare Sequenzen | Ghost-Bewegung muss smooth sein |
| **Temporal Processing** | Gehirn verarbeitet < 100ms als "instant" | Animationen müssen schnell sein |
| **Gestalt Laws** | Ähnlichkeit, Nähe, Kontinuität | Visual Grouping von Switch-Typen |

---

## ✅ Status: Was TrackPlanPage bereits richtig macht

```
✅ Farbkodierung
   - Orange = Port Open (Warnung: kann hier snappen)
   - Grün = Port Connected (Success: verbunden)
   - Blau = Accent für Selektion
   → Farbpsychologie: Benutzer versteht sofort ohne Text

✅ Dynamic Ghost Track
   - Zeigt kommende Position LIVE während Drag
   → Predictive Coding: Gehirn kann nächste Aktion vorhersehen

✅ Cursor Hidden während Drag
   - Reduziert visuelle Ablenkung
   - Fokus ausschließlich auf Ghost
   → Kognitive Belastung: Nur 1 Element in Focus

✅ Snap Preview mit Accent-Farbe
   - Visuell hervorgehobener Snap-Punkt
   - Kontrast zur Normal-Anzeige
   → Affordance: "Hier passiert etwas Wichtiges"

✅ Theme-Aware Colors (Light/Dark)
   - Augen-Strain reduziert
   - Arbeitet mit biologischem Rhythmus
   → Psychophysik: Optimale Kontraste in beiden Modes

✅ Dynamic Opacity (0.75 Light, 0.85 Dark)
   - Ghost-Sichtbarkeit in beiden Themes
   - Kontrast-optimiert
   → Gestalt Law: Figure-Ground Separation
```

---

## 🎯 Phase 9: Drei konkrete Neuro-UI Verbesserungen

### **9.1: Attention Control - "Chunking Pattern"**

**Problem:** Benutzer muss ALLE Tracks verarbeiten, während er 1 zieht
- Kognitives "Noise"
- Working Memory überfordert
- Gehirn kann sich nicht konzentrieren

**Neuro-Lösung: "Dim Irrelevant Content"**

```csharp
/// <summary>
/// Gehirn ignoriert schwache Signale automatisch (< 30% Opacity)
/// Selektive Aufmerksamkeit fokussiert auf relevante Tracks
/// </summary>
public void BeginMultiGhostPlacement(IReadOnlyList<Guid> trackIds)
{
    _viewModel.BeginMultiGhostPlacement(trackIds);
    DimIrrelevantTracks(trackIds);
}

private void DimIrrelevantTracks(IReadOnlyList<Guid> selectedTrackIds)
{
    foreach (var edge in _viewModel.Graph.Edges)
    {
        if (selectedTrackIds.Contains(edge.Id))
        {
            _trackOpacity[edge.Id] = 1.0;      // Full brightness for selected
            _trackStrokeWidth[edge.Id] = 6.0;  // Thicker for emphasis
        }
        else
        {
            _trackOpacity[edge.Id] = 0.25;     // Dim out (Gehirn ignoriert)
            _trackStrokeWidth[edge.Id] = 2.0;  // Thinner
        }
    }
    
    RenderGraph();
}

private void EndMultiGhostPlacement()
{
    // Restore all tracks
    foreach (var trackId in _viewModel.Graph.Edges.Select(e => e.Id))
    {
        _trackOpacity[trackId] = 1.0;
        _trackStrokeWidth[trackId] = 6.0;
    }
    
    RenderGraph();
}
```

**Neuro-Effekt:**
- Chunking: Gehirn reduziert Informationen auf "relevante" + "Hintergrund"
- Selektive Aufmerksamkeit: Working Memory arbeitet nur mit 1-2 Items
- Schnellere Entscheidung: Weniger mentale Ressourcen nötig

**Performance:** O(n) jedes Mal, aber nur während Drag (akzeptabel)

---

### **9.2: Type Indicators - "Pattern Recognition"**

**Problem:** Benutzer muss sich Weichen-Typen merken (kognitiv anstrengend)
- WL = Linkskurve (aber wie?)
- WR = Rechtskurve (aber wie?)
- W3 = Dreiweiche (aber wie?)

**Neuro-Lösung: "Visual Pattern Recognition"**

```csharp
/// <summary>
/// Kleine Unicode-Symbole + Farbkodierung für sofortige Mustererkennung
/// Gestalt Law: Ähnlichkeit - Gehirn gruppiert automatisch
/// </summary>
private void RenderSwitchWithTypeIndicator(
    Canvas canvas,
    Guid switchId,
    SwitchTemplate template,
    Point2D position,
    double rotation)
{
    // Original Switch rendern
    var primitives = SwitchGeometry.Render(template, position, rotation);
    // ... render primitives ...
    
    // Type Indicator hinzufügen (oben-links vom Switch)
    var typeIndicator = new TextBlock
    {
        Text = GetSwitchTypeSymbol(template.Id),
        FontFamily = new FontFamily("Segoe UI Symbol"),
        FontSize = 8,
        FontWeight = FontWeights.Bold,
        Opacity = 0.5,
        Foreground = new SolidColorBrush(GetSwitchTypeColor(template.Id))
    };
    
    var indicatorX = position.X * DisplayScale - 8;
    var indicatorY = position.Y * DisplayScale - 10;
    Canvas.SetLeft(typeIndicator, indicatorX);
    Canvas.SetTop(typeIndicator, indicatorY);
    canvas.Children.Add(typeIndicator);
}

private string GetSwitchTypeSymbol(string templateId)
{
    return templateId switch
    {
        // Left switches (linkes Symbol)
        _ when templateId.EndsWith("L", StringComparison.OrdinalIgnoreCase) => "◀",
        
        // Right switches (rechtes Symbol)
        _ when templateId.EndsWith("R", StringComparison.OrdinalIgnoreCase) => "▶",
        
        // Triple/three-way (triple Symbol)
        _ when templateId.Contains("W3", StringComparison.OrdinalIgnoreCase) => "▼",
        
        // Curved left (C-Form)
        _ when templateId.StartsWith("B", StringComparison.OrdinalIgnoreCase) => "◜",
        
        // Default
        _ => "◆"
    };
}

private Color GetSwitchTypeColor(string templateId)
{
    return templateId switch
    {
        // WL: Blau (kalt, "links")
        _ when templateId == "WL" => Color.FromArgb(255, 68, 114, 196),
        
        // WR: Rot (warm, "rechts")
        _ when templateId == "WR" => Color.FromArgb(255, 196, 49, 35),
        
        // W3: Grün (Triple = Balance/Dreieck)
        _ when templateId == "W3" => Color.FromArgb(255, 34, 177, 76),
        
        // Curved: Orange (Variation)
        _ when templateId.StartsWith("B") => Color.FromArgb(255, 255, 140, 0),
        
        // Default: Grau
        _ => Colors.Gray
    };
}
```

**Neuro-Effekt:**
- Gestalt Law (Ähnlichkeit): Gehirn gruppiert Switches gleichen Typs automatisch
- Pattern Recognition: Nach 2-3 Mal sieht Benutzer den Typ sofort (ohne zu lesen)
- Schnellere Entscheidung: Visuelle Information schneller als Text
- Arbeitsgedächtnis entlastet: Muss Typ nicht mehr merken

**Implementierung:**
- Größe: 8pt (subtil, nicht störend)
- Opacity: 0.5 (sichtbar aber nicht aufdringlich)
- Position: Top-Left von Switch (Leseverhalten: von oben-links nach unten-rechts)

---

### **9.3: Hover Affordances - "Interaction Signals"**

**Problem:** Benutzer weiß nicht, dass Ports/Tracks interaktiv sind
- Macht "falsches Gefühl"
- Keine visuellen Hinweise auf Interaktivität
- Gehirn lernt keine neuen Affordances

**Neuro-Lösung: "Hover State Feedback"**

```csharp
/// <summary>
/// Ports zeigen BEVOR man interagiert, dass sie interaktiv sind
/// Affordances: Gehirn lernt "Ich kann hier klicken/snappen"
/// </summary>
private void RenderPort(
    Canvas canvas,
    Port port,
    Point2D position,
    bool isConnected)
{
    var circle = new Ellipse
    {
        Width = PortRadius * 2,
        Height = PortRadius * 2,
        Fill = isConnected ? _portConnectedBrush : _portOpenBrush,
        Stroke = new SolidColorBrush(Colors.Transparent),
        StrokeThickness = 0,
        Opacity = 0.6  // Base: dimmed (recessive)
    };
    
    Canvas.SetLeft(circle, position.X * DisplayScale - PortRadius);
    Canvas.SetTop(circle, position.Y * DisplayScale - PortRadius);
    canvas.Children.Add(circle);
    
    // Hover-Effekt: Affordance zeigen
    circle.PointerEntered += (_, _) =>
    {
        circle.Opacity = 1.0;                                    // Voll sichtbar
        circle.Stroke = new SolidColorBrush(Colors.White);       // Weißer Rand
        circle.StrokeThickness = 2;                              // Stärker betont
        
        // Optional: Sound-Feedback (auditory affordance)
        PlayHoverSound(port.Id);
    };
    
    circle.PointerExited += (_, _) =>
    {
        circle.Opacity = 0.6;                                    // Zurück zu dimmed
        circle.Stroke = new SolidColorBrush(Colors.Transparent);
        circle.StrokeThickness = 0;
    };
    
    circle.PointerPressed += (_, _) =>
    {
        // Snap-Initiierung
        circle.Opacity = 0.85;  // Auch etwas "gedrückt" aussehen
    };
}

/// <summary>
/// Hover-Effekt für Tracks: zeige dass sie draggbar sind
/// </summary>
private void RenderTrackWithHoverFeedback(
    Canvas canvas,
    Edge edge,
    bool isSelected)
{
    var trackShapes = CreateTrackShapes(edge);  // Existing method
    
    foreach (var shape in trackShapes)
    {
        shape.Opacity = 0.7;  // Base: Slightly dimmed
        
        shape.PointerEntered += (_, _) =>
        {
            if (!isSelected)
            {
                shape.Stroke = new SolidColorBrush(Colors.Yellow);  // Hover highlight
                shape.StrokeThickness = 2;
                shape.Opacity = 1.0;  // Full brightness on hover
            }
        };
        
        shape.PointerExited += (_, _) =>
        {
            if (!isSelected)
            {
                shape.Stroke = new SolidColorBrush(Colors.Transparent);
                shape.StrokeThickness = 0;
                shape.Opacity = 0.7;  // Back to dimmed
            }
        };
        
        canvas.Children.Add(shape);
    }
}

private void PlayHoverSound(string portId)
{
    // Auditory affordance: Benutzer HÖRT dass Port interaktiv ist
    // Optional: Subtles "beep" bei Port-Hover
    // Wichtig: Nicht aufdringlich (< 30dB, < 100ms)
    
    // Implementation würde ISoundEngine verwenden
}
```

**Neuro-Effekt:**
- Affordances (Don Norman): Visuelle Hinweise zeigen Interaktivität
- Wahrnehmung: Benutzer lernt durch Feedback "Das ist interaktiv"
- Gehirn-Lernen: Nach 2-3 Hovers merkt sich Benutzer Affordance
- Sicherheit: Benutzer traut sich zu interagieren (weiß, wo sicher geklickt werden kann)

**Optionale Enhancement:**
- Sound-Feedback: Subtiler "beep" bei Port-Hover (auditory affordance)
- Animation: Kleines Pulse beim Hover (temporal signal)
- Cursor-Change: Bereits in WinUI möglich (Hand-Cursor bei hover)

---

## 📊 Neuro-UI Verbesserungen: Vergleich

| Feature | Komplexität | Neuro-Effekt | Performance | User Benefit |
|---------|------------|-------------|-------------|--------------|
| **Attention Control (9.1)** | Mittel | Chunking ++ | O(n) in Drag | Fokus, weniger Stress |
| **Type Indicators (9.2)** | Einfach | Pattern Recognition ++ | O(1) statisch | Schnelle Erkennung |
| **Hover Affordances (9.3)** | Einfach | Affordances ++ | O(1) interactive | Vertrauen, Safety |

---

## 🚀 Implementierungs-Roadmap

### Schritt 1: Type Indicators (Einfach - 30 min)
```
1. GetSwitchTypeSymbol() Method hinzufügen
2. GetSwitchTypeColor() Method hinzufügen
3. RenderSwitchWithTypeIndicator() aufrufen statt RenderSwitch()
4. Test: Alle Switch-Typen zeigen richtige Symbole
```

### Schritt 2: Hover Affordances (Einfach - 20 min)
```
1. Port Hover-Handler hinzufügen (Opacity + Stroke)
2. Track Hover-Handler hinzufügen (Yellow highlight)
3. Optional: Sound-Feedback via ISoundEngine
4. Test: Hovers funktionieren für Ports + Tracks
```

### Schritt 3: Attention Control (Mittel - 40 min)
```
1. DimIrrelevantTracks() Method hinzufügen
2. BeginMultiGhostPlacement() → DimIrrelevantTracks() aufrufen
3. EndMultiGhostPlacement() → RestoreTracks() aufrufen
4. Test: Nur selected Tracks bleiben bright, andere dimmed
5. Performance: Check dass Drag smooth bleibt
```

---

## 🧪 Testing: Neuro-UI Feedback

**Wie man weiß, ob es funktioniert:**

- [ ] Benutzer findet Ports schneller (ohne zu suchen)
- [ ] Benutzer versteht Switch-Typen sofort (ohne zu fragen)
- [ ] Benutzer fühlt sich bei Drag-Operation fokussierter
- [ ] Weniger Fehler beim Snappen (Affordance half)
- [ ] Benutzer arbeitet schneller (gemessen in Tasks/minute)

---

## 📚 Weiterführende Ressourcen

**Bücher:**
- "The Design of Everyday Things" - Don Norman (Affordances)
- "Thinking, Fast and Slow" - Daniel Kahneman (System 1 vs 2)
- "Seductive Interaction Design" - Stephen Anderson (Emotional Design)

**Online:**
- Nielsen Norman Group: Cognitive Load
- W3C WCAG: Perceivable, Operable, Understandable, Robust (POUR)
- Gestalt Design Principles

---

## 🎯 Fazit

**MOBAflow TrackPlanPage hat bereits starke Neuro-UI Foundation:**
- Richtige Farbkodierung ✅
- Dynamic Ghost Track ✅
- Theme-Awareness ✅

**Mit Phase 9 kommt noch mehr:**
- Attention Control (Chunking)
- Type Indicators (Pattern Recognition)
- Hover Affordances (Interactivity Signals)

**Resultat:** Professionelle, gehirngerechte UI, die Benutzer schneller arbeiten lässt und weniger Fehler macht.

