// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Localization;

using SharedUI.Localization;
using Microsoft.Maui.Controls.Xaml;
using System.Globalization;

/// <summary>Resolves shared catalogue text at view construction in the startup UI culture.</summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class LocalizeExtension : IMarkupExtension<string>
{
    /// <summary>A stable semantic key in the shared catalogue.</summary>
    public string Key { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ProvideValue(IServiceProvider serviceProvider) => new UiLocalizer(CultureInfo.CurrentUICulture).Get(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
