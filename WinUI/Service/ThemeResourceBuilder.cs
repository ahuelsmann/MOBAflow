// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Programmatic theme resource builder for WinUI 3.
/// Avoids XAML parsing issues by building ResourceDictionaries in C#.
/// </summary>
public static class ThemeResourceBuilder
{
    /// <summary>
    /// Creates the Modern theme ResourceDictionary (Blue accents).
    /// </summary>
    public static ResourceDictionary BuildModernTheme()
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
    /// Creates the Classic theme ResourceDictionary (Green accents, Märklin-inspired).
    /// </summary>
    public static ResourceDictionary BuildClassicTheme()
    {
        var dict = new ResourceDictionary();

        // Primary Accent Colors - Märklin Green
        dict["ThemeAccentColor"] = Color.FromArgb(255, 42, 164, 55); // #2AA437
        dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 30, 125, 45); // #1E7D2D
        dict["ThemeAccentLightColor"] = Color.FromArgb(255, 94, 200, 103); // #5EC867

        // Control Backgrounds - Silver/Gray (Professional)
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
        /// Creates the Dark theme ResourceDictionary (Purple/Violet accents, night-friendly).
        /// </summary>
        public static ResourceDictionary BuildDarkTheme()
        {
            var dict = new ResourceDictionary();

            // Primary Accent Colors - Violet/Purple
            dict["ThemeAccentColor"] = Color.FromArgb(255, 155, 95, 255); // #9B5FFF
            dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 122, 71, 212); // #7A47D4
            dict["ThemeAccentLightColor"] = Color.FromArgb(255, 185, 141, 255); // #B98DFF

            // Control Backgrounds - Dark Gray/Blue
            dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 45, 45, 48); // #2D2D30
            dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 62, 62, 66); // #3E3E42
            dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 74, 74, 80); // #4A4A50

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
        /// Creates the ESU CabControl theme ResourceDictionary (Orange/Amber accents, dark).
        /// Inspired by ESU CabControl DCC System.
        /// </summary>
        public static ResourceDictionary BuildEsuCabControlTheme()
        {
            var dict = new ResourceDictionary();

            // Primary Accent Colors - ESU Orange/Amber
            dict["ThemeAccentColor"] = Color.FromArgb(255, 255, 140, 0); // #FF8C00
            dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 204, 112, 0); // #CC7000
            dict["ThemeAccentLightColor"] = Color.FromArgb(255, 255, 179, 71); // #FFB347

            // Control Backgrounds - Dark ESU Style
            dict["ThemeControlBackgroundColor"] = Color.FromArgb(255, 26, 26, 26); // #1A1A1A
            dict["ThemeControlBackgroundHoverColor"] = Color.FromArgb(255, 42, 42, 42); // #2A2A2A
            dict["ThemeControlBackgroundPressedColor"] = Color.FromArgb(255, 58, 58, 58); // #3A3A3A

            // Text Colors
            dict["ThemeTextPrimaryColor"] = Color.FromArgb(255, 255, 255, 255); // White
            dict["ThemeTextSecondaryColor"] = Color.FromArgb(255, 176, 176, 176); // #B0B0B0

            // Brushes
            dict["ThemeAccentBrush"] = new SolidColorBrush((Color)dict["ThemeAccentColor"]);
            dict["ThemeAccentDarkBrush"] = new SolidColorBrush((Color)dict["ThemeAccentDarkColor"]);
            dict["ThemeAccentLightBrush"] = new SolidColorBrush((Color)dict["ThemeAccentLightColor"]);

            // TrainControlPage Specific Colors
            dict["TrainControlHeaderColor"] = (Color)dict["ThemeAccentColor"];
            dict["TachometerNeedleColor"] = (Color)dict["ThemeAccentColor"];
            dict["TachometerBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
            dict["TachometerScaleColor"] = (Color)dict["ThemeAccentLightColor"];
            dict["SpeedDisplayBackgroundColor"] = Color.FromArgb(255, 42, 42, 42);
            dict["SpeedDisplayTextColor"] = (Color)dict["ThemeAccentColor"];
            dict["FunctionButtonActiveColor"] = (Color)dict["ThemeAccentColor"];
            dict["FunctionButtonInactiveColor"] = Color.FromArgb(255, 58, 58, 58);

            // SignalBoxPage Specific Colors
            dict["ToolboxHeaderColor"] = (Color)dict["ThemeAccentColor"];
            dict["ToolboxBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
            dict["ToolboxItemHoverColor"] = (Color)dict["ThemeControlBackgroundHoverColor"];
            dict["ToolboxItemActiveColor"] = (Color)dict["ThemeAccentColor"];
            dict["PropertiesPanelBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
            dict["ElementSelectionColor"] = (Color)dict["ThemeAccentColor"];
            dict["CanvasBackgroundColor"] = Color.FromArgb(255, 13, 13, 13);
            dict["GridLineColor"] = Color.FromArgb(255, 51, 51, 51);

            // Track Colors
            dict["TrackFreeColor"] = Color.FromArgb(255, 102, 102, 102);
            dict["TrackOccupiedColor"] = Color.FromArgb(255, 255, 68, 68);
            dict["TrackRouteSetColor"] = Color.FromArgb(255, 68, 255, 68);

            return dict;
        }

        /// <summary>
        /// Creates the Roco Z21 theme ResourceDictionary (Orange accents, black background).
        /// Inspired by Roco Z21 App Interface.
        /// </summary>
        public static ResourceDictionary BuildRocoZ21Theme()
        {
            var dict = new ResourceDictionary();

            // Primary Accent Colors - Z21 Orange
            dict["ThemeAccentColor"] = Color.FromArgb(255, 255, 102, 0); // #FF6600
            dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 204, 82, 0); // #CC5200
            dict["ThemeAccentLightColor"] = Color.FromArgb(255, 255, 153, 51); // #FF9933

            // Control Backgrounds - Z21 Black Style
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

            // Track Colors (Z21 Style)
            dict["TrackFreeColor"] = Color.FromArgb(255, 85, 85, 85);
            dict["TrackOccupiedColor"] = Color.FromArgb(255, 255, 51, 51);
            dict["TrackRouteSetColor"] = Color.FromArgb(255, 51, 255, 51);

            // Status Colors
            dict["StatusConnectedColor"] = Color.FromArgb(255, 0, 204, 0);
            dict["StatusDisconnectedColor"] = Color.FromArgb(255, 204, 0, 0);

            return dict;
        }

        /// <summary>
        /// Creates the Maerklin CS theme ResourceDictionary (Red accents, grey background).
        /// Inspired by Maerklin Central Station 2/3.
        /// </summary>
        public static ResourceDictionary BuildMaerklinCSTheme()
        {
            var dict = new ResourceDictionary();

            // Primary Accent Colors - Maerklin Red
            dict["ThemeAccentColor"] = Color.FromArgb(255, 204, 0, 0); // #CC0000
            dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 153, 0, 0); // #990000
            dict["ThemeAccentLightColor"] = Color.FromArgb(255, 255, 51, 51); // #FF3333

            // Control Backgrounds - Light Grey CS Style
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

            // SignalBoxPage Specific Colors (CS2 Stellwerk Style)
            dict["ToolboxHeaderColor"] = (Color)dict["ThemeAccentColor"];
            dict["ToolboxBackgroundColor"] = (Color)dict["ThemeControlBackgroundColor"];
            dict["ToolboxItemHoverColor"] = Color.FromArgb(255, 208, 208, 208);
            dict["ToolboxItemActiveColor"] = (Color)dict["ThemeAccentColor"];
            dict["PropertiesPanelBackgroundColor"] = Color.FromArgb(255, 240, 240, 240);
            dict["ElementSelectionColor"] = (Color)dict["ThemeAccentColor"];
            dict["CanvasBackgroundColor"] = Color.FromArgb(255, 42, 42, 42);
            dict["GridLineColor"] = Color.FromArgb(255, 64, 64, 64);

            // Track Colors (CS2 Style - Green for routes)
            dict["TrackFreeColor"] = Color.FromArgb(255, 128, 128, 128);
            dict["TrackOccupiedColor"] = Color.FromArgb(255, 255, 0, 0);
            dict["TrackRouteSetColor"] = Color.FromArgb(255, 0, 204, 0);

            return dict;
        }

        /// <summary>
        /// Creates the Original theme ResourceDictionary (Light and Dark variants).
        /// This is the default MOBAflow theme, extracted from original TrainControlPage and SignalBoxPage.
        /// Used as a skin option in Page2 variants for backward compatibility.
        /// </summary>
        public static ResourceDictionary BuildOriginalTheme()
        {
            var dict = new ResourceDictionary();

            // Primary Accent Colors - Subtle Blue (from original MOBAflow)
            dict["ThemeAccentColor"] = Color.FromArgb(255, 51, 102, 153); // #336699
            dict["ThemeAccentDarkColor"] = Color.FromArgb(255, 32, 64, 102); // #204066
            dict["ThemeAccentLightColor"] = Color.FromArgb(255, 102, 153, 204); // #6699CC

            // Control Backgrounds - Light variant (original default)
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

            // Dark variant for TrainControlPage
            dict["TrainControlHeaderColorDark"] = Color.FromArgb(255, 50, 50, 50);
            dict["TrainControlHeaderTextColorDark"] = Color.FromArgb(255, 255, 255, 255);
            dict["TrainControlPanelBackgroundColorDark"] = Color.FromArgb(255, 40, 40, 40);
            dict["TachometerBackgroundColorDark"] = Color.FromArgb(255, 20, 20, 20);
            dict["TachometerNeedleColorDark"] = Color.FromArgb(255, 102, 153, 204);

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

            // Dark variant for SignalBoxPage
            dict["ToolboxHeaderColorDark"] = Color.FromArgb(255, 50, 50, 50);
            dict["ToolboxHeaderTextColorDark"] = Color.FromArgb(255, 255, 255, 255);
            dict["ToolboxBackgroundColorDark"] = Color.FromArgb(255, 40, 40, 40);
            dict["ToolboxItemHoverColorDark"] = Color.FromArgb(255, 60, 60, 60);
            dict["CanvasBackgroundColorDark"] = Color.FromArgb(255, 20, 20, 20);
            dict["GridLineColorDark"] = Color.FromArgb(255, 50, 50, 50);

            // Track Colors (Original MOBAflow)
            dict["TrackFreeColor"] = Color.FromArgb(255, 150, 150, 150);
            dict["TrackOccupiedColor"] = Color.FromArgb(255, 204, 0, 0);
            dict["TrackRouteSetColor"] = Color.FromArgb(255, 0, 153, 0);

            return dict;
        }
    }
