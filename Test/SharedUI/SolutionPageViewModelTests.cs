// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;

/// <summary>
/// Solution page state: project creation and deletion through the session and the persisted panel expansion.
/// </summary>
[TestFixture]
internal sealed partial class SolutionPageViewModelTests : IDisposable
{
    private static readonly string[] ProjectsAfterAdd = ["Main", "Yard", "New Project"];
    private static readonly string[] ProjectsAfterDelete = ["Main"];

    private SolutionSession _session = null!;
    private Mock<IDialogService> _dialogs = null!;
    private AppSettings _settings = null!;
    private Mock<ISettingsService> _persistence = null!;
    private SolutionPageViewModel _page = null!;
    private string? _directory;

    [SetUp]
    public void SetUp()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _session = TestSolutionSessions.Create(
            new Solution { Projects = [new Project { Name = "Main" }, new Project { Name = "Yard" }] },
            dispatcher.Object, runtime.Object);
        _dialogs = new Mock<IDialogService>();
        _dialogs.Setup(value => value.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _page = CreatePage();
    }

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        if (_directory is not null && Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private SolutionPageViewModel CreatePage() => new(_session, _dialogs.Object, TestLayouts.Create(_persistence.Object),
        NullLogger<SolutionPageViewModel>.Instance);

    [Test]
    public void Constructor_RequiresSessionDialogsLayoutAndLogger()
    {
        var layout = TestLayouts.Create(_persistence.Object);
        var logger = NullLogger<SolutionPageViewModel>.Instance;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new SolutionPageViewModel(null!, _dialogs.Object, layout, logger), Throws.ArgumentNullException);
            Assert.That(() => new SolutionPageViewModel(_session, null!, layout, logger), Throws.ArgumentNullException);
            Assert.That(() => new SolutionPageViewModel(_session, _dialogs.Object, null!, logger), Throws.ArgumentNullException);
            Assert.That(() => new SolutionPageViewModel(_session, _dialogs.Object, layout, null!), Throws.ArgumentNullException);
        }
    }

    [Test]
    public void Page_PresentsTheSessionSolutionAndSelection()
    {
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        _page.SelectedProject = _session.SolutionViewModel!.Projects[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.SolutionViewModel, Is.SameAs(_session.SolutionViewModel));
            Assert.That(_session.SelectedProject, Is.SameAs(_session.SolutionViewModel.Projects[1]));
            Assert.That(_page.SelectedProject, Is.SameAs(_session.SelectedProject));
            Assert.That(notifications, Does.Contain(nameof(SolutionPageViewModel.SelectedProject)));
        }
    }

    [Test]
    public void Layout_DefaultsAreExpanded()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.IsProjectListExpanded, Is.True);
            Assert.That(_page.IsProjectPropertiesExpanded, Is.True);
        }
    }

    /// <summary>Each flag reads and writes its own key: one flag differs from the other in every case.</summary>
    [TestCase(nameof(SolutionPageViewModel.IsProjectListExpanded))]
    [TestCase(nameof(SolutionPageViewModel.IsProjectPropertiesExpanded))]
    public void Layout_FlagReadsAndWritesItsOwnKey(string flag)
    {
        var layout = _settings.Layout.SolutionPage;
        layout.IsProjectListExpanded = flag != nameof(SolutionPageViewModel.IsProjectListExpanded);
        layout.IsPropertiesExpanded = flag != nameof(SolutionPageViewModel.IsProjectPropertiesExpanded);
        using var page = CreatePage();
        var notifications = new List<string?>();
        page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        using var assertions = Assert.EnterMultipleScope();
        Assert.That(page.IsProjectListExpanded, Is.EqualTo(layout.IsProjectListExpanded), "list read");
        Assert.That(page.IsProjectPropertiesExpanded, Is.EqualTo(layout.IsPropertiesExpanded), "properties read");

        if (flag == nameof(SolutionPageViewModel.IsProjectListExpanded))
            page.IsProjectListExpanded = true;
        else
            page.IsProjectPropertiesExpanded = true;

        Assert.That(layout.IsProjectListExpanded, Is.True, "list key");
        Assert.That(layout.IsPropertiesExpanded, Is.True, "properties key");
        Assert.That(notifications, Is.EqualTo(new[] { flag }).AsCollection);
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Once);
    }

    [Test]
    public void Layout_UnchangedValueIsNeitherNotifiedNorSaved()
    {
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        _page.IsProjectListExpanded = true;
        _page.IsProjectPropertiesExpanded = true;

        Assert.That(notifications, Is.Empty);
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
    }

    [Test]
    public void AddProject_AddsAndSelectsANewProject()
    {
        var canExecuteChanged = 0;
        _page.DeleteProjectCommand.CanExecuteChanged += (_, _) => canExecuteChanged++;

        _page.AddProjectCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_session.Solution.Projects.Select(project => project.Name), Is.EqualTo(ProjectsAfterAdd));
            Assert.That(_page.SelectedProject!.Model, Is.SameAs(_session.Solution.Projects[^1]));
            Assert.That(_page.DeleteProjectCommand.CanExecute(null), Is.True);
            Assert.That(canExecuteChanged, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task DeleteProject_ConfirmedRemovesTheProjectAndSelectsTheFirstRemaining()
    {
        _session.SelectedProject = _session.SolutionViewModel!.Projects[1];
        var journey = new JourneyViewModel(new Journey(), _session.SelectedProject!.Model);
        _session.SelectedJourney = journey;

        await _page.DeleteProjectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_session.Solution.Projects.Select(project => project.Name), Is.EqualTo(ProjectsAfterDelete));
            Assert.That(_session.SelectedProject!.Model.Name, Is.EqualTo("Main"));
            Assert.That(_session.SelectedJourney, Is.Not.SameAs(journey), "the deleted project's journey is cleared");
        }
        _dialogs.Verify(value => value.ShowConfirmationAsync("Delete Project", "Do you really want to delete the project?",
            "Yes", "No", true), Times.Once);
    }

    [Test]
    public async Task DeleteProject_CancelledKeepsTheProject()
    {
        _dialogs.Setup(value => value.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(false);
        var selected = _session.SelectedProject;

        await _page.DeleteProjectCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_session.Solution.Projects, Has.Count.EqualTo(2));
            Assert.That(_session.SelectedProject, Is.SameAs(selected));
        }
    }

    [Test]
    public async Task DeleteProject_ConnectedRuntimeWarnsThatItsTrainsStop()
    {
        var project = new ProjectViewModel(new Project { Name = "Main" }, TestViewModelServices.Default);
        var session = new Mock<ISolutionSession>();
        session.SetupGet(value => value.SelectedProject).Returns(project);
        session.Setup(value => value.GetConnectedProjectNames(It.IsAny<IReadOnlyCollection<Guid>?>())).Returns((string[])["Main"]);
        _dialogs.Setup(value => value.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(false);
        using var page = new SolutionPageViewModel(session.Object, _dialogs.Object, TestLayouts.Create(_persistence.Object),
            NullLogger<SolutionPageViewModel>.Instance);

        await page.DeleteProjectCommand.ExecuteAsync(null).ConfigureAwait(false);

        _dialogs.Verify(value => value.ShowConfirmationAsync("Delete Project",
            It.Is<string>(message => message.Contains("set to speed 0", StringComparison.Ordinal)), "Yes", "No", true), Times.Once);
        session.Verify(value => value.GetConnectedProjectNames(It.Is<IReadOnlyCollection<Guid>?>(ids =>
            ids != null && ids.Single() == project.Model.Id)), Times.Once);
        session.Verify(value => value.RemoveProject(It.IsAny<ProjectViewModel>()), Times.Never);
    }

    [Test]
    public async Task DeleteProject_BacksUpTheSolutionFileFirst()
    {
        var solutionPath = CreateSolutionFile("{\"saved\":true}");

        await _page.DeleteProjectCommand.ExecuteAsync(null).ConfigureAwait(false);

        var backup = await File.ReadAllTextAsync(Path.Combine(_directory!, "layout.backup.json")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(backup, Is.EqualTo("{\"saved\":true}"));
            Assert.That(File.Exists(solutionPath), Is.True);
            Assert.That(_session.Solution.Projects, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task DeleteProject_FailedBackupStillDeletesTheConfirmedProject()
    {
        CreateSolutionFile("{}");
        // A directory in the backup's place makes the copy fail.
        Directory.CreateDirectory(Path.Combine(_directory!, "layout.backup.json"));

        await _page.DeleteProjectCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.That(_session.Solution.Projects, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DeleteProject_IsUnavailableWithoutASelectedProject()
    {
        _session.SelectedProject = null;

        Assert.That(_page.DeleteProjectCommand.CanExecute(null), Is.False);
        await _page.DeleteProjectCommand.ExecuteAsync(null).ConfigureAwait(false);
        _dialogs.Verify(value => value.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public void Dispose_StopsFollowingTheSession()
    {
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        _page.Dispose();
        _session.SelectedProject = _session.SolutionViewModel!.Projects[1];

        Assert.That(notifications, Is.Empty);
    }

    private string CreateSolutionFile(string content)
    {
        _directory = Path.Combine(Path.GetTempPath(), "moba-solution-page-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        var solutionPath = Path.Combine(_directory, "layout.json");
        File.WriteAllText(solutionPath, content);
        _session.CurrentSolutionPath = solutionPath;
        return solutionPath;
    }
}
