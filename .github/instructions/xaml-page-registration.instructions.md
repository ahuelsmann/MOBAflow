---
description: Prevents XAML Page registration issues when recreating files
applyTo: "WinUI/**/*.xaml"
---

# XAML Page Registration (CRITICAL!)

## Problem: Page files excluded from XAML Compiler

When creating, deleting, or recreating XAML files (especially via `remove_file` and `create_file` tools), 
MSBuild/WinUI SDK may incorrectly add entries to `.csproj` that **exclude** the page from compilation:

```xml
<!-- WRONG: Page excluded from XAML compilation -->
<ItemGroup>
    <Page Remove="View\TrainControlPage.xaml" />
</ItemGroup>
<ItemGroup>
    <None Update="View\TrainControlPage.xaml">
        <Generator>MSBuild:Compile</Generator>
    </None>
</ItemGroup>
```

## Symptoms

- `CS0103: The name 'InitializeComponent' does not exist in the current context`
- Page not appearing in XAML compiler output (check `obj\Debug\output.json`)
- `MSB3073: XamlCompiler.exe` errors

## Cause

- Deleting XAML files via tools sometimes adds `<Page Remove="..."/>` entries
- SDK auto-globbing conflict with explicit exclusions
- File recreated but csproj still has removal entry

## Solution: Check .csproj after any XAML file operations!

The WinUI SDK automatically includes all `.xaml` files as Page items.
Only use `<Page Remove="..."/>` for intentional exclusions (like temporary files).

## Verification Steps

### 1. Check csproj for Page Remove entries:
```powershell
Select-String -Path "WinUI\WinUI.csproj" -Pattern "Page Remove" -SimpleMatch
```

### 2. If found, remove the exclusion entries for your page:
```xml
<!-- DELETE these lines if your page should compile -->
<Page Remove="View\YourPage.xaml" />
<None Update="View\YourPage.xaml">...</None>
```

### 3. Clean and rebuild:
```powershell
dotnet clean WinUI\WinUI.csproj
dotnet build WinUI\WinUI.csproj
```

## Prevention

When using file tools to recreate XAML:

1. **After deleting a XAML file**, check csproj for auto-added `<Page Remove="..."/>` entries
2. **After creating a XAML file**, verify no conflicting entries in csproj
3. **Run build** to confirm XAML compiler recognizes the file

## Checklist for Copilot

When recreating or modifying XAML files:

- [ ] Check `WinUI\WinUI.csproj` for `<Page Remove="..."/>` entries
- [ ] Remove any unintended exclusions for the page being created
- [ ] Run `dotnet build` to verify XAML compilation
- [ ] Confirm `InitializeComponent()` compiles without errors

---

**Last Updated:** 2026-01-07
**Related Error:** CS0103, MSB3073, WMC1119
