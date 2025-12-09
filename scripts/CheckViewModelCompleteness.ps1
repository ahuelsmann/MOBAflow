$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8

function Get-Properties {
    param($FilePath)
    
    $content = Get-Content $FilePath -Raw
    $properties = [regex]::Matches($content, 'public\s+(?:virtual\s+)?(\S+)\s+(\w+)\s*\{[^}]*get')
    
    $result = @()
    foreach ($match in $properties) {
        $type = $match.Groups[1].Value
        $name = $match.Groups[2].Value
        
        # Skip special cases
        if ($name -in @('Model', 'EntityType', 'ModelChanged', 'Values', 'BehaviorOnLastStopValues')) { continue }
        if ($name -like '*Values') { continue }  # Enum values collections
        
        $result += [PSCustomObject]@{
            Type = $type
            Name = $name
        }
    }
    
    return $result | Sort-Object -Property Name
}

Write-Host "`n=== Journey ===" -ForegroundColor Cyan
$domainJourney = Get-Properties "C:\Repos\ahuelsmann\MOBAflow\Domain\Journey.cs"
$vmJourney = Get-Properties "C:\Repos\ahuelsmann\MOBAflow\SharedUI\ViewModel\JourneyViewModel.cs"

Write-Host "`nDomain Properties:" -ForegroundColor Yellow
$domainJourney | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nViewModel Properties:" -ForegroundColor Yellow
$vmJourney | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nMissing in ViewModel:" -ForegroundColor Red
$domainJourney | Where-Object { $_.Name -notin $vmJourney.Name } | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nExtra in ViewModel (Runtime):" -ForegroundColor Green
$vmJourney | Where-Object { $_.Name -notin $domainJourney.Name } | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`n=== Train ===" -ForegroundColor Cyan
$domainTrain = Get-Properties "C:\Repos\ahuelsmann\MOBAflow\Domain\Train.cs"
$vmTrain = Get-Properties "C:\Repos\ahuelsmann\MOBAflow\SharedUI\ViewModel\TrainViewModel.cs"

Write-Host "`nDomain Properties:" -ForegroundColor Yellow
$domainTrain | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nViewModel Properties:" -ForegroundColor Yellow
$vmTrain | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nMissing in ViewModel:" -ForegroundColor Red
$domainTrain | Where-Object { $_.Name -notin $vmTrain.Name } | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`n=== Workflow ===" -ForegroundColor Cyan
$domainWorkflow = Get-Properties "C:\Repos\ahuelsmann\MOBAflow\Domain\Workflow.cs"
$vmWorkflow = Get-Properties "C:\Repos\ahuelsmann\MOBAflow\SharedUI\ViewModel\WorkflowViewModel.cs"

Write-Host "`nDomain Properties:" -ForegroundColor Yellow
$domainWorkflow | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nViewModel Properties:" -ForegroundColor Yellow
$vmWorkflow | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }

Write-Host "`nMissing in ViewModel:" -ForegroundColor Red
$domainWorkflow | Where-Object { $_.Name -notin $vmWorkflow.Name } | ForEach-Object { Write-Host "  - $($_.Name) : $($_.Type)" }
