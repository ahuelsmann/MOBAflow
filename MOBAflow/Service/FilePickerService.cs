// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Windows.Storage.Pickers;

using SharedUI.Interface;

/// <summary>
/// WinUI open and save pickers for the main window.
/// </summary>
internal sealed class FilePickerService(WinUiWindowContext window) : IFilePickerService
{
    private const string JsonExtension = ".json";

    private readonly WinUiWindowContext _window = window ?? throw new ArgumentNullException(nameof(window));

    /// <summary>
    /// Opens a file picker to browse for a JSON file.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForJsonFileAsync()
    {
        var picker = new FileOpenPicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaJsonPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Select JSON File",
            FileTypeChoices = { { "JSON Files (*.json)", new List<string> { JsonExtension } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <inheritdoc />
    public async Task<string?> BrowseForRecordingFileAsync()
    {
        var picker = new FileOpenPicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaRecordingPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Open MOBAflow Recording",
            FileTypeChoices = { { "MOBAflow Recording (*.mobarecording.json)", new List<string> { JsonExtension } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <inheritdoc />
    public async Task<string?> SaveRecordingFileAsync(string suggestedFileName)
    {
        var picker = new FileSavePicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaRecordingSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"{suggestedFileName}.mobarecording",
            DefaultFileExtension = JsonExtension,
            ShowOverwritePrompt = true,
            Title = "Save MOBAflow Recording",
            FileTypeChoices = { { "MOBAflow Recording (*.mobarecording.json)", new List<string> { JsonExtension } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file save picker for saving a JSON file.
    /// </summary>
    public async Task<string?> SaveJsonFileAsync(string suggestedFileName)
    {
        var picker = new FileSavePicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaJsonSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = JsonExtension,
            ShowOverwritePrompt = true,
            Title = "Save JSON File",
            FileTypeChoices = { { "JSON Files (*.json)", new List<string> { JsonExtension } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    public async Task<string?> SaveHtmlFileAsync(string suggestedFileName)
    {
        var picker = new FileSavePicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaHtmlSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = ".html",
            ShowOverwritePrompt = true,
            Title = "Save Locomotive Passport",
            FileTypeChoices = { { "HTML Documents (*.html)", new List<string> { ".html" } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file picker to browse for an XML file (e.g., AnyRail layout).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForXmlFileAsync()
    {
        var picker = new FileOpenPicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaXmlPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Import AnyRail Layout",
            FileTypeChoices = { { "AnyRail Layout (*.xml)", new List<string> { ".xml" } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file save picker for saving an XML file.
    /// </summary>
    public async Task<string?> SaveXmlFileAsync(string suggestedFileName)
    {
        var picker = new FileSavePicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaXmlSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = ".xml",
            ShowOverwritePrompt = true,
            Title = "Save XML File",
            FileTypeChoices = { { "XML Files (*.xml)", new List<string> { ".xml" } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file picker to browse for an audio file (WAV, MP3, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForAudioFileAsync()
    {
        var picker = new FileOpenPicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaAudioPicker",
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            Title = "Select Audio File",
            FileTypeChoices = { { "Audio Files", new List<string> { ".wav", ".mp3", ".ogg", ".flac", ".m4a" } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file picker to browse for a photo/image file (JPG, PNG, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForPhotoAsync()
    {
        var picker = new FileOpenPicker(_window.RequireWindowId())
        {
            SettingsIdentifier = "MobaPhotoPicker",
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            Title = "Select Photo",
            FileTypeChoices = { { "Image Files", new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif" } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }
}
