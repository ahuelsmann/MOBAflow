// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;

/// <summary>
/// ViewModel wrapper for <see cref="Wagon"/> model.
/// </summary>
public partial class WagonViewModel : ObservableObject, IViewModelWrapper<Wagon>
{
    #region Fields
    // Model (ObservableProperty - mutable reference)
    [ObservableProperty]
    private Wagon _model;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WagonViewModel"/> class.
    /// </summary>
    /// <param name="model">The wagon domain model.</param>
    public WagonViewModel(Wagon model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    partial void OnModelChanged(Wagon value)
    {
        _ = value;
        // Reset photo version and notify UI when model changes
        _photoVersion = 0;
        OnPropertyChanged(nameof(PhotoPath));
        OnPropertyChanged(nameof(PhotoPathWithVersion));
        OnPropertyChanged(nameof(HasPhoto));
    }

    /// <summary>
    /// Gets or sets the display name of the wagon.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the 1-based position of the wagon within a train.
    /// </summary>
    public uint Pos
    {
        get => Model.Pos;
        set => SetProperty(Model.Pos, value, Model, (m, v) => m.Pos = v);
    }

    /// <summary>
    /// Gets or sets the optional digital address associated with this wagon.
    /// </summary>
    public uint? DigitalAddress
    {
        get => Model.DigitalAddress;
        set => SetProperty(Model.DigitalAddress, value, Model, (m, v) => m.DigitalAddress = v);
    }

    /// <summary>
    /// Gets or sets the manufacturer name of the wagon model.
    /// </summary>
    public string? Manufacturer
    {
        get => Model.Manufacturer;
        set => SetProperty(Model.Manufacturer, value, Model, (m, v) => m.Manufacturer = v);
    }

    /// <summary>
    /// Gets or sets the manufacturer article number of the wagon model.
    /// </summary>
    public string? ArticleNumber
    {
        get => Model.ArticleNumber;
        set => SetProperty(Model.ArticleNumber, value, Model, (m, v) => m.ArticleNumber = v);
    }

    /// <summary>
    /// Gets or sets the series or class name of the wagon.
    /// </summary>
    public string? Series
    {
        get => Model.Series;
        set => SetProperty(Model.Series, value, Model, (m, v) => m.Series = v);
    }

    /// <summary>
    /// Gets or sets the primary color scheme of the wagon.
    /// </summary>
    public ColorScheme? ColorPrimary
    {
        get => Model.ColorPrimary;
        set => SetProperty(Model.ColorPrimary, value, Model, (m, v) => m.ColorPrimary = v);
    }

    /// <summary>
    /// Gets or sets the secondary color scheme of the wagon.
    /// </summary>
    public ColorScheme? ColorSecondary
    {
        get => Model.ColorSecondary;
        set => SetProperty(Model.ColorSecondary, value, Model, (m, v) => m.ColorSecondary = v);
    }

    /// <summary>
    /// Gets or sets additional descriptive details for the wagon.
    /// </summary>
    public Details? Details
    {
        get => Model.Details;
        set => SetProperty(Model.Details, value, Model, (m, v) => m.Details = v);
    }

    /// <summary>
    /// Gets or sets the invoice date when the wagon was purchased.
    /// </summary>
    public DateTime? InvoiceDate
    {
        get => Model.InvoiceDate;
        set => SetProperty(Model.InvoiceDate, value, Model, (m, v) => m.InvoiceDate = v);
    }

    /// <summary>
    /// Gets or sets the delivery date when the wagon was received.
    /// </summary>
    public DateTime? DeliveryDate
    {
        get => Model.DeliveryDate;
        set => SetProperty(Model.DeliveryDate, value, Model, (m, v) => m.DeliveryDate = v);
    }

    private int _photoVersion;

    /// <summary>
    /// Gets or sets the relative path to the wagon photo file, if any.
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
    /// Cache-busting photo path for UI bindings.
    /// </summary>
    public string? PhotoPathWithVersion => string.IsNullOrWhiteSpace(Model.PhotoPath) ? null : $"{Model.PhotoPath}?v={_photoVersion}";

    /// <summary>
    /// Returns true if a photo is assigned to this wagon.
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
