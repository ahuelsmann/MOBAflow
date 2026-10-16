// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

/// <summary>
/// File picking for hosts without file dialogs; every picker behaves as if the user cancelled.
/// </summary>
public sealed class NullFilePickerService : IFilePickerService
{
    private static readonly Task<string?> Cancelled = Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<string?> BrowseForRecordingFileAsync() => Cancelled;

    /// <inheritdoc />
    public Task<string?> SaveRecordingFileAsync(string suggestedFileName) => Cancelled;

    /// <inheritdoc />
    public Task<string?> BrowseForJsonFileAsync() => Cancelled;

    /// <inheritdoc />
    public Task<string?> SaveJsonFileAsync(string suggestedFileName) => Cancelled;

    /// <inheritdoc />
    public Task<string?> SaveHtmlFileAsync(string suggestedFileName) => Cancelled;

    /// <inheritdoc />
    public Task<string?> BrowseForXmlFileAsync() => Cancelled;

    /// <inheritdoc />
    public Task<string?> SaveXmlFileAsync(string suggestedFileName) => Cancelled;

    /// <inheritdoc />
    public Task<string?> BrowseForAudioFileAsync() => Cancelled;

    /// <inheritdoc />
    public Task<string?> BrowseForPhotoAsync() => Cancelled;
}
