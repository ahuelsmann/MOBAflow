namespace Moba.WinUI.Service;

using Backend.Model;

using Moba.SharedUI.Service;

using Microsoft.Windows.Storage.Pickers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class IoService : IIoService
{
    private Microsoft.UI.WindowId? _windowId;

    /// <summary>
    /// Sets the WindowId for the file pickers. Must be called before using the service.
    /// </summary>
    public void SetWindowId(Microsoft.UI.WindowId windowId)
    {
        _windowId = windowId;
    }

    public async Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        try
        {
            var picker = new FileOpenPicker(_windowId.Value)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                FileTypeFilter = { ".json" }
            };

            var result = await picker.PickSingleFileAsync();
            if (result == null) return (null, null, null);

            var sol = new Solution();
            sol = await sol.LoadAsync(result.Path);
            return (sol, result.Path, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    public async Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath)
    {
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        try
        {
            string? path = currentPath;
            if (string.IsNullOrEmpty(path))
            {
                var picker = new FileSavePicker(_windowId.Value)
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = "solution",
                    DefaultFileExtension = ".json",
                    FileTypeChoices = { { "JSON", new List<string> { ".json" } } }
                };

                var result = await picker.PickSaveFileAsync();
                if (result == null) return (false, null, null);
                path = result.Path;
            }

            await Solution.SaveAsync(path!, solution);
            return (true, path, null);
        }
        catch (Exception ex)
        {
            return (false, currentPath, ex.Message);
        }
    }
}