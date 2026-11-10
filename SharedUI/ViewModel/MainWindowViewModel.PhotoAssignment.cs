// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;

using Microsoft.Extensions.Logging;

/// <summary>Routes uploaded photos to the editor of the active inventory page.</summary>
public partial class MainWindowViewModel
{
    private string? _activePhotoAssignmentPageTag;

    public void UpdateActivePhotoAssignmentPageTag(string? pageTag) => _activePhotoAssignmentPageTag = pageTag;

    public PhotoAssignmentTarget AssignUploadedPhotoToSelectedEntity(string photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
            return PhotoAssignmentTarget.None;
        return _activePhotoAssignmentPageTag switch
        {
            "locomotives" => LocomotivesPage.AssignUploadedPhoto(photoPath),
            "passengerwagons" => WagonsPage.AssignUploadedPassengerWagonPhoto(photoPath),
            "goodswagons" => WagonsPage.AssignUploadedGoodsWagonPhoto(photoPath),
            _ => PhotoAssignmentTarget.None,
        };
    }

    /// <summary>
    /// Event handler for <see cref="PhotoAssignedEvent"/>, delivered on the UI thread by the decorated event bus.
    /// Performs the photo assignment and logs the actual target.
    /// </summary>
    private void OnPhotoAssigned(PhotoAssignedEvent e)
    {
        var target = AssignUploadedPhotoToSelectedEntity(e.PhotoPath);

        switch (target)
        {
            case PhotoAssignmentTarget.Locomotive:
                LogPhotoAssigned(_logger, "locomotive", e.PhotoPath);
                break;
            case PhotoAssignmentTarget.PassengerWagon:
                LogPhotoAssigned(_logger, "passenger wagon", e.PhotoPath);
                break;
            case PhotoAssignmentTarget.GoodsWagon:
                LogPhotoAssigned(_logger, "goods wagon", e.PhotoPath);
                break;
            default:
                LogPhotoWithoutTarget(_logger, e.PhotoPath);
                break;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Assigned uploaded photo to selected {Target}: {PhotoPath}")]
    private static partial void LogPhotoAssigned(ILogger logger, string target, string photoPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Photo uploaded but no locomotive/wagon is selected. Path: {PhotoPath}")]
    private static partial void LogPhotoWithoutTarget(ILogger logger, string photoPath);
}
