// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Service.Recording;

using Common.Recording;

using SharedUI.Interface;

/// <summary>
/// Uses the initialized WinUI picker abstraction to import and atomically export recording artifacts.
/// </summary>
internal sealed class RecordingFileService : IRecordingFileService
{
    private readonly IFilePickerService _filePickerService;
    private readonly RecordingArtifactSerializer _serializer;

    public RecordingFileService(
        IFilePickerService filePickerService,
        RecordingArtifactSerializer serializer)
    {
        _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public async Task<RecordingFileExportResult> ExportAsync(
        RecordingArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = await _filePickerService.SaveRecordingFileAsync(CreateSuggestedFileName(artifact.Metadata.Name));
            if (string.IsNullOrWhiteSpace(path))
            {
                return new RecordingFileExportResult(false, true, null, null);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var bytes = _serializer.SerializeToUtf8(artifact);
            temporaryPath = path + ".tmp";
            await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, path, overwrite: true);
            temporaryPath = null;
            return new RecordingFileExportResult(true, false, path, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RecordingFileExportResult(false, true, null, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new RecordingFileExportResult(false, false, null, $"Export failed: {exception.Message}");
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    // Best-effort cleanup must not hide the original export result.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup must not hide the original export result.
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<RecordingFileImportResult> ImportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = await _filePickerService.BrowseForRecordingFileAsync();
            if (string.IsNullOrWhiteSpace(path))
            {
                return new RecordingFileImportResult(false, true, null, null, null);
            }

            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var validation = _serializer.Import(bytes);
            if (!validation.IsValid)
            {
                var error = validation.Errors[0];
                return new RecordingFileImportResult(
                    false,
                    false,
                    path,
                    null,
                    $"Import failed at {error.Path}: {error.Message}");
            }

            return new RecordingFileImportResult(true, false, path, validation.Artifact, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RecordingFileImportResult(false, true, null, null, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new RecordingFileImportResult(false, false, null, null, $"Import failed: {exception.Message}");
        }
    }

    private static string CreateSuggestedFileName(string sessionName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var normalized = new string(sessionName
            .Trim()
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? "recording" : normalized;
    }
}