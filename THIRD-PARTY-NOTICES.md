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
- **Purpose:** Azure Cognitive Services Text-to-Speech

#### Newtonsoft.Json (MIT License)
- **License:** MIT
- **Copyright:** © James Newton-King
- **Repository:** https://github.com/JamesNK/Newtonsoft.Json
- **Used in:** Domain, Backend
- **Purpose:** JSON serialization and deserialization

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

### Logging

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

### UI Libraries

#### UraniumUI.Material (Apache-2.0 License)
- **License:** Apache-2.0
- **Copyright:** © Enis Necipoglu
- **Repository:** https://github.com/enisn/UraniumUI
- **Used in:** MAUI
- **Purpose:** Material Design components for MAUI

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

### Build & DevOps

#### SonarAnalyzer.CSharp (LGPL-3.0 License)
- **License:** LGPL-3.0
- **Copyright:** © SonarSource SA
- **Repository:** https://github.com/SonarSource/sonar-dotnet
- **Used in:** All projects
- **Purpose:** Static code analysis

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

## Platform Frameworks

### .NET 10 (LTS)
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/dotnet/runtime

### WinUI 3 (Windows App SDK)
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/microsoft/WindowsAppSDK

### .NET MAUI
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/dotnet/maui

### Blazor Server
- **License:** MIT
- **Copyright:** © Microsoft Corporation
- **Repository:** https://github.com/dotnet/aspnetcore

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
- ✅ **LGPL-3.0:** SonarAnalyzer (dev-only, not distributed)

---

## License Texts

### MIT License

```
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Apache License 2.0

Serilog and UraniumUI are licensed under Apache 2.0. Full license text: https://www.apache.org/licenses/LICENSE-2.0

### BSD-3-Clause License

Moq is licensed under BSD-3-Clause. Full license text: https://opensource.org/licenses/BSD-3-Clause

### LGPL-3.0 License

SonarAnalyzer.CSharp is licensed under LGPL-3.0. Full license text: https://www.gnu.org/licenses/lgpl-3.0.html

---

## Full Dependency Tree

For a complete list of all dependencies (including transitive packages), run:

```bash
dotnet list package --include-transitive
```

---

## Updating This Document

When adding new NuGet packages, please update this file with:
1. Package name and version
2. License type
3. Copyright holder
4. Repository URL
5. Which project uses it
6. Brief purpose description

---

## Acknowledgments

We are grateful to all open-source maintainers whose work makes MOBAflow possible.

Thank you! 🙏

---

**Last Updated:** 2025-12-11  
**MOBAflow Version:** 1.0.0  
**License:** MIT License (see [LICENSE](LICENSE))
