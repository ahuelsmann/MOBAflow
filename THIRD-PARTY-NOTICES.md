# Third-Party Notices

MOBAflow uses the following open-source components. We are grateful to the developers and contributors of these projects.

---

## NuGet Packages

### .NET Foundation & Microsoft

#### Microsoft.Extensions.* (MIT License)
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/dotnet/runtime
- **Used in:** WinUI, Backend, Sound, Test
- **Purpose:** Dependency Injection, Configuration, Logging

#### Microsoft.WindowsAppSDK (MIT License)
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/microsoft/WindowsAppSDK
- **Used in:** WinUI
- **Purpose:** WinUI 3 Framework

#### Microsoft.Maui.Controls (MIT License)
- **License:** MIT
- **Copyright:** © .NET Foundation
- **Repository:** https://github.com/dotnet/maui
- **Used in:** MAUI
- **Purpose:** Cross-platform UI framework

#### Microsoft.CognitiveServices.Speech (MIT License)
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/Azure-Samples/cognitive-services-speech-sdk
- **Used in:** Sound
- **Purpose:** Azure Text-to-Speech

---

### Community Toolkit

#### CommunityToolkit.Mvvm (MIT License)
- **License:** MIT
- **Copyright:** © .NET Foundation
- **Repository:** https://github.com/CommunityToolkit/dotnet
- **Used in:** SharedUI, WebApp
- **Purpose:** MVVM helpers (ObservableObject, RelayCommand)

#### CommunityToolkit.Maui (MIT License)
- **License:** MIT
- **Copyright:** © .NET Foundation
- **Repository:** https://github.com/CommunityToolkit/Maui
- **Used in:** MAUI
- **Purpose:** MAUI extensions and controls

---

### Logging & Serialization

#### Serilog (Apache-2.0 License)
- **License:** Apache-2.0
- **Copyright:** © Serilog Contributors
- **Repository:** https://github.com/serilog/serilog
- **Used in:** WinUI
- **Purpose:** Structured logging

#### Serilog.Sinks.File (Apache-2.0 License)
- **License:** Apache-2.0
- **Copyright:** © Serilog Contributors
- **Repository:** https://github.com/serilog/serilog-sinks-file
- **Used in:** WinUI
- **Purpose:** File logging

#### Serilog.Sinks.Debug (Apache-2.0 License)
- **License:** Apache-2.0
- **Copyright:** © Serilog Contributors
- **Repository:** https://github.com/serilog/serilog-sinks-debug
- **Used in:** WinUI
- **Purpose:** Debug output logging

---

### Testing

#### NUnit (MIT License)
- **License:** MIT
- **Copyright:** © Charlie Poole, Rob Prouse
- **Repository:** https://github.com/nunit/nunit
- **Used in:** Test
- **Purpose:** Unit testing framework

#### Moq (BSD-3-Clause License)
- **License:** BSD-3-Clause
- **Copyright:** © Daniel Cazzulino, kzu
- **Repository:** https://github.com/moq/moq4
- **Used in:** Test
- **Purpose:** Mocking framework

---

### UI Libraries

#### UraniumUI.Material (Apache-2.0 License)
- **License:** Apache-2.0
- **Copyright:** © Enis Necipoglu
- **Repository:** https://github.com/enisn/UraniumUI
- **Used in:** MAUI
- **Purpose:** Material Design components for MAUI

---

### Build & DevOps

#### Microsoft.SourceLink.AzureRepos.Git (MIT License)
- **License:** MIT
- **Copyright:** © .NET Foundation
- **Repository:** https://github.com/dotnet/sourcelink
- **Used in:** All projects
- **Purpose:** Source code linking for debugging

#### coverlet.collector (MIT License)
- **License:** MIT
- **Copyright:** © Coverlet Team
- **Repository:** https://github.com/coverlet-coverage/coverlet
- **Used in:** Test
- **Purpose:** Code coverage collection

---

## System Dependencies

### System.Speech (MIT License)
- **License:** MIT
- **Copyright:** © .NET Foundation
- **Used in:** Sound
- **Purpose:** Windows Text-to-Speech (fallback)

### System.Windows.Extensions (MIT License)
- **License:** MIT
- **Copyright:** © .NET Foundation
- **Used in:** Sound
- **Purpose:** Windows-specific APIs

---

## License Compliance

All dependencies are compatible with the **MIT License** of MOBAflow:
- ✅ **MIT License:** Most packages (fully compatible)
- ✅ **Apache-2.0:** Serilog, UraniumUI (permissive, compatible)
- ✅ **BSD-3-Clause:** Moq (permissive, compatible)

---

## Full Dependency Tree

For a complete list of all dependencies (including transitive packages), run:

```bash
dotnet list package --include-transitive
```

---

**Last Updated:** 2025-12-05  
**MOBAflow Version:** 1.0.0  
**License:** MIT License (see [LICENSE](LICENSE))
