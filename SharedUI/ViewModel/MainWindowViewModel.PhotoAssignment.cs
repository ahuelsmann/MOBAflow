// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;

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
}
