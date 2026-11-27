// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBAflow.View;

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// A focusable <see cref="Canvas"/> that exposes a custom landmark UI Automation peer so screen readers
/// such as Narrator can discover the canvas as a named UIA landmark and announce its accessible name on focus.
/// </summary>
internal sealed partial class AccessibleCanvas : Canvas
{
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AccessibleCanvasAutomationPeer(this);
    }

    internal sealed partial class AccessibleCanvasAutomationPeer(AccessibleCanvas owner)
        : FrameworkElementAutomationPeer(owner)
    {
        internal const AutomationControlType ExpectedControlType = AutomationControlType.Group;
        internal const AutomationLandmarkType ExpectedLandmarkType = AutomationLandmarkType.Custom;
        internal const string DefaultLocalizedControlType = "track plan canvas";
        internal const string DefaultLocalizedLandmarkType = "Track plan";

        internal static string ResolveAutomationName(string? attachedName, string? fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(attachedName))
            {
                return attachedName.Trim();
            }

            return string.IsNullOrWhiteSpace(fallbackName) ? "Track plan canvas" : fallbackName.Trim();
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return ExpectedControlType;
        }

        /// <inheritdoc />
        protected override AutomationLandmarkType GetLandmarkTypeCore()
        {
            return ExpectedLandmarkType;
        }

        /// <inheritdoc />
        protected override string GetLocalizedControlTypeCore()
        {
            return DefaultLocalizedControlType;
        }

        /// <inheritdoc />
        protected override string GetLocalizedLandmarkTypeCore()
        {
            return DefaultLocalizedLandmarkType;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var attached = AutomationProperties.GetName(owner);
            return ResolveAutomationName(attached, base.GetNameCore());
        }

        /// <inheritdoc />
        protected override bool IsControlElementCore()
        {
            return true;
        }

        /// <inheritdoc />
        protected override bool IsContentElementCore()
        {
            return true;
        }
    }
}
