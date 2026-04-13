// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

/// <summary>
/// ViewModel wrapper for Locomotive model with throttle control operations.
/// </summary>
public sealed partial class LocomotiveViewModel : ObservableObject, IViewModelWrapper<Locomotive>
{
    #region Fields
    // Model
    [ObservableProperty]
    private Locomotive _model;
    private int _photoVersion;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying locomotive model to wrap.</param>
    public LocomotiveViewModel(Locomotive model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    partial void OnModelChanged(Locomotive value)
    {
        _ = value;
        // Reset photo version and notify UI when model changes
        _photoVersion = 0;
        OnPropertyChanged(nameof(PhotoPath));
        OnPropertyChanged(nameof(PhotoPathWithVersion));
        OnPropertyChanged(nameof(HasPhoto));
    }

    /// <summary>
    /// Gets or sets the display name of the locomotive.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the current position index of the locomotive within a train.
    /// </summary>
    public uint Pos
    {
        get => Model.Pos;
        set => SetProperty(Model.Pos, value, Model, (m, v) => m.Pos = v);
    }

    /// <summary>
    /// Gets or sets the optional DCC digital address assigned to this locomotive.
    /// </summary>
    public uint? DigitalAddress
    {
        get => Model.DigitalAddress;
        set => SetProperty(Model.DigitalAddress, value, Model, (m, v) => m.DigitalAddress = v);
    }

    /// <summary>
    /// Gets or sets the manufacturer name of the locomotive.
    /// </summary>
    public string? Manufacturer
    {
        get => Model.Manufacturer;
        set => SetProperty(Model.Manufacturer, value, Model, (m, v) => m.Manufacturer = v);
    }

    /// <summary>
    /// Gets or sets the manufacturer article number.
    /// </summary>
    public string? ArticleNumber
    {
        get => Model.ArticleNumber;
        set => SetProperty(Model.ArticleNumber, value, Model, (m, v) => m.ArticleNumber = v);
    }

    /// <summary>
    /// Gets the display name of the locomotive series from <see cref="Locomotive.LocomotiveSeriesRef"/>.
    /// </summary>
    public string? Series => Model.LocomotiveSeriesRef?.Name;

    /// <summary>
    /// Gets or sets the primary color scheme of the locomotive.
    /// </summary>
    public ColorScheme? ColorPrimary
    {
        get => Model.ColorPrimary;
        set => SetProperty(Model.ColorPrimary, value, Model, (m, v) => m.ColorPrimary = v);
    }

    /// <summary>
    /// Gets or sets the secondary color scheme of the locomotive.
    /// </summary>
    public ColorScheme? ColorSecondary
    {
        get => Model.ColorSecondary;
        set => SetProperty(Model.ColorSecondary, value, Model, (m, v) => m.ColorSecondary = v);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the locomotive is configured to push the train.
    /// </summary>
    public bool IsPushing
    {
        get => Model.IsPushing;
        set => SetProperty(Model.IsPushing, value, Model, (m, v) => m.IsPushing = v);
    }

    /// <summary>
    /// Gets or sets the optional technical details of the locomotive.
    /// </summary>
    public Details? Details
    {
        get => Model.Details;
        set => SetProperty(Model.Details, value, Model, (m, v) => m.Details = v);
    }

    /// <summary>
    /// Gets or sets the invoice date for the locomotive purchase.
    /// </summary>
    public DateTime? InvoiceDate
    {
        get => Model.InvoiceDate;
        set => SetProperty(Model.InvoiceDate, value, Model, (m, v) => m.InvoiceDate = v);
    }

    /// <summary>
    /// Gets or sets the delivery date of the locomotive.
    /// </summary>
    public DateTime? DeliveryDate
    {
        get => Model.DeliveryDate;
        set => SetProperty(Model.DeliveryDate, value, Model, (m, v) => m.DeliveryDate = v);
    }

    /// <summary>
    /// Gets or sets the relative path of the locomotive photo, if any.
    /// </summary>
    public string? PhotoPath
    {
        get => Model.PhotoPath;
        set
        {
            // Always update photoVersion even if path hasn't changed (e.g., file overwrite)
            if (SetProperty(Model.PhotoPath, value, Model, (m, v) => m.PhotoPath = v))
            {
                _photoVersion++;
                OnPropertyChanged(nameof(PhotoPathWithVersion));
                OnPropertyChanged(nameof(HasPhoto));
            }
            else if (value != null)
            {
                // Path didn't change but we still want to refresh the image (cache busting)
                _photoVersion++;
                OnPropertyChanged(nameof(PhotoPathWithVersion));
            }
        }
    }

    /// <summary>
    /// Gets a cache-busting photo path for UI bindings by appending a version query parameter.
    /// </summary>
    public string? PhotoPathWithVersion => string.IsNullOrWhiteSpace(Model.PhotoPath) ? null : $"{Model.PhotoPath}?v={_photoVersion}";

    /// <summary>
    /// Gets a value indicating whether a photo is assigned to this locomotive.
    /// </summary>
    public bool HasPhoto => !string.IsNullOrWhiteSpace(Model.PhotoPath);

    [ObservableProperty]
    private IAsyncRelayCommand? _browsePhotoCommand;

    [ObservableProperty]
    private IRelayCommand? _deletePhotoCommand;

    [ObservableProperty]
    private IRelayCommand? _showInExplorerCommand;

    /// <summary>
    /// Command to open/preview the photo. Set by the hosting page.
    /// </summary>
    [ObservableProperty]
    private IRelayCommand? _openPhotoCommand;
}
