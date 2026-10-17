// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>Opens platform file pickers for open and save dialogs.</summary>
public interface IFilePickerService
{
    /// <summary>Opens a file picker for a MOBAflow recording artifact.</summary>
    /// <returns>The selected recording path, or <see langword="null"/> when cancelled.</returns>
    Task<string?> BrowseForRecordingFileAsync();

    /// <summary>Opens a file save picker for a MOBAflow recording artifact.</summary>
    /// <param name="suggestedFileName">Suggested base file name without the recording suffix.</param>
    /// <returns>The selected recording path, or <see langword="null"/> when cancelled.</returns>
    Task<string?> SaveRecordingFileAsync(string suggestedFileName);

    /// <summary>
    /// Opens a file picker to browse for a JSON file.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForJsonFileAsync();

    /// <summary>
    /// Opens a file save picker for saving a JSON file.
    /// </summary>
    /// <param name="suggestedFileName">Suggested file name</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> SaveJsonFileAsync(string suggestedFileName);

    /// <summary>Opens a file save picker for a printable HTML document.</summary>
    Task<string?> SaveHtmlFileAsync(string suggestedFileName);

    /// <summary>
    /// Opens a file picker to browse for an XML file (e.g., AnyRail layout).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForXmlFileAsync();

    /// <summary>
    /// Opens a file save picker for saving an XML file.
    /// </summary>
    /// <param name="suggestedFileName">Suggested file name</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> SaveXmlFileAsync(string suggestedFileName);

    /// <summary>
    /// Opens a file picker to browse for an audio file (WAV, MP3, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForAudioFileAsync();

    /// <summary>
    /// Opens a file picker to browse for a photo/image file (JPG, PNG, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForPhotoAsync();
}
