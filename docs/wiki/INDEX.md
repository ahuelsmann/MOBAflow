# MOBAflow Platform Wiki

**Welcome to the MOBAflow Platform Wiki!** 🚂

Here you'll find all information about three platforms:

---

## 📚 Platform Overview

- **🖥️ MOBAflow**
  - **Technology:** WinUI 3 (Windows Desktop)
  - **Target users:** Power Users
  - **Main features:** Journey Management, Workflow Automation,
    Track Plan Editor, Train Control, Signal Box, Display Page
- **📱 MOBAsmart**
  - **Technology:** .NET MAUI (Android)
  - **Target users:** Mobile Users
  - **Main features:** Lap Counter, Z21 Monitoring, Feedback
    Statistics
- **🌐 MOBApi**
  - **Technology:** ASP.NET Core REST + SignalR
  - **Target users:** All Platforms
  - **Main features:** REST API, photo upload, real-time hub, client discovery

---

## 🗂️ Documentation Index

> **📖 User Documentation** - For everyone who wants to use MOBAflow
>
> **👨‍💻 Developer Documentation** - At the end of this page, for
> plugin developers and contributors

### 🖥️ MOBAflow (Windows Desktop)

**User Guide:**

- [`MOBAFLOW-USER-GUIDE.md`](MOBAFLOW-USER-GUIDE.md) - Complete Guide

**Main Topics:**

- 🚂 Journey Management (Train journeys with stations)
- ⚡ Workflow Automation (Event-driven actions)
- 🎨 Track Plan Editor (Track visualization)
- 🚦 Signal Box and Viessmann multiplex signal testing
- 🖥️ ESP32-S3 display rendering and UDP frame transport
- 🎛️ Train Control with F0-F31 functions
- 🎙️ Text-to-Speech (Piper TTS)
- 🗂️ Solution Management (Project management)

**Setup Guides:**

- [`PIPER-TTS-SETUP.md`](PIPER-TTS-SETUP.md) - local Piper TTS
  setup

---

### 📱 MOBAsmart (Android)

**User Guide:**

- [`MOBASMART-USER-GUIDE.md`](MOBASMART-USER-GUIDE.md) - Complete Guide
- [`MOBASMART-WIKI.md`](MOBASMART-WIKI.md) - Extended Documentation

**Main Topics:**

- 📊 Lap Counter (Lap counter with timer filter)
- 🔌 Z21 Connection (UDP communication)
- 📱 Display Management (Keep app in foreground)
- 🔋 Battery Optimization
- 🛠️ Troubleshooting

---

### 🌐 MOBApi (REST API)

**Main Topics:**

- 🔗 REST API for MOBAflow Overview status
- 📡 SignalR Hub for real-time updates
- 📋 Client list & health endpoints
- 🔍 UDP multicast discovery (MOBAsmart connects automatically)

> **Start MOBApi:** Run `dotnet run --project MOBApi` (Port 5001)
> or enable "Auto-start REST API" in MOBAflow Settings.

---

## 🚀 Quick Start Guide

### Which Platform is Right for Me?

**Not installed yet?** 👉 Read [`📥 Installation Guide`](INSTALLATION.md)!

**Status:** ℹ️ Currently **manual installation from source code is
required**. Automated setup scripts are planned for future versions.

#### 🖥️ **Choose MOBAflow if you...**

- ✅ ...use a **Windows PC**
- ✅ ...want the most comprehensive features
- ✅ ...manage complex model railroad layouts
- ✅ ...prefer desktop applications

**Link:** [`MOBAFLOW-USER-GUIDE.md`](MOBAFLOW-USER-GUIDE.md)

#### 📱 **Choose MOBAsmart, if you...**

- ✅ ...use an **Android device**
- ✅ ...want to access the system **on the go**
- ✅ ...need a **Lap Counter**
- ✅ ...prefer a **simple, mobile solution**
- ✅ ...want to work **without a PC**

**Link:** [`MOBASMART-USER-GUIDE.md`](MOBASMART-USER-GUIDE.md)

#### 🌐 **Run MOBApi if you...**

- ✅ ...want MOBAsmart to discover MOBAflow automatically
- ✅ ...need a REST API for external integrations
- ✅ ...want real-time status via SignalR

> MOBApi can be started standalone or automatically by MOBAflow via
> the "Auto-start REST API" setting.

---

## 🎓 For Developers & Contributors

### Contributing to MOBAflow

- **Einstieg & Übersicht:** Siehe [`README.md`](../../README.md)
  (einzige Markdown-Datei im Repository-Root)
- **Architektur:** Siehe [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
  (Überblick & Schichten)

### Technische Dokumentation

- **Architektur:** [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
- **Projekt-Referenz:** [`../PROJECT-REFERENCE.md`](../PROJECT-REFERENCE.md)
  – aktuelle Repo-Struktur, Modelle, Services, Pages, Display-Pipeline,
  Workflows, Build/Deploy und bekannte Dokumentationslücken
- **JSON-Validierung:** [`../JSON-VALIDATION.md`](../JSON-VALIDATION.md)
  – Solution-Dateiformat & Validierung

---

## 📞 Support & Community

### Getting Help

**Problem with MOBAflow?**

- 🐛 **Bug:** Report on [GitHub Issues](https://github.com/ahuelsmann/MOBAflow/issues)
- 💬 **Questions:** Ask in [GitHub Discussions](https://github.com/ahuelsmann/MOBAflow/discussions)
- 📖 **Documentation:** Search this Wiki first

### Hardware Issues

**Problem with Z21 hardware?**

- 🛠️ **Z21 Not Responding:** See [INSTALLATION.md](INSTALLATION.md) → Troubleshooting
- 📞 **Roco Support:** [Roco Customer Service](https://www.roco.cc/en/customer-service)
- ⚠️ **Safety:** See [HARDWARE-DISCLAIMER.md](../HARDWARE-DISCLAIMER.md)

### Legal & Safety

- **Hardware Disclaimer:** [`HARDWARE-DISCLAIMER.md`](../HARDWARE-DISCLAIMER.md)
- **License:** [`LICENSE`](../../LICENSE) (MIT)
- **Third-Party Notices:** [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md)

---

## 🔄 Version Information

**Current Version:** 0.1.0 (Preview)

**What's Next:**

- v0.2.0: Automated setup scripts
- v0.3.0: Commercial plugin marketplace
- v1.0.0: Feature-complete, production-ready

Siehe [`../CHANGELOG.md`](../CHANGELOG.md) für Änderungen.

---

## 📚 Complete Documentation Map

```text
MOBAflow Repository
├─ README.md (START HERE – einzige MD-Datei im Root)
├─ LICENSE (MIT)
├─ docs/
│  ├─ ARCHITECTURE.md, PROJECT-REFERENCE.md, CHANGELOG.md, CLAUDE.md
│  ├─ HARDWARE-DISCLAIMER.md, JSON-VALIDATION.md, MINVER-SETUP.md
│  ├─ SECURITY.md, THIRD-PARTY-NOTICES.md
│  └─ wiki/ (This Wiki)
│     ├─ INDEX.md (You are here)
│     ├─ INSTALLATION.md, PIPER-TTS-SETUP.md
│     ├─ MOBAFLOW-USER-GUIDE.md, MOBASMART-USER-GUIDE.md
│     └─ MOBASMART-WIKI.md, MOBATPS.md, QUICK-START-TRACK-STATISTICS.md
└─ .github/ (Development, Instructions, Workflows)
```

---

## 🎯 Navigation Tips

**New to MOBAflow?**

1. Read `README.md` (repository root) ← Start here
2. Check `docs/wiki/INSTALLATION.md` ← Setup guide
3. Choose your platform (above) ← Pick one
4. Read platform guide ← Learn features

**Developer?**

1. Read `docs/ARCHITECTURE.md` ← Technical overview
2. See `README.md` ← Contributing & overview

**Having Issues?**

1. Check this Wiki first (search box) ← Already solved?
2. Read `INSTALLATION.md` → Troubleshooting
3. Report on GitHub Issues ← Still not working?

---

*Last Updated: May 2026*
*Questions? Open an issue or discussion on GitHub!* 💬
