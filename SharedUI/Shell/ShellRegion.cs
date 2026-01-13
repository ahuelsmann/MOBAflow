// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.SharedUI.Shell;

/// <summary>
/// Defines regions where content can be displayed in the application shell.
/// </summary>
public enum ShellRegion
{
    /// <summary>
    /// The main content area (NavigationView content).
    /// </summary>
    Main,

    /// <summary>
    /// Overlay region for modal dialogs and flyouts.
    /// </summary>
    Overlay,

    /// <summary>
    /// Sidebar region for dockable tool panels.
    /// </summary>
    Sidebar,

    /// <summary>
    /// Footer region for status bar and notifications.
    /// </summary>
    Footer
}
