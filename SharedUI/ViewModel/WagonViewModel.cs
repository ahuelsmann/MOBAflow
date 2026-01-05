// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

/// <summary>
/// ViewModel wrapper for Wagon model.
/// </summary>
public partial class WagonViewModel : ObservableObject, IViewModelWrapper<Wagon>
{
    #region Fields
    // Model (ObservableProperty - mutable reference)
    [ObservableProperty]
    private Wagon model;
    #endregion

    public WagonViewModel(Wagon model)
    {
        Model = model;
    }

    partial void OnModelChanged(Wagon value)
    {
        // Reset photo version and notify UI when model changes
        photoVersion = 0;
        OnPropertyChanged(nameof(PhotoPath));
        OnPropertyChanged(nameof(PhotoPathWithVersion));
        OnPropertyChanged(nameof(HasPhoto));
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public uint Pos
    {
        get => Model.Pos;
        set => SetProperty(Model.Pos, value, Model, (m, v) => m.Pos = v);
    }

    public uint? DigitalAddress
    {
        get => Model.DigitalAddress;
        set => SetProperty(Model.DigitalAddress, value, Model, (m, v) => m.DigitalAddress = v);
    }

    public string? Manufacturer
    {
        get => Model.Manufacturer;
        set => SetProperty(Model.Manufacturer, value, Model, (m, v) => m.Manufacturer = v);
    }

    public string? ArticleNumber
    {
        get => Model.ArticleNumber;
        set => SetProperty(Model.ArticleNumber, value, Model, (m, v) => m.ArticleNumber = v);
    }

    public string? Series
    {
        get => Model.Series;
        set => SetProperty(Model.Series, value, Model, (m, v) => m.Series = v);
    }

    public ColorScheme? ColorPrimary
    {
        get => Model.ColorPrimary;
        set => SetProperty(Model.ColorPrimary, value, Model, (m, v) => m.ColorPrimary = v);
    }

    public ColorScheme? ColorSecondary
    {
        get => Model.ColorSecondary;
        set => SetProperty(Model.ColorSecondary, value, Model, (m, v) => m.ColorSecondary = v);
    }

    public Details? Details
    {
        get => Model.Details;
        set => SetProperty(Model.Details, value, Model, (m, v) => m.Details = v);
    }

    public DateTime? InvoiceDate
    {
        get => Model.InvoiceDate;
        set => SetProperty(Model.InvoiceDate, value, Model, (m, v) => m.InvoiceDate = v);
    }

    public DateTime? DeliveryDate
    {
        get => Model.DeliveryDate;
        set => SetProperty(Model.DeliveryDate, value, Model, (m, v) => m.DeliveryDate = v);
    }

    private int photoVersion;

    public string? PhotoPath
    {
        get => Model.PhotoPath;
        set
        {
            // Always update photoVersion even if path hasn't changed (e.g., file overwrite)
            if (SetProperty(Model.PhotoPath, value, Model, (m, v) => m.PhotoPath = v))
            {
                photoVersion++;
                OnPropertyChanged(nameof(PhotoPathWithVersion));
                OnPropertyChanged(nameof(HasPhoto));
            }
            else if (value != null)
            {
                // Path didn't change but we still want to refresh the image (cache busting)
                photoVersion++;
                OnPropertyChanged(nameof(PhotoPathWithVersion));
            }
        }
    }

    /// <summary>
    /// Cache-busting photo path for UI bindings.
    /// </summary>
    public string? PhotoPathWithVersion => string.IsNullOrWhiteSpace(Model.PhotoPath) ? null : $"{Model.PhotoPath}?v={photoVersion}";

    /// <summary>
    /// Returns true if a photo is assigned to this wagon.
    /// </summary>
    public bool HasPhoto => !string.IsNullOrWhiteSpace(Model.PhotoPath);

    [ObservableProperty]
    private IAsyncRelayCommand? browsePhotoCommand;

    [ObservableProperty]
    private IRelayCommand? deletePhotoCommand;

    [ObservableProperty]
    private IRelayCommand? showInExplorerCommand;
}
