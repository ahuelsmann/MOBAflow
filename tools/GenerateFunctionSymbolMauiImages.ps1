#Requires -Version 7.0
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$Theme = "dark",
    [int]$Size = 32
)

$ErrorActionPreference = "Stop"
$pngDir = Join-Path $RepoRoot "MOBAflow\Assets\FunctionSymbols\$Theme\$Size"
$excluded = @("door_close.png", "door_open.png", "door_blocked.png", "mobaflow-icon.png")

$pngFiles = Get-ChildItem -LiteralPath $pngDir -Filter "*.png" |
    Where-Object { $excluded -notcontains $_.Name } |
    Sort-Object Name

$propsPath = Join-Path $RepoRoot "MOBAsmart\FunctionSymbolMauiImages.props"
$relativeDir = "..\MOBAflow\Assets\FunctionSymbols\$Theme\$Size"
$lines = @("<Project>", "  <ItemGroup>")
foreach ($png in $pngFiles) {
    $lines += "    <MauiImage Include=`"$relativeDir\$($png.Name)`" Link=`"Resources\Images\$($png.Name)`" />"
}
$lines += "  </ItemGroup>"
$lines += "</Project>"
$lines | Set-Content -LiteralPath $propsPath -Encoding UTF8
Write-Host "Wrote $($pngFiles.Count) MauiImage PNG entries."
