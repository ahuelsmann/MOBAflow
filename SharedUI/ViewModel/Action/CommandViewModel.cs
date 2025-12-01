// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Backend;
using Moba.Domain;
using Moba.Domain.Enum;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Diagnostics;

public partial class CommandViewModel : ObservableObject
{
    private readonly Z21? _z21;

    [ObservableProperty]
    private Command model;

    public CommandViewModel(Command model, Z21? z21 = null)
    {
        Model = model;
        _z21 = z21;
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public byte[]? Bytes
    {
        get => Model.Bytes;
        set
        {
            if (SetProperty(Model.Bytes, value, Model, (m, v) => m.Bytes = v))
            {
                OnPropertyChanged(nameof(BytesString));
            }
        }
    }

    public string? BytesString
    {
        get => Model.Bytes != null ? BitConverter.ToString(Model.Bytes) : null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var bytes = value.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
                    Bytes = bytes;
                }
                catch
                {
                    Debug.WriteLine($"⚠ Ungültiges Byte-Format: {value}");
                }
            }
        }
    }

    public async Task ExecuteAsync(Journey journey, Station station)
    {
        if (Model.Bytes == null || Model.Bytes.Length == 0)
        {
            return;
        }

        Debug.WriteLine($"📤 Sende Command an Z21: {BitConverter.ToString(Model.Bytes)}");

        if (_z21 != null)
        {
            await _z21.SendCommandAsync(Model.Bytes);
        }
        else
        {
            Debug.WriteLine("⚠ Z21 nicht verfügbar - Command nicht gesendet");
        }
    }
}