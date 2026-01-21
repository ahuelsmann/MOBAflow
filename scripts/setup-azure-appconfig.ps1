# ============================================================================
# MOBAflow - Azure App Configuration Setup (EINMALIG)
# ============================================================================
# Dieses Script erstellt den Azure App Configuration Service und
# gibt den Connection String aus.
# Führe dieses Script NUR EINMAL auf EINEM System aus!
# ============================================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "mobaflow-resources",
    
    [Parameter(Mandatory=$false)]
    [string]$AppConfigName = "mobaflow-config",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "germanywestcentral",
    
    [Parameter(Mandatory=$false)]
    [string]$SpeechKey = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SpeechRegion = "germanywestcentral"
)

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host " MOBAflow - Azure App Configuration Setup" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# 1. Azure CLI Check
# ============================================================================
Write-Host "[1/6] Checking Azure CLI..." -ForegroundColor Yellow

try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "      ✓ Azure CLI installed: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "      ✗ Azure CLI not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install Azure CLI from: https://aka.ms/azure-cli" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# 2. Azure Login Check
# ============================================================================
Write-Host "[2/6] Checking Azure Login..." -ForegroundColor Yellow

try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "      ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "      ✓ Subscription: $($account.name)" -ForegroundColor Green
} catch {
    Write-Host "      ✗ Not logged in to Azure!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Running 'az login'..." -ForegroundColor Yellow
    az login
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "      ✗ Login failed!" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# 3. Create Resource Group (if not exists)
# ============================================================================
Write-Host "[3/6] Creating Resource Group..." -ForegroundColor Yellow

$rgExists = az group exists --name $ResourceGroupName
if ($rgExists -eq "true") {
    Write-Host "      ✓ Resource Group '$ResourceGroupName' already exists" -ForegroundColor Green
} else {
    Write-Host "      Creating Resource Group '$ResourceGroupName'..." -ForegroundColor Cyan
    az group create --name $ResourceGroupName --location $Location --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "      ✓ Resource Group created successfully" -ForegroundColor Green
    } else {
        Write-Host "      ✗ Failed to create Resource Group!" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# 4. Create App Configuration (if not exists)
# ============================================================================
Write-Host "[4/6] Creating App Configuration..." -ForegroundColor Yellow

$appConfigExists = az appconfig show --name $AppConfigName --resource-group $ResourceGroupName --output none 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ App Configuration '$AppConfigName' already exists" -ForegroundColor Green
} else {
    Write-Host "      Creating App Configuration '$AppConfigName' (Free Tier)..." -ForegroundColor Cyan
    az appconfig create `
        --name $AppConfigName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Free `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "      ✓ App Configuration created successfully" -ForegroundColor Green
        Write-Host "      Waiting 10 seconds for resource to be ready..." -ForegroundColor Cyan
        Start-Sleep -Seconds 10
    } else {
        Write-Host "      ✗ Failed to create App Configuration!" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# 5. Add Speech Settings (if provided)
# ============================================================================
Write-Host "[5/6] Adding Speech Settings..." -ForegroundColor Yellow

if ([string]::IsNullOrWhiteSpace($SpeechKey)) {
    Write-Host "      ! No Speech Key provided (use -SpeechKey parameter)" -ForegroundColor Yellow
    Write-Host "      You can add it later in the Azure Portal or via:" -ForegroundColor Yellow
    Write-Host "      az appconfig kv set --name $AppConfigName --key 'Speech:Key' --value 'YOUR-KEY'" -ForegroundColor Cyan
} else {
    Write-Host "      Adding Speech:Key..." -ForegroundColor Cyan
    az appconfig kv set `
        --name $AppConfigName `
        --key "Speech:Key" `
        --value $SpeechKey `
        --yes `
        --output none
    
    Write-Host "      Adding Speech:Region..." -ForegroundColor Cyan
    az appconfig kv set `
        --name $AppConfigName `
        --key "Speech:Region" `
        --value $SpeechRegion `
        --yes `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "      ✓ Speech settings added successfully" -ForegroundColor Green
    } else {
        Write-Host "      ✗ Failed to add Speech settings!" -ForegroundColor Red
    }
}

# ============================================================================
# 6. Get Connection String
# ============================================================================
Write-Host "[6/6] Retrieving Connection String..." -ForegroundColor Yellow

$connectionStrings = az appconfig credential list --name $AppConfigName --resource-group $ResourceGroupName --output json | ConvertFrom-Json
$primaryConnectionString = $connectionStrings[0].connectionString

if ([string]::IsNullOrWhiteSpace($primaryConnectionString)) {
    Write-Host "      ✗ Failed to retrieve Connection String!" -ForegroundColor Red
    exit 1
}

Write-Host "      ✓ Connection String retrieved successfully" -ForegroundColor Green

# ============================================================================
# Output
# ============================================================================
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Green
Write-Host " ✓ Azure App Configuration Setup Complete!" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Connection String (copy this):" -ForegroundColor Yellow
Write-Host ""
Write-Host $primaryConnectionString -ForegroundColor Cyan
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host " Next Steps:" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. COPY the Connection String above" -ForegroundColor White
Write-Host ""
Write-Host "2. On ALL 3 systems, run:" -ForegroundColor White
Write-Host "   .\scripts\install-appconfig-connection.ps1 -ConnectionString 'YOUR-CONNECTION-STRING'" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Restart Visual Studio on all systems" -ForegroundColor White
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan

# ============================================================================
# Save to file for easy copy
# ============================================================================
$outputFile = "azure-appconfig-connection.txt"
$primaryConnectionString | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "Connection String also saved to: $outputFile" -ForegroundColor Green
Write-Host ""
