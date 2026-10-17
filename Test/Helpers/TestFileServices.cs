// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

using Moq;

/// <summary>
/// Creates file-role test doubles that behave like a desktop host with file access. One mock object implements
/// all desktop file roles, so a test can verify solution saves and photo steps on a single object; production
/// code has no aggregate interface and each consumer receives only its role.
/// </summary>
internal static class TestFileServices
{
    /// <summary>
    /// A solution file mock whose access can persist and whose object also implements
    /// <see cref="IFilePickerService"/> and <see cref="IPhotoStorageService"/>; set those up through
    /// <c>mock.As&lt;IFilePickerService&gt;()</c> and <c>mock.As&lt;IPhotoStorageService&gt;()</c>.
    /// </summary>
    public static Mock<ISolutionIoService> CreateMock()
    {
        var mock = new Mock<ISolutionIoService>();
        mock.As<IFilePickerService>();
        mock.As<IPhotoStorageService>();
        mock.SetupGet(files => files.CanPersist).Returns(true);
        return mock;
    }

    /// <summary>
    /// Photo steps backed by a mock object from <see cref="CreateMock"/>, or by cancelling pickers when
    /// <paramref name="files"/> is omitted.
    /// </summary>
    public static PhotoAssignmentService Photos(ISolutionIoService? files = null) =>
        files is null
            ? new PhotoAssignmentService(new NullFilePickerService(), new Mock<IPhotoStorageService>().Object)
            : new PhotoAssignmentService(
                files as IFilePickerService ?? throw new ArgumentException("Use TestFileServices.CreateMock().", nameof(files)),
                files as IPhotoStorageService ?? throw new ArgumentException("Use TestFileServices.CreateMock().", nameof(files)));
}
