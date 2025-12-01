$file = "SharedUI\ViewModel\MainWindowViewModel.cs"
$content = Get-Content $file -Raw

# 1. Add AppSettings import
$content = $content -replace "using Moba.Backend.Interface;", "using Moba.Backend.Interface;`nusing Moba.Common.Configuration;"

# 2. Add AppSettings field
$content = $content -replace "private readonly IUiDispatcher _uiDispatcher;", "private readonly IUiDispatcher _uiDispatcher;`n    private readonly AppSettings _settings;"

# 3. Update constructor signature
$content = $content -replace "public MainWindowViewModel\(`n        IIoService ioService,`n        IZ21 z21,`n        IJourneyManagerFactory journeyManagerFactory,`n        IUiDispatcher uiDispatcher,`n        Solution solution\)", "public MainWindowViewModel(`n        IIoService ioService,`n        IZ21 z21,`n        IJourneyManagerFactory journeyManagerFactory,`n        IUiDispatcher uiDispatcher,`n        AppSettings settings,`n        Solution solution)"

# 4. Add _settings assignment in constructor
$content = $content -replace "_uiDispatcher = uiDispatcher;", "_uiDispatcher = uiDispatcher;`n        _settings = settings;"

# 5. Remove Settings assignment in LoadSolution
$content = $content -replace "Solution\.Settings = loadedSolution\.Settings \?\? new Settings\(\);", "// Settings are now in AppSettings (not in Solution)"

# 6. Replace Solution.Settings references with _settings.Z21
$content = $content -replace "Solution\?\.Settings != null && !string\.IsNullOrEmpty\(Solution\.Settings\.CurrentIpAddress\)", "!string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress)"
$content = $content -replace "Solution\.Settings\.CurrentIpAddress", "_settings.Z21.CurrentIpAddress"
$content = $content -replace "Solution\.Settings\.DefaultPort", "_settings.Z21.DefaultPort"
$content = $content -replace 'Z21StatusText = "No IP address configured in Solution\.Settings";', 'Z21StatusText = "No IP address configured in AppSettings";'

# 7. Remove Settings reset in NewSolution
$content = $content -replace "Solution\.Settings = new Settings\(\);", "// Settings remain in AppSettings (no reset needed)"

# 8. Fix the return statement in private Project? GetProjectForSettings
$content = $content -replace "return Solution\.Settings == setting \? Solution\.Projects\.FirstOrDefault\(\) : null;", "// TODO: Settings refactoring - this method may need rework`n            return Solution.Projects.FirstOrDefault();"

Set-Content -Path $file -Value $content -NoNewline
Write-Host "✅ MainWindowViewModel.cs updated"
