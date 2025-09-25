namespace Moba.WinUI.Service;

using Backend.Model;
using Moba.SharedUI.Service;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Windows.Storage.Pickers;

public class IoService : IIoService
{
    private readonly nint _hwnd;

    public IoService(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public async Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
            picker.FileTypeFilter.Add(".json");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return (null, null, null);
            var sol = Solution.Load(file.Path);
            return (sol, file.Path, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    public async Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath)
    {
        try
        {
            string? path = currentPath;
            if (string.IsNullOrEmpty(path))
            {
                var picker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
                picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
                picker.SuggestedFileName = "solution";
                var file = await picker.PickSaveFileAsync();
                if (file == null) return (false, null, null);
                path = file.Path;
            }
            Solution.Save(path!, solution);
            return (true, path, null);
        }
        catch (Exception ex)
        {
            return (false, currentPath, ex.Message);
        }
    }
}