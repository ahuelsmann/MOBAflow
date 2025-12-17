# Azure DevOps Pipelines

This folder contains Azure DevOps pipeline definitions for CI/CD.

## 📁 Structure

```
.azure-pipelines/
├── pr-validation.yml          # PR validation (build + test)
├── templates/
│   ├── build.yml              # Reusable build template
│   └── test.yml               # Reusable test template
└── variables/
    └── common-variables.yml   # Shared variables
```

## 🚀 Pipelines

### PR Validation (`pr-validation.yml`)

**Purpose:** Validates Pull Requests before they can be merged to `main`.

**Triggers:**
- Pull Requests to `main` branch
- Excludes: `*.md` files, `docs/`, `.github/`

**Steps:**
1. Install .NET 10 SDK
2. Install Windows App SDK workload
3. Restore NuGet packages
4. Build solution (Release)
5. Run unit tests (NUnit)
6. Publish test results

**Branch Policy Setup:**

In Azure DevOps, configure branch policy for `main`:

1. Go to **Project Settings** → **Repositories** → **Policies**
2. Select `main` branch
3. Add **Build Validation**:
   - Pipeline: `pr-validation`
   - Trigger: Automatic
   - Policy requirement: Required
   - Build expiration: Immediately

## 📋 Templates

### `templates/build.yml`

Reusable build template with parameters:
- `projects`: Projects to build (default: `**/*.sln`)
- `configuration`: Build configuration (default: `Release`)
- `restorePackages`: Restore NuGet packages (default: `true`)

### `templates/test.yml`

Reusable test template with parameters:
- `testProjects`: Test projects to run (default: `**/Test.csproj`)
- `configuration`: Build configuration (default: `Release`)
- `testRunTitle`: Display name for test results

## 🔧 Requirements

- **Agent:** `windows-latest`
- **.NET SDK:** 10.0.x (preview)
- **Workloads:** `maui-windows` (for WinUI)

## 📝 Future Pipelines (TODO)

- [ ] `ci-main.yml` - Continuous Integration on main branch
- [ ] `release.yml` - Release pipeline with MSIX packaging
- [ ] `build-maui.yml` - MAUI Android build
- [ ] `build-webapp.yml` - Blazor WebApp build and deploy
