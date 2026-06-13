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
- **License:** Various Creative Commons licenses, including CC0, CC-BY 3.0, and CC-BY 4.0
- **Usage:** Workflow sound effects such as station bells, train whistles, and warning signals
- **Attribution:** Individual sound attributions are listed in [`Sound/Resources/Sounds/ATTRIBUTION.md`](../Sound/Resources/Sounds/ATTRIBUTION.md)
- **Compliance:** MOBAflow does not distribute the Freesound platform itself. Sound attribution is tracked per file where required by the original license.

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
- **Website:** [roco.cc](https://www.roco.cc)
- **License:** Proprietary hardware platform
- **Usage:** Direct UDP communication with the Z21 digital command station
- **Legal Notice:** MOBAflow is an independent control application and is not affiliated with Roco or Fleischmann.
- **Basis:** Publicly documented or reverse-engineered interoperability at the protocol level

---

## Direct Package Inventory

The following table reflects the direct package and framework references currently present in the repository's active project files.

| Package or family | Version(s) | Used in | Purpose |
| --- | --- | --- | --- |
| `CommunityToolkit.Labs.WinUI.Controls.DataTable`, `CommunityToolkit.Labs.WinUI.Controls.Ribbon` | `0.1.251217-build.2433` | `MOBAflow` | Experimental WinUI data table and ribbon controls. |
| `CommunityToolkit.WinUI.*`, `CommunityToolkit.WinUI.UI.Controls.Markdown` | `8.2.251219`, `7.1.2` | `MOBAflow` | WinUI helpers, behaviors, animations, controls, and Markdown rendering. |
| `CommunityToolkit.Mvvm` | `8.4.2` | `Domain`, `SharedUI` | MVVM primitives such as observable properties and relay commands. |
| `CommunityToolkit.Maui`, `CommunityToolkit.Maui.MediaElement` | `14.1.0`, `9.0.0` | `MAUI.Controls`, `MOBAsmart` | MAUI helpers, converters, behaviors, and media playback support. |
| `Microsoft.WindowsAppSDK`, `Microsoft.Graphics.Win2D`, `Microsoft.Xaml.Behaviors.WinUI.Managed` | `2.0.1`, `1.4.0`, `3.0.1` | `MOBAflow` | Windows desktop shell, GPU-accelerated 2D rendering, and XAML behaviors. |
| `Microsoft.Azure.AppConfiguration.AspNetCore` | `8.5.0` | `MOBAflow` | Centralized runtime configuration from Azure App Configuration. |
| `Microsoft.AspNetCore.App`, `Microsoft.AspNetCore.SignalR.Client` | `Framework reference`, `10.0.7` | `MOBAflow`, `MOBApi`, `SharedUI.Web` | ASP.NET Core shared framework and SignalR client connectivity. |
| `Microsoft.Extensions.*` | `10.0.5` to `10.0.7` | `Common`, `Backend`, `SharedUI.Web`, `Sound`, `MOBAsmart`, `Test` | Logging, dependency injection, configuration, options, hosting, and debug output. |
| `Microsoft.Maui.Controls` | `10.0.60` | `MOBAsmart` | Core .NET MAUI UI framework for the Android client. |
| `System.Speech`, `System.Windows.Extensions` | `10.0.7`, `10.0.7` | `Sound` | Windows text-to-speech and Windows-specific audio APIs. |
| `Serilog`, `Serilog.Extensions.Logging`, `Serilog.Sinks.Debug`, `Serilog.Sinks.File` | `4.3.1`, `10.0.0`, `3.0.0`, `7.0.0` | `Common`, `MOBAflow` | Structured logging, file logging, and debug integration. |
| `UraniumUI.Material`, `UraniumUI.Icons.MaterialIcons`, `Xamarin.AndroidX.Startup.StartupRuntime` | `2.15.0`, `2.15.0`, `1.2.0.7` | `MOBAsmart` | Material UI components, icon resources, and AndroidX startup integration. |
| `SkiaSharp`, `System.Drawing.Common` | `3.119.2`, `10.0.7` | `MOBAdisplay` | Display frame rendering and image conversion utilities. |
| `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit.Analyzers`, `NUnit3TestAdapter`, `Moq` | `10.0.0`, `18.5.1`, `4.5.1`, `4.12.0`, `6.2.0`, `4.20.72` | `Test` | Test execution, coverage collection, analyzers, and mocking. |
| `MinVer` | `7.0.0` | Build-wide via `Directory.Build.props` | Semantic version generation from git tags during builds. |
| `Microsoft.SourceLink.AzureRepos.Git` | `10.0.203` | Build-wide via `Directory.Build.targets` | Source link metadata for debugger source navigation. |

---

## License Overview

The currently referenced direct dependencies primarily fall under the following license families:

| License family | Examples |
| --- | --- |
| MIT | Most Microsoft/.NET packages, CommunityToolkit packages, NUnit, coverlet, MinVer, SourceLink |
| Apache-2.0 | Serilog family, UraniumUI family, AndroidX startup runtime, SkiaSharp |
| BSD-3-Clause | Moq |

The authoritative license for a package is the license metadata published by its upstream project or NuGet package page.
Before shipping a release with newly added dependencies, verify the package's current license and attribution requirements.

---

## License Compatibility

Based on the currently referenced direct dependencies, the repository mainly uses permissive open-source licenses that are generally compatible with MOBAflow's MIT license.
Any new dependency with copyleft, commercial, or field-of-use restrictions should be reviewed before distribution.

---

## Updating This Document

When adding or removing a direct dependency:

1. Update the package inventory in `Directory.Packages.props` or the relevant project file.
2. Reflect the change in this document, including version, usage scope, and purpose.
3. Re-check whether the package introduces new attribution or redistribution requirements.
4. Keep `README.md` references in sync when user-facing build or dependency information changes.

---

## Acknowledgments

We are grateful to the open-source maintainers and standards authors whose work makes MOBAflow possible.

---

**Last Updated:** 2026-05-02
**Scope:** Current direct dependencies and external interoperability surface  
**License:** MIT License (see [LICENSE](../LICENSE))
