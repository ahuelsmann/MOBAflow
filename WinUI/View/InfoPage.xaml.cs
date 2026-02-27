namespace Moba.WinUI.View;

using Common.Navigation;

using System.IO;
using System.Reflection;

[NavigationItem(
    Tag = "info",
    Title = "Info",
    Icon = "\uE946",
    Category = NavigationCategory.Help,
    Order = 20)]
internal sealed partial class InfoPage
{
    private const string ReadmeFileName = "README.md";

    public InfoPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadReadme();
    }

    private void LoadReadme()
    {
        var content = LoadReadmeContent();
        ReadmeMarkdownBlock.Text = content;
    }

    private static string LoadReadmeContent()
    {
        // Zuerst neben der ausfuehrenden Datei suchen (z.B. bin/Debug/)
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        var readmePath = Path.Combine(baseDir, ReadmeFileName);

        if (File.Exists(readmePath))
        {
            return File.ReadAllText(readmePath);
        }

        // Fallback: Embedded Resource
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.{ReadmeFileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return GetFallbackContent();
    }

    private static string GetFallbackContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var versionString = !string.IsNullOrWhiteSpace(infoVersion)
            ? infoVersion.Split('+')[0]
            : $"{assembly.GetName().Version?.Major ?? 0}.{assembly.GetName().Version?.Minor ?? 0}";

        return $"""
            # MOBAflow

            Version {versionString}

            Die README.md konnte nicht geladen werden. Bitte stellen Sie sicher, dass die Datei
            im Anwendungsverzeichnis vorhanden ist, oder installieren Sie MOBAflow neu.

            Dokumentation: [GitHub](https://github.com/ahuelsmann/MOBAflow)
            """;
    }
}
