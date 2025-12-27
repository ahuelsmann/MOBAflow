#!/usr/bin/env dotnet fsi

// MOBAflow Icon Resizer
// This script resizes the base icon to all required sizes for WinUI 3
// Usage: dotnet fsi resize-icons.fsx

open System
open System.IO
open System.Drawing
open System.Drawing.Imaging

let sourceIcon = "WinUI/Assets/mobaflow-icon.png"
let assetsDir = "WinUI/Assets"

// Check if source icon exists
if not (File.Exists sourceIcon) then
    printfn "❌ Source icon not found: %s" sourceIcon
    printfn "Please ensure mobaflow-icon.png exists in WinUI/Assets/"
    exit 1

printfn "🎨 Resizing WinUI 3 app icons from mobaflow-icon.png..."

// Define required icon sizes (name, width, height)
let iconSizes = [
    ("Square44x44Logo.png", 44, 44)
    ("Square44x44Logo.scale-200.png", 88, 88)
    ("Square150x150Logo.png", 150, 150)
    ("Square150x150Logo.scale-200.png", 300, 300)
    ("Wide310x150Logo.png", 310, 150)
    ("Wide310x150Logo.scale-200.png", 620, 300)
    ("StoreLogo.png", 50, 50)
    ("StoreLogo.scale-200.png", 100, 100)
    ("SplashScreen.png", 620, 300)
    ("SplashScreen.scale-200.png", 1240, 600)
    ("LargeTile.png", 310, 310)
    ("LargeTile.scale-200.png", 620, 620)
]

// Resize function
let resizeImage (sourcePath: string) (targetPath: string) (width: int) (height: int) =
    try
        use sourceImage = Image.FromFile(sourcePath)
        use resized = new Bitmap(width, height)
        use graphics = Graphics.FromImage(resized)
        
        graphics.InterpolationMode <- Drawing2D.InterpolationMode.HighQualityBicubic
        graphics.SmoothingMode <- Drawing2D.SmoothingMode.HighQuality
        graphics.PixelOffsetMode <- Drawing2D.PixelOffsetMode.HighQuality
        
        graphics.DrawImage(sourceImage, 0, 0, width, height)
        resized.Save(targetPath, ImageFormat.Png)
        
        printfn "✅ Created: %s (%dx%d)" (Path.GetFileName targetPath) width height
        true
    with ex ->
        printfn "❌ Failed to create %s: %s" targetPath ex.Message
        false

// Resize all icons
let results = 
    iconSizes
    |> List.map (fun (name, w, h) ->
        let targetPath = Path.Combine(assetsDir, name)
        resizeImage sourceIcon targetPath w h)

let successCount = results |> List.filter id |> List.length
let totalCount = iconSizes.Length

printfn ""
printfn "✨ Icon resizing complete: %d/%d successful" successCount totalCount
printfn ""
printfn "Next steps:"
printfn "1. Rebuild your WinUI project: dotnet build WinUI/WinUI.csproj"
printfn "2. Clear Windows icon cache (see instructions below)"
printfn "3. Restart your app"
printfn ""
printfn "To clear Windows icon cache:"
printfn "  ie4uinit.exe -show"
