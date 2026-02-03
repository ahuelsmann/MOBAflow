// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Programmatic skin resource builder for WinUI 3.
/// Avoids XAML parsing issues by building ResourceDictionaries in C#.
/// </summary>
public static class SkinResourceBuilder
{
    /// <summary>
    /// Creates the Blue skin ResourceDictionary.
    /// </summary>
    public static ResourceDictionary BuildBlueSkin()
    {
        var dict = new ResourceDictionary();

        // Primary Accent Colors
        dict["ThemeAccentColor"] = Color.FromArgb(255, 0, 120, 212); // #0078D4
        dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 0, 90, 158); // #005A9E
        dict["ThemeAccentLightColor"] = Color.FromArgb(255, 80, 180, 247); // #50B4F7

        // Control Backgrounds
        dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 243, 243, 243); // #F3F3F3
        dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 235, 235, 235); // #EBEBEB
        dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 224, 224, 224); // #E0E0E0

        // Brushes
        dict["ThemeAccentBrush"] = new SolidColorBrush((Color)dict["ThemeAccentColor"]);
        dict["ThemeAccentDarkBrush"] = new SolidColorBrush((Color)dict["ThemeAccentDarkColor"]);
        dict["ThemeAccentLightBrush"] = new SolidColorBrush((Color)dict["ThemeAccentLightColor"]);

        // Page-specific Colors
        dict["TrainControlHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["TachometerNeedleColor"] = (Color)dict["ThemeAccentColor"];
        dict["SpeedDisplayBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["FunctionButtonActiveColor"] = (Color)dict["ThemeAccentLightColor"];

        dict["ToolboxHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["ToolboxItemHoverColor"] = (Color)dict["ThemeControlBackgroundHoverColor"];
        dict["PropertiesPanelBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["ElementSelectionColor"] = (Color)dict["ThemeAccentColor"];

        return dict;
    }

    /// <summary>
    /// Creates the Green skin ResourceDictionary.
    /// </summary>
    public static ResourceDictionary BuildGreenSkin()
    {
        var dict = new ResourceDictionary();

        // Primary Accent Colors - Green
        dict["ThemeAccentColor"] = Color.FromArgb(255, 42, 164, 55); // #2AA437
        dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 30, 125, 45); // #1E7D2D
        dict["ThemeAccentLightColor"] = Color.FromArgb(255, 94, 200, 103); // #5EC867

        // Control Backgrounds - Silver/Gray
        dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 218, 218, 218); // #DADADA
        dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 199, 199, 199); // #C7C7C7
        dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 176, 176, 176); // #B0B0B0

        // Brushes
        dict["ThemeAccentBrush"] = new SolidColorBrush((Color)dict["ThemeAccentColor"]);
        dict["ThemeAccentDarkBrush"] = new SolidColorBrush((Color)dict["ThemeAccentDarkColor"]);
        dict["ThemeAccentLightBrush"] = new SolidColorBrush((Color)dict["ThemeAccentLightColor"]);

        // Page-specific Colors
        dict["TrainControlHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["TachometerNeedleColor"] = (Color)dict["ThemeAccentColor"];
        dict["SpeedDisplayBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["FunctionButtonActiveColor"] = (Color)dict["ThemeAccentLightColor"];

        dict["ToolboxHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["ToolboxItemHoverColor"] = (Color)dict["ThemeControlBackgroundHoverColor"];
        dict["PropertiesPanelBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["ElementSelectionColor"] = (Color)dict["ThemeAccentColor"];

        return dict;
    }

    /// <summary>
    /// Creates the Orange skin ResourceDictionary (dark style with deep orange accents).
    /// </summary>
    public static ResourceDictionary BuildOrangeSkin()
    {
        var dict = new ResourceDictionary();

        // Primary Accent Colors - Deep Orange
        dict["ThemeAccentColor"] = Color.FromArgb(255, 255, 102, 0); // #FF6600
        dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 204, 82, 0); // #CC5200
        dict["ThemeAccentLightColor"] = Color.FromArgb(255, 255, 153, 51); // #FF9933

        // Control Backgrounds - Black Style
        dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 15, 15, 15); // #0F0F0F
        dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 31, 31, 31); // #1F1F1F
        dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 47, 47, 47); // #2F2F2F

        // Text Colors
        dict["ThemeTextPrimaryColor"] = Color.FromArgb(255, 255, 255, 255); // White
        dict["ThemeTextSecondaryColor"] = Color.FromArgb(255, 153, 153, 153); // #999999

        // Brushes
        dict["ThemeAccentBrush"] = new SolidColorBrush((Color)dict["ThemeAccentColor"]);
        dict["ThemeAccentDarkBrush"] = new SolidColorBrush((Color)dict["ThemeAccentDarkColor"]);
        dict["ThemeAccentLightBrush"] = new SolidColorBrush((Color)dict["ThemeAccentLightColor"]);

        // TrainControlPage Specific Colors
        dict["TrainControlHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["TachometerNeedleColor"] = (Color)dict["ThemeAccentColor"];
        dict["TachometerBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["TachometerScaleColor"] = (Color)dict["ThemeAccentLightColor"];
        dict["SpeedDisplayBackgroundColor"] = Color.FromArgb(255, 26, 26, 26);
        dict["SpeedDisplayTextColor"] = (Color)dict["ThemeAccentColor"];
        dict["FunctionButtonActiveColor"] = (Color)dict["ThemeAccentColor"];
        dict["FunctionButtonInactiveColor"] = Color.FromArgb(255, 42, 42, 42);
        dict["FunctionButtonBorderColor"] = Color.FromArgb(255, 68, 68, 68);

        // SignalBoxPage Specific Colors
        dict["ToolboxHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["ToolboxBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["ToolboxItemHoverColor"] = Color.FromArgb(255, 42, 42, 42);
        dict["ToolboxItemActiveColor"] = (Color)dict["ThemeAccentColor"];
        dict["PropertiesPanelBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["ElementSelectionColor"] = (Color)dict["ThemeAccentColor"];
        dict["CanvasBackgroundColor"] = Color.FromArgb(255, 5, 5, 5);
        dict["GridLineColor"] = Color.FromArgb(255, 37, 37, 37);

        // Track Colors
        dict["TrackFreeColor"] = Color.FromArgb(255, 85, 85, 85);
        dict["TrackOccupiedColor"] = Color.FromArgb(255, 255, 51, 51);
        dict["TrackRouteSetColor"] = Color.FromArgb(255, 51, 255, 51);

        // Status Colors
        dict["StatusConnectedColor"] = Color.FromArgb(255, 0, 204, 0);
        dict["StatusDisconnectedColor"] = Color.FromArgb(255, 204, 0, 0);

        return dict;
    }

    /// <summary>
    /// Creates the Red skin ResourceDictionary.
    /// </summary>
    public static ResourceDictionary BuildRedSkin()
    {
        var dict = new ResourceDictionary();

        // Primary Accent Colors - Red
        dict["ThemeAccentColor"] = Color.FromArgb(255, 204, 0, 0); // #CC0000
        dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 153, 0, 0); // #990000
        dict["ThemeAccentLightColor"] = Color.FromArgb(255, 255, 51, 51); // #FF3333

        // Control Backgrounds - Light Grey
        dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 232, 232, 232); // #E8E8E8
        dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 216, 216, 216); // #D8D8D8
        dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 200, 200, 200); // #C8C8C8

        // Text Colors
        dict["ThemeTextPrimaryColor"] = Color.FromArgb(255, 26, 26, 26); // #1A1A1A
        dict["ThemeTextSecondaryColor"] = Color.FromArgb(255, 80, 80, 80); // #505050

        // Brushes
        dict["ThemeAccentBrush"] = new SolidColorBrush((Color)dict["ThemeAccentColor"]);
        dict["ThemeAccentDarkBrush"] = new SolidColorBrush((Color)dict["ThemeAccentDarkColor"]);
        dict["ThemeAccentLightBrush"] = new SolidColorBrush((Color)dict["ThemeAccentLightColor"]);

        // TrainControlPage Specific Colors
        dict["TrainControlHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["TachometerNeedleColor"] = (Color)dict["ThemeAccentColor"];
        dict["TachometerBackgroundColor"] = Color.FromArgb(255, 245, 245, 245);
        dict["TachometerScaleColor"] = Color.FromArgb(255, 64, 64, 64);
        dict["SpeedDisplayBackgroundColor"] = Color.FromArgb(255, 26, 26, 26);
        dict["SpeedDisplayTextColor"] = Color.FromArgb(255, 0, 255, 0); // Green LCD
        dict["FunctionButtonActiveColor"] = (Color)dict["ThemeAccentColor"];
        dict["FunctionButtonInactiveColor"] = Color.FromArgb(255, 208, 208, 208);

        // SignalBoxPage Specific Colors
        dict["ToolboxHeaderColor"] = (Color)dict["ThemeAccentColor"];
        dict["ToolboxBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
        dict["ToolboxItemHoverColor"] = Color.FromArgb(255, 208, 208, 208);
        dict["ToolboxItemActiveColor"] = (Color)dict["ThemeAccentColor"];
        dict["PropertiesPanelBackgroundColor"] = Color.FromArgb(255, 240, 240, 240);
        dict["ElementSelectionColor"] = (Color)dict["ThemeAccentColor"];
        dict["CanvasBackgroundColor"] = Color.FromArgb(255, 42, 42, 42);
        dict["GridLineColor"] = Color.FromArgb(255, 64, 64, 64);

        // Track Colors
        dict["TrackFreeColor"] = Color.FromArgb(255, 128, 128, 128);
        dict["TrackOccupiedColor"] = Color.FromArgb(255, 255, 0, 0);
        dict["TrackRouteSetColor"] = Color.FromArgb(255, 0, 204, 0);

        return dict;
    }

    /// <summary>
    /// Creates the System skin ResourceDictionary (uses Windows accent color).
    /// </summary>
    public static ResourceDictionary BuildSystemSkin()
    {
        var dict = new ResourceDictionary();

        // Primary Accent Colors - Subtle Blue (system default)
        dict["ThemeAccentColor"] = Color.FromArgb(255, 51, 102, 153); // #336699
        dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 32, 64, 102); // #204066
        dict["ThemeAccentLightColor"] = Color.FromArgb(255, 102, 153, 204); // #6699CC

        // Control Backgrounds - Light variant
        dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 248, 248, 248); // #F8F8F8
        dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 240, 240, 240); // #F0F0F0
        dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 224, 224, 224); // #E0E0E0

        // Dark variant colors
        dict["ThemeControlBackgroundColorDark"] = Color.FromArgb(255, 50, 50, 50); // #323232
        dict["ThemeControlBackgroundHoverColorDark"] = Color.FromArgb(255, 68, 68, 68); // #444444
        dict["ThemeControlBackgroundPressedColorDark"] = Color.FromArgb(255, 85, 85, 85); // #555555

        // Text Colors
        dict["ThemeTextPrimaryColor"] = Color.FromArgb(255, 0, 0, 0); // Black
        dict["ThemeTextSecondaryColor"] = Color.FromArgb(255, 102, 102, 102); // #666666
        dict["ThemeTextPrimaryColorDark"] = Color.FromArgb(255, 255, 255, 255); // White
        dict["ThemeTextSecondaryColorDark"] = Color.FromArgb(255, 170, 170, 170); // #AAAAAA

        // Brushes
        dict["ThemeAccentBrush"] = new SolidColorBrush((Color)dict["ThemeAccentColor"]);
        dict["ThemeAccentDarkBrush"] = new SolidColorBrush((Color)dict["ThemeAccentDarkColor"]);
        dict["ThemeAccentLightBrush"] = new SolidColorBrush((Color)dict["ThemeAccentLightColor"]);

        // TrainControlPage Original Colors (Light)
        dict["TrainControlHeaderColor"] = Color.FromArgb(255, 240, 240, 240);
        dict["TrainControlHeaderTextColor"] = Color.FromArgb(255, 0, 0, 0);
        dict["TrainControlPanelBackgroundColor"] = Color.FromArgb(255, 250, 250, 250);
        dict["TachometerBackgroundColor"] = Color.FromArgb(255, 255, 255, 255);
        dict["TachometerNeedleColor"] = Color.FromArgb(255, 51, 102, 153);
        dict["TachometerScaleColor"] = Color.FromArgb(255, 64, 64, 64);
        dict["SpeedDisplayBackgroundColor"] = Color.FromArgb(255, 40, 40, 40);
        dict["SpeedDisplayTextColor"] = Color.FromArgb(255, 200, 200, 200);
        dict["FunctionButtonBackgroundColor"] = Color.FromArgb(255, 224, 224, 224);
        dict["FunctionButtonActiveColor"] = Color.FromArgb(255, 102, 153, 204);
        dict["FunctionButtonBorderColor"] = Color.FromArgb(255, 170, 170, 170);

        // SignalBoxPage Original Colors (Light)
        dict["ToolboxHeaderColor"] = Color.FromArgb(255, 240, 240, 240);
        dict["ToolboxHeaderTextColor"] = Color.FromArgb(255, 0, 0, 0);
        dict["ToolboxBackgroundColor"] = Color.FromArgb(255, 250, 250, 250);
        dict["ToolboxItemHoverColor"] = Color.FromArgb(255, 235, 235, 235);
        dict["ToolboxItemActiveColor"] = Color.FromArgb(255, 102, 153, 204);
        dict["PropertiesPanelBackgroundColor"] = Color.FromArgb(255, 248, 248, 248);
        dict["ElementSelectionColor"] = Color.FromArgb(255, 51, 102, 153);
        dict["CanvasBackgroundColor"] = Color.FromArgb(255, 255, 255, 255);
        dict["GridLineColor"] = Color.FromArgb(255, 220, 220, 220);

        // Track Colors
        dict["TrackFreeColor"] = Color.FromArgb(255, 150, 150, 150);
        dict["TrackOccupiedColor"] = Color.FromArgb(255, 204, 0, 0);
        dict["TrackRouteSetColor"] = Color.FromArgb(255, 0, 153, 0);

        return dict;
    }
}
