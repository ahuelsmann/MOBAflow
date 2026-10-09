// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Characterizes solution ownership in <see cref="SolutionSession"/>: load, save, auto-save status, dirty state,
/// project and journey selection.
/// </summary>
[TestFixture]
internal sealed partial class SolutionSessionTests
{
    private static readonly string[] LoadedProjectNames = ["A", "B"];

    [Test]
    public void Constructor_AddsUntitledProjectToEmptySolutionAndSelectsIt()
    {
        var session = CreateSession(new Solution(), out _, out _);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Solution.Projects, Has.Count.EqualTo(1));
            Assert.That(session.Solution.Projects[0].Name, Is.EqualTo("(Untitled Project)"));
            Assert.That(session.HasSolution, Is.True);
            Assert.That(session.SelectedProject?.Model, Is.SameAs(session.Solution.Projects[0]));
        }
    }

    [Test]
    public async Task LoadSolutionFromPath_ReplacesProjectsInPlaceSelectsFirstAndCreatesTheirRuntimes()
    {
        var solution = new Solution();
        var session = CreateSession(solution, out var io, out var runtime);
        var loaded = new Solution { Name = "Loaded", Projects = [new Project { Name = "A" }, new Project { Name = "B" }] };
        io.Setup(value => value.LoadFromPathAsync("layout.json")).ReturnsAsync((loaded, "layout.json", (string?)null));
        var replacing = 0;
        var loadedEvents = 0;
        session.SolutionReplacing += (_, _) => replacing++;
        session.SolutionLoaded += (_, _) => loadedEvents++;

        await session.LoadSolutionFromPathAsync("layout.json").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Solution, Is.SameAs(solution), "The solution instance is replaced in place.");
            Assert.That(session.Solution.Name, Is.EqualTo("Loaded"));
            Assert.That(session.Solution.Projects.Select(project => project.Name), Is.EqualTo(LoadedProjectNames));
            Assert.That(session.SelectedProject?.Name, Is.EqualTo("A"));
            Assert.That(session.CurrentSolutionPath, Is.EqualTo("layout.json"));
            Assert.That(session.HasUnsavedChanges, Is.False);
            Assert.That(session.SolutionSaveState, Is.EqualTo(SolutionSaveState.Saved));
            Assert.That(replacing, Is.EqualTo(1));
            Assert.That(loadedEvents, Is.EqualTo(1));
        }

        runtime.Verify(value => value.LoadAsync(
            It.Is<IReadOnlyList<Project>>(projects => projects.SequenceEqual(loaded.Projects)),
            It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.SelectProject(loaded.Projects[0].Id), Times.AtLeastOnce);
    }

    [Test]
    public void LoadSolutionFromPath_WithError_Throws()
    {
        var session = CreateSession(new Solution(), out var io, out _);
        io.Setup(value => value.LoadFromPathAsync("broken.json")).ReturnsAsync(((Solution?)null, (string?)null, "bad file"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => session.LoadSolutionFromPathAsync("broken.json"));

        Assert.That(exception!.Message, Does.Contain("bad file"));
    }

    [Test]
    public async Task SaveSolutionInternal_WithPath_WritesAndReportsSaved()
    {
        var session = CreateSession(new Solution(), out var io, out _);
        session.CurrentSolutionPath = "layout.json";
        io.Setup(value => value.SaveAsync(session.Solution, "layout.json")).ReturnsAsync((true, "layout.json", (string?)null));
        var saving = 0;
        session.SolutionSaving += (_, _) => saving++;

        var result = await session.SaveSolutionWithStatusAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.State, Is.EqualTo(SolutionSaveState.Saved));
            Assert.That(result.StatusText, Is.EqualTo("Saved"));
            Assert.That(session.HasUnsavedChanges, Is.False);
            Assert.That(saving, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task SaveSolutionInternal_WithoutPath_MarksDirtyAndAsksForSaveAs()
    {
        var session = CreateSession(new Solution(), out var io, out _);

        await session.SaveSolutionInternalAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.HasUnsavedChanges, Is.True);
            Assert.That(session.SolutionSaveState, Is.EqualTo(SolutionSaveState.NotSaved));
            Assert.That(session.SolutionSaveStatusText, Is.EqualTo("Not saved - choose Save As"));
        }

        io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SaveSolutionInternal_AfterShutdownBegan_DoesNotWrite()
    {
        var session = CreateSession(new Solution(), out var io, out _);
        session.CurrentSolutionPath = "layout.json";
        session.BeginShutdown();

        await session.SaveSolutionInternalAsync().ConfigureAwait(false);
        await session.DrainPendingSaveAsync().ConfigureAwait(false);

        Assert.That(session.SolutionSaveStatusText, Is.EqualTo("Not saved - application is shutting down"));
        io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task NewSolution_ReplacesProjectsClearsSelectionAndCreatesTheNewProjectRuntime()
    {
        var session = CreateSession(new Solution { Projects = [new Project { Name = "Old" }] }, out _, out var runtime);
        session.CurrentSolutionPath = "old.json";

        await session.NewSolutionAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Solution.Name, Is.EqualTo("New Solution"));
            Assert.That(session.Solution.Projects.Single().Name, Is.EqualTo("New Project"));
            Assert.That(session.CurrentSolutionPath, Is.Null);
            Assert.That(session.HasUnsavedChanges, Is.True);
            Assert.That(session.SelectedProject, Is.Null);
        }

        runtime.Verify(value => value.LoadAsync(
            It.Is<IReadOnlyList<Project>>(projects => projects.Single() == session.Solution.Projects[0]),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AddAndRemoveProject_UpdateSelectionAndHasSolution()
    {
        var session = CreateSession(new Solution(), out _, out _);
        var original = session.SelectedProject!;

        var added = session.AddProject(new Project { Name = "Second" });
        Assert.That(session.SelectedProject, Is.SameAs(added));

        session.RemoveProject(added);
        Assert.That(session.SelectedProject, Is.SameAs(original));

        session.RemoveProject(original);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.SelectedProject, Is.Null);
            Assert.That(session.HasSolution, Is.False);
            Assert.That(session.Solution.Projects, Is.Empty);
        }
    }

    [Test]
    public void SuppressAutoSave_IsActiveUntilDisposedAndNests()
    {
        var session = CreateSession(new Solution(), out _, out _);

        var outer = session.SuppressAutoSave();
        var inner = session.SuppressAutoSave();
        inner.Dispose();
        var stillSuppressed = session.IsAutoSaveSuppressed;
        outer.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stillSuppressed, Is.True);
            Assert.That(session.IsAutoSaveSuppressed, Is.False);
        }
    }

    [Test]
    public void SelectedProjectEdit_RaisesModelChangedAndMarksSolutionDirty()
    {
        var session = CreateSession(new Solution(), out _, out _);
        var changes = new List<string?>();
        session.ModelChanged += (_, e) => changes.Add(e.PropertyName);

        session.SelectedProject!.Name = "Renamed";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changes, Does.Contain(nameof(ProjectViewModel.Name)));
            Assert.That(session.HasUnsavedChanges, Is.True);
        }
    }

    [Test]
    public void SelectedProjectEdit_RefreshesTheRuntimeSnapshotWithoutReactivation()
    {
        var session = CreateSession(new Solution(), out _, out var runtime);
        runtime.Invocations.Clear();

        session.SelectedProject!.Name = "Renamed";

        runtime.Verify(value => value.UpdateAsync(session.SelectedProject.Model, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.ReplaceAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
        runtime.Verify(value => value.LoadAsync(It.IsAny<IReadOnlyList<Project>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void SelectingAProject_OnlyShowsItsRuntime()
    {
        var solution = new Solution { Projects = [new Project { Name = "A" }, new Project { Name = "B" }] };
        var session = CreateSession(solution, out _, out var runtime);
        runtime.Invocations.Clear();

        session.SelectedProject = session.SolutionViewModel!.Projects[1];

        // Selecting a project must not restart or re-activate any runtime (#190).
        runtime.Verify(value => value.SelectProject(solution.Projects[1].Id), Times.Once);
        runtime.VerifyNoOtherCalls();
    }

    [Test]
    public void ChangingTheZ21Assignment_RecreatesTheProjectRuntime()
    {
        var session = CreateSession(new Solution(), out _, out var runtime);
        runtime.Invocations.Clear();

        session.SelectedProject!.Z21IpAddress = "192.168.0.111";

        // A new address also clears the old serial number; that second change only refreshes the snapshot.
        runtime.Verify(value => value.ReplaceAsync(session.SelectedProject.Model, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AddingAndRemovingProjects_CreatesAndDiscardsTheirRuntimes()
    {
        var session = CreateSession(new Solution(), out _, out var runtime);
        var added = new Project { Name = "Yard" };

        var viewModel = session.AddProject(added);
        session.RemoveProject(viewModel);

        runtime.Verify(value => value.AddAsync(added, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.RemoveAsync(added.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TrackedChanges_IgnoreUiOnlyPropertiesSuppressionAndUntracking()
    {
        var session = CreateSession(new Solution(), out _, out _);
        var source = new ChangeSource();
        var changes = 0;
        session.ModelChanged += (_, _) => changes++;
        session.TrackChanges(source);
        session.TrackChanges(source);

        source.Raise("IsExpanded");
        using (session.SuppressAutoSave())
        {
            source.Raise("Name");
        }

        source.Raise("Name");
        session.UntrackChanges(source);
        source.Raise("Name");

        Assert.That(changes, Is.EqualTo(1), "Only one persisted change while tracked, not suppressed and not UI-only.");
    }

    private static SolutionSession CreateSession(
        Solution solution,
        out Mock<IIoService> io,
        out Mock<IProjectRuntimeHost> runtime)
    {
        io = new Mock<IIoService>();
        runtime = new Mock<IProjectRuntimeHost>();
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        dispatcher.Setup(value => value.InvokeOnUiAsync(It.IsAny<Func<Task<SolutionSaveResult>>>()))
            .Returns<Func<Task<SolutionSaveResult>>>(func => func());
        return new SolutionSession(
            solution,
            io.Object,
            dispatcher.Object,
            runtime.Object,
            NullLogger<SolutionSession>.Instance);
    }

    private sealed partial class ChangeSource : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public void Raise(string propertyName) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
