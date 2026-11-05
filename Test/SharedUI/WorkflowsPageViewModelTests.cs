// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
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

/// <summary>Workflow page presentation: shared library identity and persisted panel expansion.</summary>
[TestFixture]
internal sealed partial class WorkflowsPageViewModelTests : IDisposable
{
    private static readonly string[] ExpansionProperties =
    [
        nameof(WorkflowsPageViewModel.IsWorkflowListExpanded),
        nameof(WorkflowsPageViewModel.IsWorkflowActionsExpanded),
        nameof(WorkflowsPageViewModel.IsWorkflowPropertiesExpanded)
    ];

    private SolutionSession _session = null!;
    private Mock<ISettingsService> _persistence = null!;
    private AppSettings _settings = null!;
    private WorkflowLibraryViewModel _library = null!;

    [SetUp]
    public void SetUp()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _session = TestSolutionSessions.Create(new Solution { Projects = [new Project
        {
            Workflows = [new Workflow { Name = "Departure" }, new Workflow { Name = "Arrival" }]
        }] }, dispatcher.Object, runtime.Object);
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _library = new WorkflowLibraryViewModel(_session);
    }

    [TearDown]
    public void Dispose()
    {
        _library.Dispose();
        GC.SuppressFinalize(this);
    }

    private WorkflowsPageViewModel CreatePage(ILogger<WorkflowsPageViewModel>? logger = null) =>
        new(_library, _persistence.Object, logger ?? NullLogger<WorkflowsPageViewModel>.Instance);

    [Test]
    public void Constructor_RequiresLibrarySettingsAndLogger()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new WorkflowsPageViewModel(null!, _persistence.Object, NullLogger<WorkflowsPageViewModel>.Instance),
                Throws.ArgumentNullException);
            Assert.That(() => new WorkflowsPageViewModel(_library, null!, NullLogger<WorkflowsPageViewModel>.Instance),
                Throws.ArgumentNullException);
            Assert.That(() => new WorkflowsPageViewModel(_library, _persistence.Object, null!), Throws.ArgumentNullException);
        }
    }

    [Test]
    public void Page_PresentsTheSharedLibraryAndItsProjectSelection()
    {
        var page = CreatePage();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.WorkflowLibrary, Is.SameAs(_library));
            Assert.That(page.WorkflowLibrary.SelectedWorkflow, Is.SameAs(_session.SelectedProject!.Workflows[0]));
            Assert.That(page.WorkflowLibrary.SelectedEditorObject, Is.SameAs(_session.SelectedProject.Workflows[0]));
        }
        _library.SearchText = "Arr";
        Assert.That(page.WorkflowLibrary.FilteredWorkflows.Single().Name, Is.EqualTo("Arrival"));
        _session.SelectedProject = null;
        Assert.That(page.WorkflowLibrary.SelectedWorkflow, Is.Null);
    }

    [Test]
    public void Layout_UsesPersistedDefaultsAndSavesOnlyActualChanges()
    {
        using var assertions = Assert.EnterMultipleScope();
        var layout = _settings.Layout.WorkflowsPage;
        layout.IsWorkflowListExpanded = false;
        layout.IsActionsExpanded = false;
        layout.IsPropertiesExpanded = false;
        var page = CreatePage();
        Assert.That(page.IsWorkflowListExpanded, Is.False);
        Assert.That(page.IsWorkflowActionsExpanded, Is.False);
        Assert.That(page.IsWorkflowPropertiesExpanded, Is.False);
        page.IsWorkflowListExpanded = false;
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        var notifications = new List<string?>();
        page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        page.IsWorkflowListExpanded = true;
        page.IsWorkflowActionsExpanded = true;
        page.IsWorkflowPropertiesExpanded = true;
        Assert.That(layout.IsWorkflowListExpanded && layout.IsActionsExpanded && layout.IsPropertiesExpanded, Is.True);
        Assert.That(notifications, Is.EqualTo(ExpansionProperties));
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Exactly(3));
    }

    [Test]
    public async Task Layout_PersistenceFailureIsLoggedAndKeepsTheNewState()
    {
        var logged = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new Mock<ILogger<WorkflowsPageViewModel>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        logger.Setup(value => value.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation => logged.TrySetResult((Exception?)invocation.Arguments[3])));
        var failure = new IOException("settings file locked");
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.FromException(failure));
        var page = CreatePage(logger.Object);

        page.IsWorkflowActionsExpanded = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.IsWorkflowActionsExpanded, Is.False);
            Assert.That(_settings.Layout.WorkflowsPage.IsActionsExpanded, Is.False);
            Assert.That(await logged.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.SameAs(failure));
        }
    }
}
