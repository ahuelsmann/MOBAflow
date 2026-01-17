# Doxygen Documentation Generation

MOBAflow uses **Doxygen** to generate comprehensive technical API documentation from XML documentation comments in the C# source code.

---

## 📋 Prerequisites

### Install Doxygen

**Windows (Recommended):**
```powershell
# Using Chocolatey
choco install doxygen.install

# Using winget
winget install DimitriVanHeesch.Doxygen

# Manual Download
https://www.doxygen.nl/download.html
```

**macOS:**
```bash
brew install doxygen
```

**Linux:**
```bash
sudo apt-get install doxygen graphviz
```

### Install Graphviz (for diagrams)

**Windows:**
```powershell
choco install graphviz
```

**macOS:**
```bash
brew install graphviz
```

**Linux:**
```bash
sudo apt-get install graphviz
```

---

## 🚀 Generate Documentation

### Full Documentation

```powershell
# From repository root
doxygen Doxyfile
```

Output will be in: `docs/api/html/index.html`

### Open in Browser

```powershell
# Windows
start docs/api/html/index.html

# macOS
open docs/api/html/index.html

# Linux
xdg-open docs/api/html/index.html
```

---

## 📁 What Gets Documented

Doxygen processes the following projects:

| Project | Description |
|---------|-------------|
| **Domain/** | Core business entities (POCO models) |
| **Backend/** | Services and business logic |
| **Common/** | Shared utilities and interfaces |
| **SharedUI/** | ViewModels and UI logic |
| **WinUI.Controls/** | WinUI 3 user controls |
| **MAUI.Controls/** | .NET MAUI controls |
| **TrackPlan/** | Track plan domain models |
| **TrackPlan.Renderer/** | Track rendering engine |
| **TrackPlan.Editor/** | Track editor logic |
| **TrackLibrary.PikoA/** | Piko A-Track library |
| **Sound/** | Audio system |

### Excluded

- `obj/` and `bin/` directories
- Auto-generated files (`.g.cs`, `.Designer.cs`)
- `AssemblyInfo.cs` files
- XAML code-behind files (`.xaml.cs`)

---

## 📖 Documentation Standards

### Class Documentation

```csharp
/// <summary>
/// Brief one-line description of the class.
/// </summary>
/// <remarks>
/// Detailed explanation of the class purpose, usage, and behavior.
/// Multiple paragraphs are allowed.
/// </remarks>
public class MyClass
{
}
```

### Property Documentation

```csharp
/// <summary>
/// Gets or sets the name of the train.
/// </summary>
/// <value>The train's display name. Default is an empty string.</value>
public string TrainName { get; set; }
```

### Method Documentation

```csharp
/// <summary>
/// Connects to the Z21 command station asynchronously.
/// </summary>
/// <param name="ipAddress">The IP address of the Z21 station.</param>
/// <param name="cancellationToken">Cancellation token for the async operation.</param>
/// <returns>A task representing the async operation, with a boolean indicating success.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="ipAddress"/> is null.</exception>
public async Task<bool> ConnectAsync(string ipAddress, CancellationToken cancellationToken = default)
{
}
```

### Parameter Documentation

```csharp
/// <param name="name">The name parameter (required).</param>
/// <param name="value">The value to assign (optional).</param>
```

### Return Value Documentation

```csharp
/// <returns>True if successful; otherwise, false.</returns>
/// <returns>A collection of trains, or an empty collection if none found.</returns>
```

### Exception Documentation

```csharp
/// <exception cref="ArgumentNullException">Thrown when required parameter is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when the operation fails.</exception>
```

---

## 🎨 Doxygen Features

### Generated Content

- **Class Hierarchy** - Inheritance diagrams
- **Class Diagrams** - UML-style class relationships
- **Namespace Overview** - Logical code organization
- **Member Index** - Alphabetical listing of all members
- **File Documentation** - Source file overview
- **Call Graphs** - Method call relationships (if enabled)
- **Search** - Full-text search across all documentation

### Diagram Types

- **Inheritance Graphs** - Shows base classes and derived classes
- **Collaboration Graphs** - Shows class dependencies
- **Directory Graphs** - Project structure visualization
- **Include Graphs** - File dependency graphs

---

## ⚙️ Configuration

### Doxyfile Settings

Key configuration options in `Doxyfile`:

| Setting | Value | Purpose |
|---------|-------|---------|
| `PROJECT_NAME` | "MOBAflow Platform" | Documentation title |
| `OUTPUT_DIRECTORY` | docs/api | Output location |
| `EXTRACT_ALL` | NO | Only documented members |
| `HIDE_UNDOC_MEMBERS` | YES | Hide undocumented items |
| `WARN_IF_UNDOCUMENTED` | YES | Warn about missing docs |
| `GENERATE_HTML` | YES | HTML output |
| `GENERATE_XML` | YES | XML output (for other tools) |
| `HAVE_DOT` | YES | Enable diagrams |
| `UML_LOOK` | YES | UML-style diagrams |
| `INTERACTIVE_SVG` | YES | Interactive SVG diagrams |
| `SOURCE_BROWSER` | YES | Include source code |

### Customization

Edit `Doxyfile` to customize:
- Project name and version
- Input/output directories
- Diagram styles
- Warning levels
- Output formats

---

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
name: Generate Documentation

on:
  push:
    branches: [ main ]

jobs:
  docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Doxygen
        run: sudo apt-get install -y doxygen graphviz
      
      - name: Generate Docs
        run: doxygen Doxyfile
      
      - name: Deploy to GitHub Pages
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./docs/api/html
```

### Azure DevOps Pipeline Example

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
  - task: PowerShell@2
    displayName: 'Install Doxygen'
    inputs:
      targetType: 'inline'
      script: 'choco install doxygen.install graphviz -y'
  
  - task: PowerShell@2
    displayName: 'Generate Documentation'
    inputs:
      targetType: 'inline'
      script: 'doxygen Doxyfile'
  
  - task: PublishBuildArtifacts@1
    inputs:
      PathtoPublish: 'docs/api/html'
      ArtifactName: 'documentation'
```

---

## 📚 Additional Resources

- **Doxygen Manual:** https://www.doxygen.nl/manual/index.html
- **C# Documentation:** https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/
- **Graphviz:** https://graphviz.org/
- **MOBAflow Architecture:** [ARCHITECTURE.md](ARCHITECTURE.md)

---

## 🐛 Troubleshooting

### "Doxygen not found"

Ensure Doxygen is in your PATH:
```powershell
doxygen --version
```

### "dot: command not found"

Install Graphviz and ensure it's in your PATH:
```powershell
dot -V
```

### Empty Documentation

Check that:
1. Classes have XML documentation comments (`///`)
2. `EXTRACT_ALL` is set to `YES` (or add documentation)
3. Files are not in `EXCLUDE_PATTERNS`

### Missing Diagrams

Ensure:
1. Graphviz is installed
2. `HAVE_DOT` is set to `YES` in Doxyfile
3. `DOT_PATH` points to Graphviz installation (if needed)

---

**Made with ❤️ for model railroad enthusiasts**
