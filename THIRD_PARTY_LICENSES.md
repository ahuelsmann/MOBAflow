# Third-Party Licenses

MOBAflow uses the following open-source packages and libraries. We are grateful to their maintainers and contributors.

---

## .NET Libraries

### Microsoft.Extensions.Logging.Abstractions
- **Version**: 10.0.0
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/runtime
- **Purpose**: Logging abstractions for dependency injection

### Newtonsoft.Json
- **Version**: 13.0.4
- **License**: MIT
- **Copyright**: © James Newton-King
- **URL**: https://github.com/JamesNK/Newtonsoft.Json
- **Purpose**: JSON serialization and deserialization

---

## MVVM & UI Frameworks

### CommunityToolkit.Mvvm
- **Version**: 8.x
- **License**: MIT
- **Copyright**: © .NET Foundation
- **URL**: https://github.com/CommunityToolkit/dotnet
- **Purpose**: MVVM helpers (ObservableObject, RelayCommand, etc.)

### UraniumUI
- **Version**: 2.x
- **License**: Apache 2.0
- **Copyright**: © Enis Necipoglu
- **URL**: https://github.com/enisn/UraniumUI
- **Purpose**: Material Design components for .NET MAUI

### CommunityToolkit.Maui
- **Version**: Latest
- **License**: MIT
- **Copyright**: © .NET Foundation
- **URL**: https://github.com/CommunityToolkit/Maui
- **Purpose**: MAUI community toolkit

---

## Testing Frameworks

### NUnit
- **Version**: 4.x
- **License**: MIT
- **Copyright**: © NUnit Software
- **URL**: https://nunit.org/
- **Purpose**: Unit testing framework

### Moq
- **Version**: 4.x
- **License**: BSD-3-Clause
- **Copyright**: © Daniel Cazzulino and Contributors
- **URL**: https://github.com/moq/moq4
- **Purpose**: Mocking framework for tests

---

## Azure SDKs

### Microsoft.CognitiveServices.Speech
- **Version**: Latest
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/Azure-Samples/cognitive-services-speech-sdk
- **Purpose**: Azure Cognitive Services Text-to-Speech

---

## Development Tools

### SonarAnalyzer.CSharp
- **Version**: 10.15.0
- **License**: LGPL-3.0
- **Copyright**: © SonarSource SA
- **URL**: https://github.com/SonarSource/sonar-dotnet
- **Purpose**: Static code analysis

### Microsoft.SourceLink.AzureRepos.Git
- **Version**: 8.0.0
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/sourcelink
- **Purpose**: Source link for debugging

---

## Platform Frameworks

### .NET 10 (LTS)
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/runtime

### WinUI 3 (Windows App SDK)
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/microsoft/WindowsAppSDK

### .NET MAUI
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/maui

### Blazor Server
- **License**: MIT
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/aspnetcore

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

UraniumUI is licensed under Apache 2.0. Full license text: https://www.apache.org/licenses/LICENSE-2.0

### BSD-3-Clause License

Moq is licensed under BSD-3-Clause. Full license text: https://opensource.org/licenses/BSD-3-Clause

### LGPL-3.0 License

SonarAnalyzer.CSharp is licensed under LGPL-3.0. Full license text: https://www.gnu.org/licenses/lgpl-3.0.html

---

## Updating This Document

When adding new NuGet packages, please update this file with:
1. Package name and version
2. License type
3. Copyright holder
4. URL to project repository
5. Brief purpose description

---

**Note**: This list may not be exhaustive. For a complete list of dependencies, run:

```bash
dotnet list package --include-transitive
```

---

## Acknowledgments

We are grateful to all open-source maintainers whose work makes MOBAflow possible.

Thank you! 🙏
