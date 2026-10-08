// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Discovery;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;

/// <summary>
/// Searches the network for Z21 command stations and lists those not yet assigned to a project, so the user can
/// assign each one to a project. Every Z21 belongs to at most one project.
/// </summary>
public sealed partial class Z21AssignmentViewModel : ObservableObject
{
    private static readonly Action<ILogger, Exception?> LogSearchFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, nameof(LogSearchFailed)), "Z21 search failed");

    private readonly ISolutionSession _session;
    private readonly IZ21DiscoveryService _discovery;
    private readonly ILogger<Z21AssignmentViewModel> _logger;
    private IReadOnlyList<DiscoveredZ21> _found = [];
    private bool _hasSearched;

    /// <summary>
    /// Initializes a new instance of the <see cref="Z21AssignmentViewModel"/> class.
    /// </summary>
    public Z21AssignmentViewModel(ISolutionSession session, IZ21DiscoveryService discovery, ILogger<Z21AssignmentViewModel> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.ModelChanged += OnSessionModelChanged;
        _session.SolutionLoaded += OnSolutionLoaded;
    }

    /// <summary>
    /// Gets the Z21 command stations found by the latest search that no project uses yet.
    /// </summary>
    public ObservableCollection<Z21AssignmentCandidate> UnassignedZ21s { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the Z21 finder column is expanded.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFinderExpanded { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether a search is running.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    public partial bool IsSearching { get; private set; }

    /// <summary>
    /// Gets the result of the latest search for display.
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Search the network to list the Z21 command stations.";

    /// <summary>
    /// Gets a value indicating whether the Z21 may be assigned to the project, that is, no other project uses it.
    /// </summary>
    public bool CanAssignToProject(Z21AssignmentCandidate candidate, ProjectViewModel project)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(project);
        return FindOwner(candidate.IpAddress) is not { } owner || ReferenceEquals(owner, project);
    }

    /// <summary>
    /// Assigns a found Z21 to a project and selects that project; the project's previous Z21 becomes available again.
    /// </summary>
    /// <returns><see langword="true"/> when the Z21 was assigned; <see langword="false"/> when another project uses it.</returns>
    public bool AssignToProject(Z21AssignmentCandidate candidate, ProjectViewModel project)
    {
        if (!CanAssignToProject(candidate, project))
        {
            return false;
        }

        // Selecting the project first lets the session track, save and report the change.
        _session.SelectedProject = project;
        project.Z21IpAddress = candidate.IpAddress;
        project.Z21Port = candidate.Port;
        project.Z21SerialNumber = candidate.SerialNumber;
        RebuildUnassigned();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        IsSearching = true;
        StatusText = "Searching for Z21 command stations...";
        try
        {
            _found = await _discovery.DiscoverAllAsync().ConfigureAwait(true);
            _hasSearched = true;
        }
        catch (SocketException ex)
        {
            ReportSearchFailure(ex);
            return;
        }
        catch (InvalidOperationException ex)
        {
            ReportSearchFailure(ex);
            return;
        }
        finally
        {
            IsSearching = false;
        }

        RebuildUnassigned();
    }

    private bool CanSearch() => !IsSearching;

    private void ReportSearchFailure(Exception exception)
    {
        LogSearchFailed(_logger, exception);
        _found = [];
        _hasSearched = false;
        UnassignedZ21s.Clear();
        StatusText = $"Z21 search failed: {exception.Message}";
    }

    [RelayCommand]
    private void AssignToSelectedProject(Z21AssignmentCandidate? candidate)
    {
        if (candidate is not null && _session.SelectedProject is { } project)
        {
            AssignToProject(candidate, project);
        }
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            RebuildUnassigned();
        }
    }

    private void OnSessionModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ProjectViewModel && e.PropertyName is nameof(ProjectViewModel.Z21IpAddress))
        {
            RebuildUnassigned();
        }
    }

    private void OnSolutionLoaded(object? sender, EventArgs e) => RebuildUnassigned();

    private ProjectViewModel? FindOwner(string ipAddress) =>
        _session.SolutionViewModel?.Projects.FirstOrDefault(project =>
            string.Equals(project.Z21IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));

    private void RebuildUnassigned()
    {
        UnassignedZ21s.Clear();
        foreach (var z21 in _found.Where(z21 => FindOwner(z21.IpAddress) is null))
        {
            UnassignedZ21s.Add(new Z21AssignmentCandidate(z21.IpAddress, z21.Port, z21.SerialNumber));
        }

        if (!_hasSearched)
        {
            return;
        }

        StatusText = (_found.Count, UnassignedZ21s.Count) switch
        {
            (0, _) => "No Z21 found. Check that the Z21 is switched on and in the same network.",
            (_, 0) => $"{_found.Count} Z21 found; all are assigned to projects.",
            _ => $"{_found.Count} Z21 found; {UnassignedZ21s.Count} not assigned yet."
        };
    }
}

/// <summary>
/// A Z21 found on the network that no project uses yet.
/// </summary>
/// <param name="IpAddress">IP address of the Z21.</param>
/// <param name="Port">UDP port of the Z21.</param>
/// <param name="SerialNumber">Serial number reported by the Z21.</param>
public sealed record Z21AssignmentCandidate(string IpAddress, int Port, uint SerialNumber)
{
    /// <summary>Gets the title shown for the Z21.</summary>
    public string Title => $"Z21 {IpAddress}";

    /// <summary>Gets the details shown for the Z21.</summary>
    public string Detail => $"Serial number {SerialNumber}, port {Port}";
}
