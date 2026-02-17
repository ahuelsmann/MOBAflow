// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Dialog zur Auswahl eines Segoe-MDL2-Symbols für eine Funktionsschaltfläche (Train Control).
/// </summary>
public sealed partial class FunctionSymbolPickerDialog : ContentDialog
{
    /// <summary>
    /// Nach Schließen: gewähltes Symbol (Unicode-Glyph-String) oder null bei Abbrechen.
    /// </summary>
    public string? SelectedGlyph { get; private set; }

    /// <summary>
    /// Repräsentative Auswahl an Segoe MDL2 Assets Glyphen (F0–F20-relevant und allgemein nützlich).
    /// </summary>
    private static readonly string[] GlyphList =
    {
        "\uE7B7", "\uE767", "\uE7C0", "\uE8BB", "\uE8BA", "\uE754", "\uEB50", "\uE753",
        "\uE74A", "\uE71B", "\uE713", "\uEC4F", "\uE720", "\uE90F", "\uE7C1", "\uE9D9",
        "\uE945", "\uE823", "\uE734", "\uE790", "\uE711", "\uE7C3", "\uE8A5", "\uE8F1",
        "\uE710", "\uE711", "\uE712", "\uE714", "\uE715", "\uE716", "\uE717", "\uE718",
        "\uE719", "\uE71A", "\uE71E", "\uE721", "\uE722", "\uE723", "\uE734", "\uE735",
        "\uE736", "\uE737", "\uE738", "\uE739", "\uE73A", "\uE73B", "\uE73C", "\uE73D",
        "\uE73E", "\uE73F", "\uE740", "\uE741", "\uE742", "\uE743", "\uE744", "\uE745",
        "\uE746", "\uE747", "\uE748", "\uE749", "\uE74B", "\uE74C", "\uE74D", "\uE74E",
        "\uE750", "\uE751", "\uE752", "\uE755", "\uE756", "\uE757", "\uE758", "\uE759",
        "\uE75A", "\uE75B", "\uE75C", "\uE75D", "\uE75E", "\uE75F", "\uE760", "\uE761",
        "\uE762", "\uE763", "\uE764", "\uE765", "\uE766", "\uE768", "\uE769", "\uE76B",
        "\uE76C", "\uE76D", "\uE76E", "\uE76F", "\uE770", "\uE771", "\uE772", "\uE773",
        "\uE774", "\uE775", "\uE776", "\uE777", "\uE778", "\uE779", "\uE77A", "\uE77B",
        "\uE77C", "\uE77D", "\uE77E", "\uE77F", "\uE780", "\uE781", "\uE782", "\uE783",
        "\uE784", "\uE785", "\uE786", "\uE787", "\uE788", "\uE789", "\uE78A", "\uE78B",
        "\uE78C", "\uE78D", "\uE78E", "\uE78F", "\uE790", "\uE791", "\uE792", "\uE793",
        "\uE794", "\uE795", "\uE796", "\uE797", "\uE798", "\uE799", "\uE79A", "\uE79B",
        "\uE79C", "\uE79D", "\uE79E", "\uE79F", "\uE7A0", "\uE7A1", "\uE7A2", "\uE7A3",
        "\uE7A4", "\uE7A5", "\uE7A6", "\uE7A7", "\uE7A8", "\uE7A9", "\uE7AA", "\uE7AB",
        "\uE7AC", "\uE7AD", "\uE7AE", "\uE7AF", "\uE7B0", "\uE7B1", "\uE7B2", "\uE7B3",
        "\uE7B4", "\uE7B5", "\uE7B6", "\uE7B8", "\uE7B9", "\uE7BA", "\uE7BB", "\uE7BC",
        "\uE7BE", "\uE7BF", "\uE7C2", "\uE7C3", "\uE7C4", "\uE7C5", "\uE8A6", "\uE8A7",
        "\uE8A8", "\uE8A9", "\uE8B0", "\uE8B1", "\uE8B2", "\uE8B3", "\uE8B4", "\uE8B5",
        "\uE8B6", "\uE8B7", "\uE8B8", "\uE8B9", "\uE8BA", "\uE8BB", "\uE8BC", "\uE8BD",
        "\uE8BE", "\uE8BF", "\uE8EB", "\uE8EC", "\uE8ED", "\uE8EE", "\uE8EF", "\uE90F",
        "\uE910", "\uE911", "\uE912", "\uE913", "\uE914", "\uE915", "\uE916", "\uE917",
        "\uE918", "\uE919", "\uE91A", "\uE91B", "\uE91C", "\uE91D", "\uE91E", "\uE91F",
        "\uE920", "\uE921", "\uE922", "\uE923", "\uE924", "\uE925", "\uE926", "\uE927",
        "\uE928", "\uE929", "\uE92A", "\uE92B", "\uE92C", "\uE92D", "\uE92E", "\uE92F",
        "\uE930", "\uE931", "\uE932", "\uE933", "\uE934", "\uE935", "\uE936", "\uE937",
        "\uE938", "\uE939", "\uE93A", "\uE93B", "\uE93C", "\uE93D", "\uE93E", "\uE93F",
        "\uE940", "\uE941", "\uE942", "\uE943", "\uE944", "\uE946", "\uE947", "\uE948",
        "\uE949", "\uE94A", "\uE94B", "\uE94C", "\uE94D", "\uE94E", "\uE94F", "\uEC49",
        "\uEC4A", "\uEC4B", "\uEC4C", "\uEC4D", "\uEC4E", "\uEC50", "\uEB50", "\uEB51",
        "\uEB52", "\uEB53", "\uEB54", "\uEB55", "\uEB56", "\uEB57", "\uEB58", "\uEB59",
        "\uEB5A", "\uEB5B", "\uEB5C", "\uEB5D", "\uEB5E", "\uEB5F", "\uE9D5", "\uE9D6",
        "\uE9D7", "\uE9D8", "\uE9DA", "\uE9DB", "\uE9DC", "\uE9DD", "\uE9DE", "\uE9DF",
        "\uE9E0", "\uE9E1", "\uE9E2", "\uE9E3", "\uE9E4", "\uE9E5", "\uE9E6", "\uE9E7",
        "\uE9E8", "\uE9E9", "\uE9EA", "\uE9EB", "\uE9EC", "\uE9ED", "\uE9EE", "\uE9EF",
        "\uE9F0", "\uE9F1", "\uE9F2", "\uE9F3", "\uE9F4", "\uE9F5", "\uE9F6", "\uE9F7",
        "\uE9F8", "\uE9F9", "\uE9FA", "\uE9FB", "\uE9FC", "\uE9FD", "\uE9FE", "\uE9FF",
        "\uEA00", "\uEA01", "\uEA02", "\uEA03", "\uEA04", "\uEA05", "\uEA06", "\uEA07",
        "\uEA08", "\uEA09", "\uEA0A", "\uEA0B", "\uEA0C", "\uEA0D", "\uEA0E", "\uEA0F",
        "\uEA10", "\uEA11", "\uEA12", "\uEA13", "\uEA14", "\uEA15", "\uEA16", "\uEA17",
        "\uEA18", "\uEA19", "\uEA1A", "\uEA1B", "\uEA1C", "\uEA1D", "\uEA1E", "\uEA1F",
        "\uEA20", "\uEA21", "\uEA22", "\uEA23", "\uEA24", "\uEA25", "\uEA26", "\uEA27",
        "\uEA28", "\uEA29", "\uEA2A", "\uEA2B", "\uEA2C", "\uEA2D", "\uEA2E", "\uEA2F",
        "\uEA30", "\uEA31", "\uEA32", "\uEA33", "\uEA34", "\uEA35", "\uEA36", "\uEA37",
        "\uEA38", "\uEA39", "\uEA3A", "\uEA3B", "\uEA3C", "\uEA3D", "\uEA3E", "\uEA3F",
        "\uEA40", "\uEA41", "\uEA42", "\uEA43", "\uEA44", "\uEA45", "\uEA46", "\uEA47",
        "\uEA48", "\uEA49", "\uEA4A", "\uEA4B", "\uEA4C", "\uEA4D", "\uEA4E", "\uEA4F",
        "\uEA50", "\uEA51", "\uEA52", "\uEA53", "\uEA54", "\uEA55", "\uEA56", "\uEA57",
        "\uEA58", "\uEA59", "\uEA5A", "\uEA5B", "\uEA5C", "\uEA5D", "\uEA5E", "\uEA5F",
    };

    public FunctionSymbolPickerDialog()
    {
        InitializeComponent();
        SymbolsItemsControl.ItemsSource = GlyphList;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedGlyph = null;
    }

    private void SymbolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string glyph)
        {
            SelectedGlyph = glyph;
            Hide();
        }
    }
}
