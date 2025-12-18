# AnyRail Integration - Legal & Compliance Documentation

**Status:** ✅ Legally Compliant  
**Last Updated:** December 2025  
**Version:** 1.0

---

## Overview

MOBAflow integrates with **AnyRail** format for track plan imports. This document explains the legal basis, compliance measures, and best practices.

---

## What is AnyRail?

**AnyRail** is a proprietary model railroad track planning software developed by Carsten Kühling and Paco Ahlqvist.

- **Website:** https://www.anyrail.com
- **License:** Proprietary (commercial software)
- **Usage:** Design and optimize model railroad layouts
- **Export Formats:** XML, images, printing

---

## MOBAflow's AnyRail Integration

### What We Do ✅

1. **Import XML Files**
   - Users export their track plans from AnyRail as XML files
   - MOBAflow reads these XML files
   - Parses geometry (lines, arcs, endpoints)
   - Generates SVG rendering in MOBAflow UI

2. **Detect Track Types**
   - Extract article codes (R1, R2, G231, WL, WR, DWW, DKW)
   - Map to Piko A-Gleis standard nomenclature
   - Classify as Straight, Curve, Turnout, etc.

3. **Provide User Value**
   - Integrates with model railroad automation (Z21 feedback)
   - Links feedback sensors to track segments
   - Enables event-driven workflows

### What We DON'T Do ❌

- ❌ Distribute AnyRail software
- ❌ Modify AnyRail code or files
- ❌ Replicate AnyRail functionality
- ❌ Create an AnyRail alternative/competitor
- ❌ Bypass AnyRail's licensing

---

## Legal Basis

### Fair Use / Interoperability

The integration is based on **interoperability law** and **fair use** principles:

1. **File Format Parsing**
   - Users have the right to export their own files from AnyRail
   - Parsing the exported XML format is standard interoperability
   - Similar to opening `.xlsx` files or `.pdf` documents

2. **Public Standards**
   - Piko A-Gleis article codes are **public domain** (standard nomenclature)
   - XML format is a **public standard** (not proprietary)
   - SVG rendering is a **standard graphics format**

3. **No Commercial Harm**
   - MOBAflow doesn't replace AnyRail
   - MOBAflow actually increases AnyRail's value (users can do more with their layouts)
   - We're a complementary tool, not a competitor

### Relevant Laws

- **EU Copyright Directive (2001/29/EC):** Article 6 - Interoperability exception
- **US Copyright Law (17 USC):** Section 1201(f) - Reverse engineering for interoperability
- **German Copyright Law (UrhG):** § 69d - Interoperability rights
- **Fair Use (17 USC § 107):** Educational, transformative use

---

## Compliance Measures

### 1. Documentation ✅

All references to AnyRail are properly documented:

- **README.md** - Legal notice linking to THIRD-PARTY-NOTICES.md
- **THIRD-PARTY-NOTICES.md** - Comprehensive disclaimer
- **.github/copilot-instructions.md** - Developer guidelines
- **Source code comments** - Explaining the integration

### 2. Attribution ✅

- AnyRail is credited as proprietary software
- Developer names (Carsten Kühling, Paco Ahlqvist) are mentioned
- Website is linked for users to learn more

### 3. No Code Reuse ✅

- We don't use any AnyRail code
- We don't reverse-engineer AnyRail algorithms
- We parse the **public XML format** only

### 4. Clear Licensing ✅

- MOBAflow is MIT Licensed (permissive, open-source)
- THIRD-PARTY-NOTICES.md clearly distinguishes:
  - Open-source dependencies (MIT, Apache 2.0, etc.)
  - Proprietary third-party software (AnyRail, Roco, Piko)

### 5. User Workflow ✅

The integration is designed for legitimate user workflows:

```
1. User owns AnyRail (purchased/licensed)
2. User creates track plan in AnyRail
3. User exports as XML from AnyRail
4. User imports XML into MOBAflow
5. User links feedback sensors in MOBAflow
6. User automates their model railroad
```

This is **not** circumventing any DRM or license terms.

---

## Best Practices for Developers

### When Mentioning AnyRail

✅ **CORRECT:**
```
"MOBAflow supports importing track plans from AnyRail XML format."
"AnyRail is a third-party track planning tool by Carsten Kühling and Paco Ahlqvist."
"Users can export their track layouts from AnyRail and import them into MOBAflow."
```

❌ **INCORRECT:**
```
"MOBAflow is a free alternative to AnyRail."
"We replicated AnyRail's functionality in MOBAflow."
"Download AnyRail layouts and use them in MOBAflow without AnyRail."
```

### Documentation Checklist

- [ ] Every mention of "AnyRail" includes context (what we support, not full replacement)
- [ ] User workflows clearly show: AnyRail export → MOBAflow import
- [ ] THIRD-PARTY-NOTICES.md is linked from README.md
- [ ] Legal notice is visible to users
- [ ] Source code has comments explaining the integration

### Code Examples

```csharp
/// <summary>
/// Parses an AnyRail XML file and imports the track layout.
/// User must export the layout from AnyRail first.
/// See THIRD-PARTY-NOTICES.md for legal disclaimers.
/// </summary>
public void LoadAnyRailLayout(string xmlPath)
{
    var layout = AnyRailLayout.Parse(xmlPath);
    // ... process layout ...
}
```

---

## What If AnyRail Objects?

**Unlikely Scenario:** If AnyRail's developers object to MOBAflow:

1. **First Step:** We would contact them directly and explain:
   - We're supporting user workflows (fair use)
   - We're not distributing their software
   - We're not competing with their product
   - We provide value to AnyRail users

2. **Potential Solutions:**
   - Official partnership (AnyRail endorses MOBAflow)
   - Licensing agreement
   - Format specification sharing

3. **Worst Case:** We would:
   - Remove AnyRail support if required
   - Pivot to alternative formats (IronRail, Scarm, custom XML)
   - Document the change transparently

---

## Conclusion

MOBAflow's AnyRail integration is:

✅ **Legally compliant** - Based on fair use and interoperability rights  
✅ **Ethically sound** - Increases value for AnyRail users  
✅ **Properly documented** - Clear disclaimers and attribution  
✅ **User-focused** - Enables legitimate workflows  

We are grateful to the AnyRail team for creating excellent software that our users love!

---

## References

- **AnyRail Website:** https://www.anyrail.com
- **MOBAflow THIRD-PARTY-NOTICES.md:** [Link to file](../THIRD-PARTY-NOTICES.md)
- **MOBAflow License:** [MIT License](../LICENSE)
- **MOBAflow Repository:** https://dev.azure.com/ahuelsmann/MOBAflow

---

**For Legal Questions:**  
Contact: Andreas Hülsmann (ahuelsmann on GitHub / Azure DevOps)

