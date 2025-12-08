# Code Analysis & Architecture Review Best Practices

## 🎯 **Ziel: Systematische Erkennung von Altlasten und Anti-Patterns**

Diese Checkliste wurde erstellt nach dem PropertyGrid-Refactoring (Dez 2025), wo eine **~350-Zeilen Custom-Lösung** durch **native WinUI 3 Pattern** ersetzt wurde.

---

## 🔍 **Systematische Code-Analyse - 5-Schritt-Methode**

### **Schritt 1: Custom Controls & Helper kritisch prüfen**

Für **jede Custom-Klasse >100 Zeilen** fragen:

#### **Platform-Check:**
- ❓ **Gibt es ein natives WinUI 3 / .NET Äquivalent?**
  - Beispiel: `SimplePropertyGrid` → `ContentControl` + `DataTemplateSelector`
  - Beispiel: `CustomButton` → `Button` mit Style
- ❓ **Nutzt es Reflection?** → ⚠️ **Performance Red Flag**
  - `GetType()`, `GetProperties()`, `GetMethod()` sind oft vermeidbar
- ❓ **Warum wurde es custom gebaut?**
  - Legacy-Gründe? → Modernisieren
  - Platform-Feature fehlt? → Dokumentieren

#### **Complexity-Check:**
- ❓ **Mehr als 200 Zeilen?** → Wahrscheinlich zu komplex
- ❓ **Mehr als 3 verschachtelte If/Loops?** → Refactoring prüfen
- ❓ **Design-Time-Support?** (IntelliSense, Live Preview)
  - Nein → Wahrscheinlich nicht XAML-nativ

#### **MVVM-Check:**
- ❓ **Logik in Code-Behind statt ViewModel?** → MVVM-Violation
- ❓ **Direct UI-Manipulation?** (`textBox.Text = value`) → Binding bevorzugen

---

### **Schritt 2: Dependencies & Coupling analysieren**

#### **Dependency-Graph erstellen:**
```
CustomControl A
  ├─ verwendet Helper B
  ├─ ruft Manager C auf
  └─ benötigt Service D
```

**Fragen:**
- ❓ **Circular Dependencies?** (A → B → A)
- ❓ **God Objects?** (eine Klasse mit >10 Dependencies)
- ❓ **Tight Coupling?** (direkte Referenzen statt Interfaces)

#### **Cleanup-Logik identifizieren:**
Beispiel: `ClearOtherSelections` war ein **Symptom**, dass das Design falsch war!

- ❓ **Gibt es viele "Clear*" / "Reset*" Methoden?**
  - → Oft Zeichen für fehlendes reaktives Design
  - → Binding + Property-Notifications sollten reichen

---

### **Schritt 3: Performance Hotspots**

#### **Reflection-Suche:**
```csharp
// ⚠️ RED FLAGS:
GetType()
GetProperties()
GetMethod()
Activator.CreateInstance()
```

#### **Ineffiziente Patterns:**
```csharp
// ❌ BAD: Reflection in Loop
foreach (var prop in obj.GetType().GetProperties())
{
    var value = prop.GetValue(obj); // Slow!
}

// ✅ GOOD: Compiled Binding
<TextBox Text="{x:Bind Name, Mode=TwoWay}"/>  // Fast!
```

#### **Memory Leaks:**
- ❓ **Event Subscriptions ohne Unsubscribe?**
- ❓ **IDisposable implementiert aber nicht aufgerufen?**
- ❓ **Static Collections die wachsen?**

---

### **Schritt 4: XAML Anti-Patterns**

#### **Code-Behind Overuse:**
```csharp
// ❌ ANTI-PATTERN: Logic in Code-Behind
private void Button_Click(object sender, RoutedEventArgs e)
{
    // Business Logic here
    CalculateSomething();
    UpdateUI();
}

// ✅ PATTERN: Command Binding
<Button Command="{x:Bind ViewModel.CalculateCommand}"/>
```

#### **Missing Data Templates:**
```xml
<!-- ❌ ANTI-PATTERN: Programmatic UI Generation -->
<!-- SimplePropertyGrid erstellt Controls in C# -->

<!-- ✅ PATTERN: Declarative Templates -->
<ContentControl ContentTemplateSelector="{StaticResource EntityTemplateSelector}"/>
```

#### **Binding vs x:Bind:**
```xml
<!-- ⚠️ OLD: Runtime Binding (slower) -->
<TextBox Text="{Binding Name, Mode=TwoWay}"/>

<!-- ✅ NEW: Compiled Binding (faster, type-safe) -->
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay}"/>
```

---

### **Schritt 5: Architecture Violations**

#### **Clean Architecture Layer-Check:**

| Layer | Erlaubt | Verboten |
|-------|---------|----------|
| **Domain** | POCOs, Value Objects | INotifyPropertyChanged, Attributes |
| **Backend** | Business Logic | DispatcherQueue, MainThread |
| **SharedUI** | ViewModels (MVVM) | Platform-specific Code |
| **WinUI/MAUI** | UI, Platform Code | Business Logic |

**Prüfen:**
- ❓ **Domain hat UI-Dependencies?** → Violation!
- ❓ **Backend nutzt DispatcherQueue?** → Sollte IUiDispatcher sein
- ❓ **ViewModel hat Platform-Code?** → Abstraktion fehlt

---

## 📋 **Spezifische Checklisten**

### **Custom Control Audit:**

Für **jede** Custom Control-Klasse:

```
□ Gibt es Platform-Äquivalent? (ContentControl, DataTemplateSelector, etc.)
□ Nutzt Reflection? (GetType, GetProperties)
□ >200 Zeilen Code?
□ Design-Time-Support? (IntelliSense)
□ Folgt Fluent Design 2? (Spacing, Typography, Theme)
□ Testbar? (Dependency Injection)
□ MVVM-konform? (Logic in ViewModel, not Code-Behind)
□ Performance gemessen? (Profiler)
```

**Action:** Wenn >3 ☒ → **Refactoring** candidate!

---

### **Helper/Manager Audit:**

Für **jede** Helper/Manager-Klasse:

```
□ Single Responsibility? (macht nur EINE Sache gut)
□ <50 Zeilen?
□ Keine verschachtelte If-Logik? (>3 Ebenen)
□ Platform-Binding könnte es ersetzen?
□ Unit-Tests vorhanden?
□ Interface statt konkrete Klasse? (Testability)
```

**Action:** Wenn >2 ☒ → **Simplify** oder **Split**!

---

### **XAML Page Audit:**

Für **jede** XAML-Seite:

```
□ Code-Behind <50 Zeilen? (nur Constructor + DI)
□ Commands statt Click-Handler?
□ x:Bind statt Binding? (Performance)
□ DataTemplates für Listen? (ListView, GridView)
□ Converters dokumentiert? (Wieso custom?)
□ Resources organisiert? (Nicht inline)
```

**Action:** Wenn >3 ☒ → **MVVM** refactoring!

---

## 🚀 **Automated Analysis Tools**

### **1. Static Code Analysis:**

**ReSharper / Rider Inspections:**
- "Possible multiple enumeration of IEnumerable"
- "Virtual member call in constructor"
- "Async method lacks 'await' operators"

**Roslyn Analyzers:**
```xml
<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" />
<PackageReference Include="StyleCop.Analyzers" />
```

### **2. Performance Profiling:**

**dotnet-trace:**
```bash
dotnet-trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime
```

**Look for:**
- ⚠️ High `System.Reflection.*` calls
- ⚠️ Frequent `GC` collections
- ⚠️ Long `UI Thread` blocks

---

## 🎯 **Prioritäten für Refactoring**

### **High Priority (sofort):**
1. 🔴 **Reflection in Loops** (Performance-Killer)
2. 🔴 **Memory Leaks** (Event Subscriptions)
3. 🔴 **Architecture Violations** (Domain mit UI-Code)

### **Medium Priority (nächste Sprint):**
4. 🟡 **Custom Controls >200 Zeilen**
5. 🟡 **Code-Behind >50 Zeilen**
6. 🟡 **Missing Platform-Features** (Custom statt Native)

### **Low Priority (Backlog):**
7. 🟢 **Style Improvements** (Fluent Design 2)
8. 🟢 **Test Coverage** (>80%)
9. 🟢 **Documentation** (XML Comments)

---

## 📝 **Analysis Report Template**

Nach jeder Analyse:

```markdown
# Code Analysis Report - [Date]

## Identified Issues

### High Priority
- [ ] Issue 1: [Description]
  - Location: [File:Line]
  - Impact: [Performance/Architecture/Maintainability]
  - Suggested Fix: [Description]

### Medium Priority
- [ ] Issue 2: ...

### Low Priority
- [ ] Issue 3: ...

## Refactoring Opportunities

1. **CustomControl → Platform Pattern**
   - Before: SimplePropertyGrid (350 lines, Reflection)
   - After: ContentControl + DataTemplateSelector (200 lines XAML)
   - Benefit: -70% code, +native performance

## Metrics

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Lines of Code | X | Y | -Z% |
| Cyclomatic Complexity | A | B | -C% |
| Test Coverage | D% | E% | +F% |
```

---

## 🔬 **Lessons Learned: PropertyGrid Case Study**

### **Warum wurde es übersehen?**

1. ❌ **"Es funktioniert"** = nicht genug
2. ❌ **Keine Platform-Patterns geprüft**
3. ❌ **Performance nicht gemessen**
4. ❌ **Custom Controls als "Normal" akzeptiert**

### **Wie vermeiden?**

1. ✅ **Jede Custom-Lösung hinterfragen:** "Warum nicht Platform?"
2. ✅ **Performance messen:** Profiler, nicht Bauchgefühl
3. ✅ **Best Practices kennen:** WinUI Gallery, Fluent Design Docs
4. ✅ **Code Reviews:** Mit Fokus auf "Is there a simpler way?"

---

## 📚 **Ressourcen**

- **WinUI 3 Gallery:** https://apps.microsoft.com/detail/9P3JFPWWDZRC (Live Beispiele!)
- **Fluent Design 2:** https://fluent2.microsoft.design/
- **Performance Guidelines:** https://learn.microsoft.com/en-us/windows/apps/performance/
- **MVVM Toolkit:** https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/introduction

---

**Last Updated:** 2025-12-08  
**Version:** 1.0  
**Author:** MOBAflow Architecture Team
