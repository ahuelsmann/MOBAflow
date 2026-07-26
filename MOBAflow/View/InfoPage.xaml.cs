// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using System.Reflection;

internal sealed partial class InfoPage
{
    public InfoPage()
    {
        InitializeComponent();
    }

    public string VersionText { get; } = CreateVersionText();

    private static string CreateVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var version = !string.IsNullOrWhiteSpace(infoVersion)
            ? infoVersion.Split('+')[0]
            : $"{assembly.GetName().Version?.Major ?? 0}.{assembly.GetName().Version?.Minor ?? 0}";

        return $"Version {version}";
    }
}
