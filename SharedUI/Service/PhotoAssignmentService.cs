// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

/// <summary>
/// The photo steps of the vehicle pages: pick an image, store it below the photo root and resolve a stored photo.
/// The steps stay separate so a page can check that its selection is still current between picking and storing.
/// </summary>
public sealed class PhotoAssignmentService
{
    private readonly IFilePickerService _filePicker;
    private readonly IPhotoStorageService _photoStorage;

    public PhotoAssignmentService(IFilePickerService filePicker, IPhotoStorageService photoStorage)
    {
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(photoStorage);
        _filePicker = filePicker;
        _photoStorage = photoStorage;
    }

    /// <summary>Opens the photo picker; <see langword="null"/> when the user cancels.</summary>
    public Task<string?> PickPhotoAsync() => _filePicker.BrowseForPhotoAsync();

    /// <summary>Copies the picked photo into the photo storage and returns its relative path, or <see langword="null"/>.</summary>
    public Task<string?> StorePhotoAsync(string sourceFilePath, string category, Guid entityId) =>
        _photoStorage.SavePhotoAsync(sourceFilePath, category, entityId);

    /// <summary>Absolute path of a stored photo, or <see langword="null"/> when it cannot be resolved.</summary>
    public string? GetPhotoFullPath(string? relativePath) => _photoStorage.GetPhotoFullPath(relativePath);
}
