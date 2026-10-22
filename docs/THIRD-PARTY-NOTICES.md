# Third-Party Notices

MOBAflow uses third-party software, frameworks, protocols, and media sources.
This document tracks the direct dependency surface currently referenced by the repository's project files.
Package versions are centrally managed in [`Directory.Packages.props`](../Directory.Packages.props), with build-wide references added in [`Directory.Build.props`](../Directory.Build.props) and [`Directory.Build.targets`](../Directory.Build.targets).

For a complete dependency tree including transitive packages, run:

```bash
dotnet list <project>.csproj package --include-transitive
```

## Scope

- Direct `PackageReference` and `FrameworkReference` entries currently used by project files
- Build-wide package references applied to all projects
- External software, protocols, formats, and media sources that MOBAflow interoperates with
- Transitive SDK/runtime dependencies are not listed individually

Some package versions may exist in `Directory.Packages.props` for future or conditional use without being referenced by a current `PackageReference`. Those entries are not treated here as active direct dependencies.

---

## Audio Resources

### Freesound.org (Community Sound Library)

- **Website:** [Freesound.org](https://freesound.org/)
- **Licenses:** CC BY 4.0 for the two gollamar announcement recordings; CC0 1.0 for the Prague Metro Line A chime
- **Usage:** Workflow announcement intros (Announcement-04.wav and Announcement-06.wav) and the Prague Metro Line A chime.
- **Attribution:** Individual sound attributions are listed in [`Sound/Resources/Sounds/ATTRIBUTION.md`](../Sound/Resources/Sounds/ATTRIBUTION.md)
- **Redistribution:** Keep the file-specific attribution and license links with the bundled recordings. The repository's MIT license does not relicense these audio files.

---

## External Software, Protocols, and Formats

### AnyRail

- **Developer:** Carsten Kuhling, Paco Ahlqvist
- **Website:** [anyrail.com](https://www.anyrail.com)
- **License:** Proprietary
- **Usage:** Import of track plan files saved in AnyRail's XML format
- **Legal Notice:** MOBAflow does not include, distribute, or modify AnyRail. MOBAflow is an independent project and is not affiliated with, endorsed by, or sponsored by AnyRail.
- **Basis:** File-format interoperability

### Piko A-Gleis

- **Manufacturer:** Piko GmbH
- **Website:** [piko.de](https://www.piko.de)
- **License / Rights Context:** Product and article identifiers remain the property of their respective owner
- **Usage:** Track article codes and geometry data for the `TrackLibrary.PikoA` library
- **Basis:** Interoperability with commercially available model railroad components

### Roco Z21

- **Manufacturer:** Roco / Fleischmann
- **Website:** [Z21](https://www.z21.eu/en)
- **License:** Proprietary hardware platform
- **Usage:** Direct UDP communication with the Z21 digital command station
- **Legal Notice:** MOBAflow is an independent control application and is not affiliated with Roco or Fleischmann.
- **Basis:** Publicly documented or reverse-engineered interoperability at the protocol level

---

## Development Skills and Templates

### GitHub Spec Kit

- **Source:** [GitHub Spec Kit v1.1.3](https://github.com/github/spec-kit/tree/v1.1.3)
- **License:** MIT; the complete original GitHub, Inc. notice is retained in [`.specify/LICENSE`](../.specify/LICENSE).
- **Scope:** Imported scripts and templates under `.specify/` and the ten `.agents/skills/speckit-*/` skills. Local adaptations retain that notice.
- **Traceability:** The [source registry](../.agents/skills/sources.json) and integration manifests under `.specify/integrations/` record the imported version and adaptations.

### Adapted Matt Pocock Skills

- **Source:** [Matt Pocock skills at the recorded revision](https://github.com/mattpocock/skills/tree/c55ee46073ed923f86ce59a5eb3b6d895095d1b7)
- **License:** MIT; original notices are retained with [code review](../.agents/skills/mobaflow-code-review/LICENSE) and [bug diagnosis](../.agents/skills/mobaflow-diagnosing-bugs/LICENSE), including their `.claude/skills/` mirrors.
- **Scope:** These adapted workflows and the Matt Pocock material in the inactive `skills/` reference snapshot. The repository's own license does not replace upstream copyright notices.

---

## Direct Package Inventory

The following table is generated from the centrally managed versions in [`Directory.Packages.props`](../Directory.Packages.props) and the `PackageReference` entries in tracked project files and build-wide props/targets.
Conditional references and all three mutation test hosts are included; this is a source inventory,
not a resolved per-platform restore graph. Each package has its own row so its version and
usage can be compared directly with those XML entries. Versions in this section must not be edited independently of the central package catalog.

<!-- documentation-facts:packages:start -->
| Package | Version | Directly referenced in | Purpose |
| --- | --- | --- | --- |
| `CommunityToolkit.Maui` | `15.0.1` | `MOBAsmart` | MAUI helpers, converters and behaviors. |
| `CommunityToolkit.Mvvm` | `8.4.2` | `SharedUI` | Observable properties and relay commands. |
| `CommunityToolkit.WinUI.Controls.ColorPicker` | `8.2.251219` | `MOBAflow` | WinUI color picker. |
| `CommunityToolkit.WinUI.Controls.Sizers` | `8.2.251219` | `MOBAflow` | WinUI grid splitters. |
| `coverlet.collector` | `10.1.0` | `Test` | Test coverage collection. |
| `Microsoft.AspNetCore.SignalR.Client` | `10.0.13` | `MOBAflow`, `MOBAsmart`, `Test` | SignalR client connectivity. |
| `Microsoft.Azure.AppConfiguration.AspNetCore` | `8.6.0` | `MOBAflow` | Optional Azure App Configuration source. |
| `Microsoft.CodeAnalysis.CSharp` | `5.9.0` | `Test` | C# parsing for architecture tests. |
| `Microsoft.Extensions.DependencyInjection` | `10.0.13` | `MutationTest`, `MutationTest/Common`, `Test` | Dependency injection container. |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.13` | `Backend` | Dependency injection contracts. |
| `Microsoft.Extensions.Http` | `10.0.13` | `MOBAsmart` | HTTP client registration. |
| `Microsoft.Extensions.Logging` | `10.0.13` | `MutationTest/Common`, `MutationTest/Speech.Base`, `Test` | Logging services. |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.13` | `Backend`, `Common`, `SharedUI`, `Sound`, `Speech.Base`, `Speech.Piper` | Logging contracts. |
| `Microsoft.Extensions.Logging.Console` | `10.0.13` | `Test` | Console logging output. |
| `Microsoft.Extensions.Options` | `10.0.13` | `Sound` | Typed configuration options. |
| `Microsoft.Graphics.Win2D` | `1.4.0` | `MOBAflow` | Windows GPU-accelerated 2D rendering. |
| `Microsoft.Maui.Controls` | `10.0.120` | `MOBAsmart` | Android MAUI UI framework. |
| `Microsoft.NET.Test.Sdk` | `18.11.0` | `MutationTest`, `MutationTest/Common`, `MutationTest/Speech.Base`, `Test` | Test execution host. |
| `Microsoft.Windows.SDK.BuildTools` | `10.0.28000.2705` | `MOBAflow` | Windows SDK build tools under Microsoft's SDK license; development only. |
| `Microsoft.Windows.SDK.BuildTools.MSIX` | `1.7.260930100` | `MOBAflow` | Windows packaging build tools under Microsoft's SDK license; development only. |
| `Microsoft.WindowsAppSDK` | `2.5.1` | `MOBAflow` | Windows desktop UI and runtime. |
| `Microsoft.Xaml.Behaviors.WinUI.Managed` | `3.0.1` | `MOBAflow` | WinUI XAML behaviors. |
| `MinVer` | `8.0.0` | Build-wide via `Directory.Build.props` | Build-time versions from Git tags. |
| `Moq` | `4.21.0` | `MutationTest/Common`, `MutationTest/Speech.Base`, `Test` | Test doubles. |
| `NUnit` | `5.0.0` | `MutationTest`, `MutationTest/Common`, `MutationTest/Speech.Base`, `Test` | Test framework. |
| `NUnit.Analyzers` | `4.15.0` | `Test` | NUnit analyzer rules. |
| `NUnit3TestAdapter` | `6.3.1` | `MutationTest`, `MutationTest/Common`, `MutationTest/Speech.Base`, `Test` | NUnit test discovery and execution. |
| `Serilog` | `4.4.0` | `Common` | Structured logging. |
| `Serilog.Enrichers.Environment` | `3.0.1` | `MOBAflow` | Environment log context. |
| `Serilog.Enrichers.Process` | `3.0.0` | `MOBAflow` | Process log context. |
| `Serilog.Enrichers.Thread` | `4.0.0` | `MOBAflow` | Thread log context. |
| `Serilog.Extensions.Logging` | `10.0.0` | `MOBAflow` | Microsoft logging integration. |
| `Serilog.Sinks.Async` | `2.1.0` | `MOBAflow` | Asynchronous logging. |
| `Serilog.Sinks.Debug` | `3.0.0` | `MOBAflow` | Debug logging output. |
| `Serilog.Sinks.File` | `7.0.0` | `MOBAflow` | File logging output. |
| `SkiaSharp` | `4.153.1` | `MOBAdisplay`, `MOBAflow` | Display frame and image rendering. |
| `System.Drawing.Common` | `10.0.13` | `MOBAdisplay` | Image conversion utilities. |
| `System.Formats.Nrbf` | `10.0.13` | `MOBAsmart` | Android AOT dependency of the existing sound graph. |
| `System.Speech` | `10.0.13` | `Speech.SystemSpeech` | Windows text-to-speech. |
| `System.Windows.Extensions` | `10.0.13` | `MOBAflow` | Windows audio APIs. |
| `Xamarin.AndroidX.Startup.StartupRuntime` | `1.2.0.8` | `MOBAsmart` | Android startup integration. |
<!-- documentation-facts:packages:end -->

---

## Native and Platform Components

Native assets are tracked separately because their redistribution and platform behavior cannot be inferred from managed assembly references alone.

| Component | Version source | License | Distribution context |
| --- | --- | --- | --- |
| SkiaSharp native binaries | `SkiaSharp 4.153.1` in `Directory.Packages.props` | MIT | Native graphics binaries used by MOBAdisplay frame and image rendering. |
| Windows App SDK runtime | `Microsoft.WindowsAppSDK 2.5.1` in `Directory.Packages.props` | MIT | Windows desktop runtime and deployment components. |
| Win2D native components | `Microsoft.Graphics.Win2D 1.4.0` in `Directory.Packages.props` | MIT | GPU-accelerated Windows 2D rendering. |
| AndroidX Startup runtime | `Xamarin.AndroidX.Startup.StartupRuntime 1.2.0.8` in `Directory.Packages.props` | Apache-2.0 | Android initialization runtime distributed with MOBAsmart. |
| .NET / MAUI platform runtimes | Selected by the pinned .NET SDK and installed workloads | MIT and platform-specific notices | Framework/runtime files are supplied by Microsoft workloads and must be reviewed again when the SDK or workload changes. |

The authoritative SkiaSharp license is the upstream [MIT license](https://github.com/mono/SkiaSharp/blob/main/LICENSE.md).

---

## License Overview

The currently referenced direct dependencies primarily fall under the following license families:

| License family | Examples |
| --- | --- |
| MIT | Most Microsoft/.NET packages, CommunityToolkit packages, NUnit, coverlet, MinVer, SourceLink, SkiaSharp and its native binaries |
| Apache-2.0 | AndroidX startup runtime |
| BSD-3-Clause | Moq |

The authoritative license for a package is the license metadata published by its upstream project or NuGet package page.
The Windows SDK build tools use Microsoft's SDK license rather than the repository's MIT license:
see [Windows SDK BuildTools license](https://aka.ms/WinSDKLicenseURL) and the
[`sdk_license.txt` supplied with BuildTools.MSIX](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools.MSIX/1.7.260930100/License).
Both were already transitive Windows App SDK build dependencies; their explicit references pin the build-tool versions.
Before shipping a release with newly added dependencies, verify the package's current license and attribution requirements.

---

## License Compatibility

Based on the currently referenced direct dependencies, the repository mainly uses permissive open-source licenses that are generally compatible with MOBAflow's MIT license.
Any new dependency with copyleft, commercial, or field-of-use restrictions should be reviewed before distribution.

---

## Updating This Document

When adding or removing a direct dependency:

1. Update the package inventory in `Directory.Packages.props` or the relevant project file.
2. Run `python scripts/documentation_consistency.py --write` to regenerate the version and usage columns;
   Quality fails while they are stale. Review the purpose of every new package, which the generator flags.
3. Re-check whether the package introduces new attribution or redistribution requirements.
4. Keep `README.md` references in sync when user-facing build or dependency information changes.

---

## Acknowledgments

We are grateful to the open-source maintainers and standards authors whose work makes MOBAflow possible.

---

**Last Updated:** 2026-10-21
**Scope:** Current direct dependencies and external interoperability surface  
**License:** MIT License (see [LICENSE](../LICENSE))
