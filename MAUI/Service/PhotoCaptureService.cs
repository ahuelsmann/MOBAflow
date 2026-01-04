// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using SharedUI.Interface;
using System.IO;
using Microsoft.Maui.Storage;

public class PhotoCaptureService : IPhotoCaptureService
{
    public async Task<string?> CapturePhotoAsync()
    {
        // Ensure camera permission
        var camStatus = await Permissions.RequestAsync<Permissions.Camera>();
        if (camStatus != PermissionStatus.Granted)
            return null;

        // On older Android versions ensure storage read (MediaPicker writes temp)
        var storageStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
        if (storageStatus != PermissionStatus.Granted && storageStatus != PermissionStatus.Unknown)
            return null;

        if (!MediaPicker.Default.IsCaptureSupported)
            return null;

        var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Capture train photo"
        });

        if (photo == null)
            return null;

        var localPath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
        await using (var source = await photo.OpenReadAsync())
        await using (var dest = File.Create(localPath))
        {
            await source.CopyToAsync(dest);
        }
        return localPath;
    }
}
