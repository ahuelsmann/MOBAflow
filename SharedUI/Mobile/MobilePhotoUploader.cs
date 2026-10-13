// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Mobile;

using Interface;

/// <summary>
/// Result of <see cref="MobilePhotoUploader.CaptureAndUploadAsync"/>.
/// </summary>
/// <param name="Success">Whether MOBAflow received the photo.</param>
/// <param name="Status">The status text shown below the photo button.</param>
public sealed record PhotoUploadResult(bool Success, string Status);

/// <summary>
/// Takes a photo on the phone and uploads it to MOBAflow, which assigns it to the item selected on the PC.
/// Uses the known MOBApi endpoint and searches the LAN only when none is known.
/// </summary>
public sealed class MobilePhotoUploader
{
    private const string UploadCategory = "latest";

    private const string ServerNotFoundStatus =
        "⚠️ REST server not found\n\n" +
        "• Is MOBAflow (PC) running with\n  \"Auto-start REST API\" enabled?\n" +
        "• Are phone and PC on the same Wi‑Fi?\n" +
        "• Try again in a moment (discovery\n  runs automatically).\n\n" +
        "Server must listen on port 5001.";

    private readonly IPhotoCaptureService _photoCaptureService;
    private readonly IPhotoUploadService _photoUploadService;
    private readonly MobaflowEndpointLocator _endpointLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobilePhotoUploader"/> class.
    /// </summary>
    /// <param name="photoCaptureService">Takes the photo with the device camera.</param>
    /// <param name="photoUploadService">Uploads the photo to MOBApi.</param>
    /// <param name="endpointLocator">Finds MOBApi when no endpoint is known and remembers a working address.</param>
    public MobilePhotoUploader(
        IPhotoCaptureService photoCaptureService,
        IPhotoUploadService photoUploadService,
        MobaflowEndpointLocator endpointLocator)
    {
        ArgumentNullException.ThrowIfNull(photoCaptureService);
        ArgumentNullException.ThrowIfNull(photoUploadService);
        ArgumentNullException.ThrowIfNull(endpointLocator);
        _photoCaptureService = photoCaptureService;
        _photoUploadService = photoUploadService;
        _endpointLocator = endpointLocator;
    }

    /// <summary>
    /// Takes a photo and uploads it to <paramref name="knownEndpoint"/>, or to an endpoint found on the subnet of
    /// <paramref name="discoveryAnchor"/>. Failures become a status text instead of an exception.
    /// </summary>
    public async Task<PhotoUploadResult> CaptureAndUploadAsync(
        MobaflowEndpoint? knownEndpoint,
        string? discoveryAnchor,
        CancellationToken cancellationToken)
    {
        try
        {
            var localPath = await _photoCaptureService.CapturePhotoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(localPath))
            {
                return new PhotoUploadResult(false, "Capture cancelled or not available.");
            }

            var endpoint = knownEndpoint
                ?? await _endpointLocator.FindAsync(discoveryAnchor, cancellationToken).ConfigureAwait(false);
            if (endpoint is null)
            {
                return new PhotoUploadResult(false, ServerNotFoundStatus);
            }

            // Without an entity id MOBAflow assigns the photo to the selected item; the id only names the file.
            var (success, serverPhotoPath, error) = await _photoUploadService
                .UploadPhotoAsync(endpoint.IpAddress, endpoint.Port, localPath, UploadCategory, Guid.NewGuid())
                .ConfigureAwait(false);
            if (!success)
            {
                return new PhotoUploadResult(false, error ?? "Upload failed.");
            }

            await _endpointLocator.RememberReachableAddressAsync(endpoint.IpAddress).ConfigureAwait(false);
            return new PhotoUploadResult(true, serverPhotoPath ?? "Uploaded successfully.");
        }
        catch (Exception ex)
        {
            return new PhotoUploadResult(false, $"Error: {ex.Message}");
        }
    }
}
