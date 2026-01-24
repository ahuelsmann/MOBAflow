#!/usr/bin/env pwsh
# Redistribute TrackPlan classes to new modular projects

$sourcePath = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan"
$targetPaths = @{
    "Graph" = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan.Domain\Graph"
    "Constraint" = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan.Domain\Constraint"
    "TrackSystem" = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan.Domain\TrackSystem"
    "Topology" = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan.Renderer\Service"
    "Service" = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan.Renderer\Service"
    "Geometry" = "C:\Repo\ahuelsmann\MOBAflow\TrackPlan.Renderer\Geometry"
}

# Mapping: (SourceFolder, SourceFile) -> (TargetFolder, TargetNamespace)
$fileMappings = @(
    # Graph files to Domain/Graph
    @{ Source = "Graph\Endcap.cs"; Target = "TrackPlan.Domain\Graph"; Namespace = "Moba.TrackPlan.Graph" }
    @{ Source = "Graph\Endpoint.cs"; Target = "TrackPlan.Domain\Graph"; Namespace = "Moba.TrackPlan.Graph" }
    @{ Source = "Graph\Isolator.cs"; Target = "TrackPlan.Domain\Graph"; Namespace = "Moba.TrackPlan.Graph" }
    @{ Source = "Graph\Section.cs"; Target = "TrackPlan.Domain\Graph"; Namespace = "Moba.TrackPlan.Graph" }
    
    # TrackSystem files to Domain/TrackSystem
    @{ Source = "TrackSystem\TrackEnd.cs"; Target = "TrackPlan.Domain\TrackSystem"; Namespace = "Moba.TrackPlan.TrackSystem" }
    @{ Source = "TrackSystem\TrackGeometrySpec.cs"; Target = "TrackPlan.Domain\TrackSystem"; Namespace = "Moba.TrackPlan.TrackSystem" }
    @{ Source = "TrackSystem\SwitchRoutingModel.cs"; Target = "TrackPlan.Domain\TrackSystem"; Namespace = "Moba.TrackPlan.TrackSystem" }
    
    # Constraint files to Domain/Constraint
    @{ Source = "Constraint\ConstraintViolation.cs"; Target = "TrackPlan.Domain\Constraint"; Namespace = "Moba.TrackPlan.Constraint" }
    @{ Source = "Constraint\ITopologyConstraint.cs"; Target = "TrackPlan.Domain\Constraint"; Namespace = "Moba.TrackPlan.Constraint" }
    @{ Source = "Constraint\DuplicateFeedbackPointNumberConstraint.cs"; Target = "TrackPlan.Domain\Constraint"; Namespace = "Moba.TrackPlan.Constraint" }
    @{ Source = "Constraint\GeometryConnectionConstraint.cs"; Target = "TrackPlan.Domain\Constraint"; Namespace = "Moba.TrackPlan.Constraint" }
    
    # Topology files to Renderer/Service
    @{ Source = "Topology\TopologyResolver.cs"; Target = "TrackPlan.Renderer\Service"; Namespace = "Moba.TrackPlan.Renderer.Service" }
    
    # Service files to Renderer/Service
    @{ Source = "Service\TrackConnectionService.cs"; Target = "TrackPlan.Renderer\Service"; Namespace = "Moba.TrackPlan.Renderer.Service" }
    
    # Geometry files to Renderer/Geometry
    @{ Source = "Geometry\GeometryCalculationEngine.cs"; Target = "TrackPlan.Renderer\Geometry"; Namespace = "Moba.TrackPlan.Renderer.Geometry" }
    
    # UseCase files - currently unclear, skip for now
    # @{ Source = "UseCase\AssignFeedbackPointToTrackUseCase.cs"; Target = "TrackPlan.Renderer\Service"; Namespace = "Moba.TrackPlan.Renderer.Service" }
)

Write-Host "📦 Starting class redistribution..."
Write-Host ""

foreach ($mapping in $fileMappings) {
    $sourceFile = Join-Path $sourcePath $mapping.Source
    $targetDir = Join-Path "C:\Repo\ahuelsmann\MOBAflow" $mapping.Target
    $targetFile = Join-Path $targetDir (Split-Path $mapping.Source -Leaf)
    
    if (-not (Test-Path $sourceFile)) {
        Write-Host "⚠️  Source not found: $sourceFile"
        continue
    }
    
    # Create target directory if needed
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        Write-Host "📁 Created directory: $targetDir"
    }
    
    # Read source file
    $content = [IO.File]::ReadAllText($sourceFile)
    
    # Replace namespace
    $oldNamespace = "namespace Moba.TrackPlan"
    $newNamespace = "namespace $($mapping.Namespace)"
    $content = $content -replace "namespace Moba\.TrackPlan[^;]*;", "$newNamespace;"
    
    # Write to target
    [IO.File]::WriteAllText($targetFile, $content)
    
    Write-Host "✅ Moved: $($mapping.Source) → $($mapping.Target) [Namespace: $($mapping.Namespace)]"
}

Write-Host ""
Write-Host "🎉 Redistribution complete!"
Write-Host ""
Write-Host "⚠️  Next steps:"
Write-Host "1. Update GlobalUsings.cs in all projects"
Write-Host "2. Fix using statements in all moved files"
Write-Host "3. Run 'dotnet build -m:1' to identify remaining issues"
Write-Host "4. Clean up old TrackPlan project"
