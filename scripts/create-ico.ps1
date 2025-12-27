# Convert PNG to ICO for Windows executable icon
# This creates a multi-resolution .ico file from the base PNG

param(
    [string]$SourcePng = "WinUI\Assets\mobaflow-icon.png",
    [string]$OutputIco = "WinUI\Assets\mobaflow-icon.ico"
)

if (-not (Test-Path $SourcePng)) {
    Write-Error "Source PNG not found: $SourcePng"
    exit 1
}

Write-Host "🎨 Converting PNG to ICO format..." -ForegroundColor Cyan

Add-Type -AssemblyName System.Drawing

try {
    # Load source image
    $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePng))
    
    # Create icon with multiple sizes (16, 32, 48, 256)
    $sizes = @(16, 32, 48, 256)
    $iconStream = New-Object System.IO.MemoryStream
    
    # ICO file header (6 bytes)
    $iconStream.WriteByte(0)  # Reserved
    $iconStream.WriteByte(0)  # Reserved
    $iconStream.WriteByte(1)  # Type: 1 = ICO
    $iconStream.WriteByte(0)  # Type continuation
    $iconStream.WriteByte($sizes.Count)  # Number of images
    $iconStream.WriteByte(0)  # Number of images continuation
    
    $imageDataStreams = @()
    $currentOffset = 6 + ($sizes.Count * 16)  # Header + directory entries
    
    foreach ($size in $sizes) {
        # Create resized image
        $resized = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($sourceImage, 0, 0, $size, $size)
        
        # Save to PNG stream
        $imageStream = New-Object System.IO.MemoryStream
        $resized.Save($imageStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $imageData = $imageStream.ToArray()
        $imageDataStreams += $imageData
        
        # Write directory entry (16 bytes)
        $iconStream.WriteByte([byte]($size % 256))  # Width (0 = 256)
        $iconStream.WriteByte([byte]($size % 256))  # Height (0 = 256)
        $iconStream.WriteByte(0)  # Color palette
        $iconStream.WriteByte(0)  # Reserved
        $iconStream.WriteByte(1)  # Color planes
        $iconStream.WriteByte(0)  # Color planes continuation
        $iconStream.WriteByte(32) # Bits per pixel
        $iconStream.WriteByte(0)  # Bits per pixel continuation
        
        # Image data size (4 bytes, little-endian)
        $iconStream.WriteByte([byte]($imageData.Length -band 0xFF))
        $iconStream.WriteByte([byte](($imageData.Length -shr 8) -band 0xFF))
        $iconStream.WriteByte([byte](($imageData.Length -shr 16) -band 0xFF))
        $iconStream.WriteByte([byte](($imageData.Length -shr 24) -band 0xFF))
        
        # Image data offset (4 bytes, little-endian)
        $iconStream.WriteByte([byte]($currentOffset -band 0xFF))
        $iconStream.WriteByte([byte](($currentOffset -shr 8) -band 0xFF))
        $iconStream.WriteByte([byte](($currentOffset -shr 16) -band 0xFF))
        $iconStream.WriteByte([byte](($currentOffset -shr 24) -band 0xFF))
        
        $currentOffset += $imageData.Length
        
        $graphics.Dispose()
        $resized.Dispose()
        $imageStream.Dispose()
    }
    
    # Write all image data
    foreach ($imageData in $imageDataStreams) {
        $iconStream.Write($imageData, 0, $imageData.Length)
    }
    
    # Save ICO file
    [System.IO.File]::WriteAllBytes((Resolve-Path $OutputIco -ErrorAction SilentlyContinue).Path ?? $OutputIco, $iconStream.ToArray())
    
    $iconStream.Dispose()
    $sourceImage.Dispose()
    
    Write-Host "✅ ICO file created successfully: $OutputIco" -ForegroundColor Green
    Write-Host "   Contains sizes: $($sizes -join ', ')px" -ForegroundColor Cyan
}
catch {
    Write-Error "Failed to create ICO: $_"
    exit 1
}
