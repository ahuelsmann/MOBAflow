// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Data;

using Domain;

using System.Text.Json;

/// <summary>
/// Zentrale Stammdaten-Klasse für Städte/Bahnhöfe und Lokomotiv-Bibliothek.
/// Lädt und speichert analog zur Solution-Klasse aus einer gemeinsamen JSON-Datei (z. B. data.json).
/// </summary>
public class DataManager
{
    /// <summary>
    /// Aktuelle Schema-Version für das Stammdaten-JSON-Format.
    /// Erhöhen bei Breaking Changes am Schema.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    public DataManager()
    {
        Cities = [];
        Locomotives = [];
        ViessmannMultiplexSignals = [];
        SchemaVersion = CurrentSchemaVersion;
    }

    /// <summary>
    /// Schema-Version dieser Stammdaten-Datei (zur Erkennung inkompatibler Formate).
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Liste der Städte mit ihren Bahnhöfen (aus der gemeinsamen Stammdaten-Datei).
    /// </summary>
    public List<City> Cities { get; set; }

    /// <summary>
    /// Liste der Lokomotiv-Kategorien mit ihren Baureihen (aus der gemeinsamen Stammdaten-Datei).
    /// </summary>
    public List<LocomotiveCategory> Locomotives { get; set; }

    /// <summary>
    /// Viessmann Multiplex-Signale (Ks-Hauptsignal, Ks-Vorsignal) für die ComboBox-Auswahl im Stellwerk.
    /// Quelle: https://viessmann-modell.com/sortiment/spur-h0/signale/
    /// </summary>
    public List<ViessmannMultiplexSignalEntry> ViessmannMultiplexSignals { get; set; }

    /// <summary>
    /// Aktualisiert diese Instanz aus einer anderen DataManager-Instanz.
    /// Behält die gleiche Objektreferenz und ersetzt die Daten.
    /// </summary>
    public void UpdateFrom(DataManager other)
    {
        ArgumentNullException.ThrowIfNull(other);
        SchemaVersion = other.SchemaVersion;
        Cities.Clear();
        foreach (var c in other.Cities)
            Cities.Add(c);
        Locomotives.Clear();
        foreach (var l in other.Locomotives)
            Locomotives.Add(l);
        ViessmannMultiplexSignals.Clear();
        foreach (var s in other.ViessmannMultiplexSignals)
            ViessmannMultiplexSignals.Add(s);
    }

    /// <summary>
    /// Lädt Stammdaten aus einer JSON-Datei und wendet sie auf diese Instanz an.
    /// Analog zu Solution.LoadAsync. Bei fehlender Datei werden die Listen geleert (kein Wurf).
    /// </summary>
    public async Task LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required", nameof(filePath));

        if (!File.Exists(filePath))
        {
            Cities.Clear();
            Locomotives.Clear();
            ViessmannMultiplexSignals.Clear();
            return;
        }

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            Cities.Clear();
            Locomotives.Clear();
            ViessmannMultiplexSignals.Clear();
            return;
        }
        var loaded = JsonSerializer.Deserialize<DataManager>(json, JsonOptions.Default)
            ?? throw new InvalidOperationException("Failed to deserialize master data file");
        UpdateFrom(loaded);
    }

    /// <summary>
    /// Speichert die aktuellen Stammdaten in eine JSON-Datei.
    /// Analog zu Solution-Speicherung.
    /// </summary>
    public async Task SaveAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required", nameof(filePath));

        var json = JsonSerializer.Serialize(this, JsonOptions.Default);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Lädt Stammdaten aus einer Datei (statisch, für Tests oder einmaliges Laden).
    /// </summary>
    /// <param name="path">Vollständiger Pfad inkl. Dateiname.</param>
    /// <returns>Geladene Instanz oder null bei Fehler.</returns>
    public static async Task<DataManager?> LoadFromFileAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var temp = JsonSerializer.Deserialize<DataManager>(json, JsonOptions.Default);
                    return temp;
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Lädt nur die Lokomotiv-Bibliothek aus einer separaten JSON-Datei (Legacy; Standard ist data.json mit Cities + Locomotives).
    /// </summary>
    /// <param name="path">Vollständiger Pfad inkl. Dateiname.</param>
    /// <returns>Liste der Lokomotiv-Kategorien oder leere Liste bei Fehler.</returns>
    public static async Task<List<LocomotiveCategory>> LoadLocomotivesAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<LocomotiveLibraryData>(json, JsonOptions.Default);
                    return data?.Locomotives ?? [];
                }
                catch (JsonException)
                {
                    return [];
                }
            }
        }
        return [];
    }

    /// <summary>
    /// Flattens locomotive categories into a single ordered list of series.
    /// </summary>
    public static List<LocomotiveSeries> FlattenLocomotiveSeries(List<LocomotiveCategory> categories)
    {
        return categories
            .SelectMany(cat => cat.Series)
            .OrderBy(s => s.Name)
            .ToList();
    }
}

/// <summary>
/// Ein Eintrag für ein Viessmann Multiplex-Signal (Haupt- oder Vorsignal).
/// </summary>
public abstract class ViessmannMultiplexSignalEntry
{
    /// <summary>Viessmann Artikelnummer (z. B. "4040", "4046").</summary>
    public string ArticleNumber { get; set; } = string.Empty;

    /// <summary>Anzeigename für die ComboBox (z. B. "Ks-Vorsignal", "Ks-Mehrabschnittssignal").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Rolle: "main" = Hauptsignal, "distant" = Vorsignal.</summary>
    public string Role { get; set; } = "main";
}

/// <summary>
/// Helper class for deserializing locomotive library JSON files.
/// </summary>
internal class LocomotiveLibraryData
{
    public List<LocomotiveCategory> Locomotives { get; set; } = [];
}
