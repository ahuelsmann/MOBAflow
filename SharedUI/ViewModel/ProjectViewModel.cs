// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Sound;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Project model that maintains a hierarchical collection structure
/// for TreeView binding. Must be manually refreshed when model changes (models don't fire events).
/// </summary>
public sealed partial class ProjectViewModel : ObservableObject, IViewModelWrapper<Project>
{
    #region Fields
    // Model
    /// <summary>
    /// Gets the underlying project domain model represented by this ViewModel.
    /// </summary>
    public Project Model { get; }

    // Optional Services
    private readonly IUiDispatcher? _dispatcher;
    private readonly IIoService? _ioService;
    private readonly ISoundPlayer? _soundPlayer;
    private readonly ILoggerFactory? _loggerFactory;

    // Properties (ObservableProperty fields)
    [ObservableProperty]
    private string _name;

    // Statistics (computed from collections)
    /// <summary>
    /// Gets the number of journeys contained in this project.
    /// </summary>
    public int JourneyCount => Journeys.Count;
    public int StationCount => Stations.Count;
    /// <summary>
    /// Gets the number of workflows contained in this project.
    /// </summary>
    public int WorkflowCount => Workflows.Count;
    /// <summary>
    /// Gets the number of trains contained in this project.
    /// </summary>
    public int TrainCount => Trains.Count;
    /// <summary>
    /// Gets the number of locomotives contained in this project.
    /// </summary>
    public int LocomotiveCount => Locomotives.Count;
    /// <summary>
    /// Gets the number of wagons contained in this project.
    /// </summary>
    public int WagonCount => Wagons.Count;
    #endregion

    partial void OnNameChanged(string value)
    {
        // Synchronize back to Model
        Model.Name = value;
    }

    /// <summary>
    /// Gets or sets the IP address of the Z21 this project controls; empty when none is assigned.
    /// A different address clears the remembered serial number.
    /// </summary>
    public string Z21IpAddress
    {
        get => Model.Z21.IpAddress;
        set
        {
            if (SetProperty(Model.Z21.IpAddress, value?.Trim() ?? string.Empty, Model.Z21, (z21, ip) => z21.IpAddress = ip))
            {
                Z21SerialNumber = null;
                OnPropertyChanged(nameof(Z21Summary));
                RemoveZ21Command.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the UDP port of the Z21 this project controls.
    /// </summary>
    public int Z21Port
    {
        get => Model.Z21.Port;
        set
        {
            if (SetProperty(Model.Z21.Port, value, Model.Z21, (z21, port) => z21.Port = port))
            {
                OnPropertyChanged(nameof(Z21Summary));
            }
        }
    }

    /// <summary>
    /// Gets or sets the serial number of the assigned Z21, when it is known from a network search.
    /// </summary>
    public uint? Z21SerialNumber
    {
        get => Model.Z21.SerialNumber;
        set => SetProperty(Model.Z21.SerialNumber, value, Model.Z21, (z21, serialNumber) => z21.SerialNumber = serialNumber);
    }

    /// <summary>
    /// Gets a short description of the assigned Z21 for lists.
    /// </summary>
    public string Z21Summary => string.IsNullOrEmpty(Z21IpAddress)
        ? "No Z21 assigned"
        : $"Z21 {Z21IpAddress}:{Z21Port}";

    /// <summary>
    /// Removes the Z21 assignment so the Z21 can be assigned to another project.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasZ21))]
    private void RemoveZ21()
    {
        Z21IpAddress = string.Empty;
        Z21Port = Z21Endpoint.DefaultPort;
    }

    private bool HasZ21() => !string.IsNullOrEmpty(Z21IpAddress);

    /// <summary>
    /// Hierarchical collection of Journey ViewModels.
    /// Manually synced with Model.Journeys via Refresh().
    /// </summary>
    public ObservableCollection<JourneyViewModel> Journeys { get; } = [];

    public ObservableCollection<StationViewModel> Stations { get; } = [];

    /// <summary>
    /// Hierarchical collection of Workflow ViewModels.
    /// Manually synced with Model.Workflows via Refresh().
    /// </summary>
    public ObservableCollection<WorkflowViewModel> Workflows { get; } = [];

    /// <summary>
    /// Hierarchical collection of Train ViewModels.
    /// Manually synced with Model.Trains via Refresh().
    /// </summary>
    public ObservableCollection<TrainViewModel> Trains { get; } = [];

    /// <summary>
    /// Hierarchical collection of Locomotive ViewModels.
    /// Manually synced with Model.Locomotives via Refresh().
    /// </summary>
    public ObservableCollection<LocomotiveViewModel> Locomotives { get; } = [];

    /// <summary>
    /// Hierarchical collection of Wagon ViewModels (combined PassengerWagons and GoodsWagons).
    /// Manually synced with Model via Refresh().
    /// </summary>
    public ObservableCollection<WagonViewModel> Wagons { get; } = [];

    /// <summary>
    /// Separate collection for PassengerWagon ViewModels (for Train Tab UI).
    /// </summary>
    public ObservableCollection<PassengerWagonViewModel> PassengerWagons { get; } = [];

    /// <summary>
    /// Separate collection for GoodsWagon ViewModels (for Train Tab UI).
    /// </summary>
    public ObservableCollection<GoodsWagonViewModel> GoodsWagons { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectViewModel"/> class for the given project.
    /// </summary>
    /// <param name="model">The project domain model.</param>
    /// <param name="dispatcher">Optional UI dispatcher used by nested ViewModels.</param>
    /// <param name="ioService">Optional IO service used by nested ViewModels.</param>
    /// <param name="soundPlayer">Optional sound player used by nested ViewModels.</param>
    /// <param name="loggerFactory">Optional factory for nested view model loggers.</param>
    public ProjectViewModel(Project model, IUiDispatcher? dispatcher = null, IIoService? ioService = null, ISoundPlayer? soundPlayer = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        _dispatcher = dispatcher;
        _ioService = ioService;
        _soundPlayer = soundPlayer;
        _loggerFactory = loggerFactory;
        _name = model.Name;  // Initialize from Model
        Refresh();

        Journeys.CollectionChanged += (_, _) => NotifyStatisticsChanged();
        Stations.CollectionChanged += (_, _) => NotifyStatisticsChanged();
        Workflows.CollectionChanged += (_, _) => NotifyStatisticsChanged();
        Trains.CollectionChanged += (_, _) => NotifyStatisticsChanged();
        Locomotives.CollectionChanged += (_, _) => NotifyStatisticsChanged();
        Wagons.CollectionChanged += (_, _) => NotifyStatisticsChanged();
    }

    private void NotifyStatisticsChanged()
    {
        OnPropertyChanged(nameof(JourneyCount));
        OnPropertyChanged(nameof(StationCount));
        OnPropertyChanged(nameof(WorkflowCount));
        OnPropertyChanged(nameof(TrainCount));
        OnPropertyChanged(nameof(LocomotiveCount));
        OnPropertyChanged(nameof(WagonCount));
    }

    /// <summary>
    /// Refreshes all collections from the model. Call this after model changes.
    /// Simple rebuild approach - performance is not critical (called rarely on Load/Save).
    /// </summary>
    public void Refresh()
    {
        // Update scalar properties from Model
        Name = Model.Name;

        // Clear and rebuild all collections
        Journeys.Clear();
        foreach (var j in Model.Journeys)
            Journeys.Add(new JourneyViewModel(j, Model, _dispatcher));

        Stations.Clear();
        foreach (var station in Model.Stations)
            Stations.Add(new StationViewModel(station, Model));

        RefreshWorkflows();

        Trains.Clear();
        foreach (var train in Model.Trains)
            Trains.Add(new TrainViewModel(train, this));

        Locomotives.Clear();
        foreach (var l in Model.Locomotives)
            Locomotives.Add(new LocomotiveViewModel(l));

        // Wagons: Combine PassengerWagons and GoodsWagons
        Wagons.Clear();
        foreach (var pw in Model.PassengerWagons)
            Wagons.Add(new PassengerWagonViewModel(pw));
        foreach (var gw in Model.GoodsWagons)
            Wagons.Add(new GoodsWagonViewModel(gw));

        // Separate collections for Train Tab UI
        PassengerWagons.Clear();
        foreach (var pw in Model.PassengerWagons)
            PassengerWagons.Add(new PassengerWagonViewModel(pw));

        GoodsWagons.Clear();
        foreach (var gw in Model.GoodsWagons)
            GoodsWagons.Add(new GoodsWagonViewModel(gw));

        NotifyStatisticsChanged();
    }

    /// <summary>Adds a workflow model and its authoritative wrapper to this project.</summary>
    public WorkflowViewModel AddWorkflow(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        Model.Workflows.Add(workflow);
        var viewModel = CreateWorkflowViewModel(workflow);
        Workflows.Add(viewModel);
        return viewModel;
    }

    /// <summary>Removes a workflow model and its authoritative wrapper from this project.</summary>
    public bool RemoveWorkflow(WorkflowViewModel workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var modelRemoved = Model.Workflows.Remove(workflow.Model);
        var viewModelRemoved = Workflows.Remove(workflow);
        return modelRemoved && viewModelRemoved;
    }

    private void RefreshWorkflows()
    {
        var existing = Workflows.ToList();
        var ordered = new List<WorkflowViewModel>(Model.Workflows.Count);
        foreach (var workflow in Model.Workflows)
        {
            var viewModel = existing.FirstOrDefault(candidate => ReferenceEquals(candidate.Model, workflow));
            ordered.Add(viewModel != null
                ? viewModel
                : CreateWorkflowViewModel(workflow));
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            if (index < Workflows.Count && ReferenceEquals(Workflows[index], ordered[index]))
            {
                continue;
            }

            var existingIndex = Workflows.IndexOf(ordered[index]);
            if (existingIndex >= 0)
            {
                Workflows.Move(existingIndex, index);
            }
            else
            {
                Workflows.Insert(index, ordered[index]);
            }
        }

        while (Workflows.Count > ordered.Count)
        {
            Workflows.RemoveAt(Workflows.Count - 1);
        }
    }

    private WorkflowViewModel CreateWorkflowViewModel(Workflow workflow) =>
        new(workflow, ioService: _ioService, soundPlayer: _soundPlayer, loggerFactory: _loggerFactory);
}
