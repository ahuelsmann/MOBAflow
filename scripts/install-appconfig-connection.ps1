# ============================================================================
# MOBAflow - Install Azure App Configuration Connection (AUF ALLEN SYSTEMEN)
# ============================================================================
# Dieses Script setzt die Environment Variable für den Azure App Config
# Connection String.
# Führe dieses Script auf ALLEN 3 Systemen aus!
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host " MOBAflow - Install Azure App Configuration Connection" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# 1. Validate Connection String
# ============================================================================
Write-Host "[1/3] Validating Connection String..." -ForegroundColor Yellow

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "      ✗ Connection String is empty!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\install-appconfig-connection.ps1 -ConnectionString 'Endpoint=https://...'" -ForegroundColor Cyan
    exit 1
}

if (-not $ConnectionString.StartsWith("Endpoint=")) {
    Write-Host "      ✗ Invalid Connection String format!" -ForegroundColor Red
    Write-Host "      Expected format: Endpoint=https://...;Id=...;Secret=..." -ForegroundColor Yellow
    exit 1
}

Write-Host "      ✓ Connection String format valid" -ForegroundColor Green

# ============================================================================
# 2. Check existing value
# ============================================================================
Write-Host "[2/3] Checking existing environment variable..." -ForegroundColor Yellow

$currentValue = [System.Environment]::GetEnvironmentVariable('AZURE_APPCONFIG_CONNECTION', 'User')

if ([string]::IsNullOrWhiteSpace($currentValue)) {
    Write-Host "      ! No existing value found (will be set for the first time)" -ForegroundColor Yellow
} else {
    Write-Host "      ! Existing value found (will be overwritten)" -ForegroundColor Yellow
    Write-Host "      Current: $($currentValue.Substring(0, [Math]::Min(50, $currentValue.Length)))..." -ForegroundColor Gray
}

# ============================================================================
# 3. Set Environment Variable
# ============================================================================
Write-Host "[3/3] Setting environment variable..." -ForegroundColor Yellow

try {
    [System.Environment]::SetEnvironmentVariable(
        'AZURE_APPCONFIG_CONNECTION', 
        $ConnectionString, 
        'User'
    )
    
    Write-Host "      ✓ Environment variable set successfully" -ForegroundColor Green
    
    # Verify
    $newValue = [System.Environment]::GetEnvironmentVariable('AZURE_APPCONFIG_CONNECTION', 'User')
    if ($newValue -eq $ConnectionString) {
        Write-Host "      ✓ Verification successful" -ForegroundColor Green
    } else {
        Write-Host "      ✗ Verification failed!" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "      ✗ Failed to set environment variable!" -ForegroundColor Red
    Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Output
# ============================================================================
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Green
Write-Host " ✓ Installation Complete!" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "System Information:" -ForegroundColor Cyan
Write-Host "  Computer Name: $env:COMPUTERNAME" -ForegroundColor White
Write-Host "  User: $env:USERNAME" -ForegroundColor White
Write-Host "  Variable: AZURE_APPCONFIG_CONNECTION" -ForegroundColor White
Write-Host "  Scope: User (persists across reboots)" -ForegroundColor White
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host " Next Steps:" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. RESTART Visual Studio / your IDE" -ForegroundColor Yellow
Write-Host "   (Required for the new environment variable to be loaded)" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Run MOBAflow - Speech settings will be loaded automatically" -ForegroundColor White
Write-Host ""
Write-Host "3. Verify in MOBAflow:" -ForegroundColor White
Write-Host "   - Go to Settings → Speech Synthesis" -ForegroundColor Gray
Write-Host "   - Check if Speech Key and Region are populated" -ForegroundColor Gray
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host "  If Speech Key is still empty after restart:" -ForegroundColor Gray
Write-Host "  - Check Console output in MOBAflow for '[CONFIG] Azure App Configuration loaded'" -ForegroundColor Gray
Write-Host "  - Verify variable: [System.Environment]::GetEnvironmentVariable('AZURE_APPCONFIG_CONNECTION', 'User')" -ForegroundColor Gray
Write-Host ""
