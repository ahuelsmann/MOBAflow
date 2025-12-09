$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
[Console]::InputEncoding=[Text.Encoding]::UTF8
$ProgressPreference='SilentlyContinue'
if ($PSStyle) { $PSStyle.OutputRendering='Ansi' }

# Migration Script: Convert Reference-Based JSON to Nested Objects
$jsonPath = "C:\Repos\ahuelsmann\MOBAflow\artifacts\Debug\example-solution-v2.json"
$backupPath = "$jsonPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Host "🔄 Converting JSON from Reference-Based to Nested format..."
Write-Host "📂 File: $jsonPath"

# Backup original
Copy-Item -Path $jsonPath -Destination $backupPath
Write-Host "✅ Backup created: $backupPath"

# Load JSON
$json = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json

# Process each project
foreach ($project in $json.Projects) {
    Write-Host "📦 Processing project: $($project.Name)"
    
    # Create lookup dictionary for stations
    $stationLookup = @{}
    foreach ($station in $project.Stations) {
        $stationLookup[$station.Id] = $station
    }
    
    Write-Host "   - Found $($project.Stations.Count) stations in project"
    
    # Process each journey
    foreach ($journey in $project.Journeys) {
        Write-Host "   🚂 Journey: $($journey.Name)"
        
        # Check if journey has StationIds
        if ($journey.PSObject.Properties.Name -contains 'StationIds') {
            $stationCount = $journey.StationIds.Count
            Write-Host "      - Converting $stationCount StationIds to nested Stations"
            
            # Resolve StationIds to full Station objects
            $resolvedStations = @()
            foreach ($stationId in $journey.StationIds) {
                if ($stationLookup.ContainsKey($stationId)) {
                    $resolvedStations += $stationLookup[$stationId]
                    Write-Host "      - Resolved: $($stationLookup[$stationId].Name)"
                } else {
                    Write-Host "      ⚠️  Station ID not found: $stationId"
                }
            }
            
            # Replace StationIds with Stations
            $journey.PSObject.Properties.Remove('StationIds')
            $journey | Add-Member -MemberType NoteProperty -Name 'Stations' -Value $resolvedStations -Force
            
            Write-Host "      ✅ Converted to $($resolvedStations.Count) nested Stations"
        }
    }
    
    # Remove Project.Stations (no longer needed with nested structure)
    Write-Host "   - Removing Project.Stations (now nested in Journeys)"
    $project.PSObject.Properties.Remove('Stations')
}

# Save modified JSON
$json | ConvertTo-Json -Depth 100 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Host ""
Write-Host "✅ Migration complete!"
Write-Host "📄 Modified file: $jsonPath"
Write-Host "💾 Backup: $backupPath"
