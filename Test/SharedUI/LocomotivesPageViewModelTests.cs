// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Common.Configuration;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.Diagnostics.CodeAnalysis;

[TestFixture]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "NUnit creates this fixture through reflection.")]
internal sealed partial class LocomotivesPageViewModelTests : IDisposable
{
    private SolutionSession _session = null!;
    private Mock<IIoService> _io = null!;
    private Mock<ISettingsService> _persistence = null!;
    private AppSettings _settings = null!;
    private LocomotivesPageViewModel _page = null!;
    private Mock<Action<string>> _explorer = null!;
    private LocomotiveTrainUsageViewModel _usage = null!;

    [SetUp]
    public void SetUp()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _io = new Mock<IIoService>();
        _io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        _session = TestSolutionSessions.Create(new Solution { Projects = [new Project()] }, dispatcher.Object, runtime.Object, _io.Object);
        _session.CurrentSolutionPath = "locomotives-test.json";
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        _settings = new AppSettings();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _settings.Layout.LocomotivesPage.IsListExpanded = false;
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(value => value.Detect(It.IsAny<Project>())).Returns(new DigitalAddressConflictReport([], []));
        var management = new LocomotiveManagementViewModel(detector.Object, new LocomotiveLibraryService(), projectContext: _session);
        _explorer = new Mock<Action<string>>();
        _usage = TestRollingStockPages.CreateUsage(_session);
        _page = new LocomotivesPageViewModel(_session, _io.Object, _persistence.Object,
            new LayoutColumnWidthsViewModel(), new LocomotivePageAnalysis(management, _usage),
            NullLogger<LocomotivesPageViewModel>.Instance, _explorer.Object);
    }

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        _usage.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public async Task ShowUsage_ForwardsSelection_WithoutChangingDeleteOrSaving()
    {
        _page.AddLocomotiveCommand.Execute(null);
        var selected = _page.SelectedLocomotive;
        var deleteEnabled = _page.DeleteLocomotiveCommand.CanExecute(null);
        _io.Invocations.Clear();

        await _page.ShowUsageCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.Usage.EditedResult!.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Exists));
            Assert.That(_page.SelectedLocomotive, Is.SameAs(selected));
            Assert.That(_page.DeleteLocomotiveCommand.CanExecute(null), Is.EqualTo(deleteEnabled));
        }
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        _page.SelectedLocomotive = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.ShowUsageCommand.CanExecute(null), Is.False);
            Assert.That(_page.Usage.Diagnostics, Is.Empty);
        }
    }

    [Test]
    public void Layout_UsesPersistedDefaultsAndWritesOnlyRealChanges()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.IsLocomotivesListExpanded, Is.False);
            Assert.That(_page.IsLocomotivesPropertiesExpanded, Is.True);
        }
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        _page.IsLocomotivesListExpanded = false;
        _page.IsLocomotivesPropertiesExpanded = true;
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        _page.IsLocomotivesListExpanded = true;
        _page.IsLocomotivesPropertiesExpanded = false;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout.LocomotivesPage.IsListExpanded, Is.True);
            Assert.That(_settings.Layout.LocomotivesPage.IsPropertiesExpanded, Is.False);
        }
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Exactly(2));
    }

    [Test]
    public void SelectionAndEdits_RefreshManagementWithoutAViewSubscription()
    {
        _page.AddLocomotiveCommand.Execute(null);
        var locomotive = _page.SelectedLocomotive!;
        Assert.That(_page.Management.Passport!.LocomotiveId, Is.EqualTo(locomotive.Model.Id));
        locomotive.Name = "BR 218";
        locomotive.DigitalAddress = 218;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.Management.Passport!.Name, Is.EqualTo("BR 218"));
            Assert.That(_page.Management.Passport.DigitalAddress, Is.EqualTo(218));
        }
        _session.SelectedProject = new ProjectViewModel(new Project());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.SelectedLocomotive, Is.Null);
            Assert.That(_page.Management.Passport, Is.Null);
            Assert.That(_page.DeleteLocomotiveCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public void RemovingTheSelectedRow_ClearsSelectionAndStopsTrackingOldEdits()
    {
        _page.AddLocomotiveCommand.Execute(null);
        var locomotive = _page.SelectedLocomotive!;
        _session.SelectedProject!.Locomotives.Remove(locomotive);
        _io.Invocations.Clear();
        locomotive.Name = "Detached";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.SelectedLocomotive, Is.Null);
            Assert.That(_page.DeleteLocomotiveCommand.CanExecute(null), Is.False);
        }
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void Dispose_DetachesSelectionProjectAndSessionSubscriptions()
    {
        _page.AddLocomotiveCommand.Execute(null);
        var project = _session.SelectedProject!;
        var locomotive = _page.SelectedLocomotive!;
        _page.Dispose();
        _page.Dispose();
        var notifications = 0;
        _page.PropertyChanged += (_, _) => notifications++;
        _io.Invocations.Clear();
        locomotive.Name = "Detached";
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        project.Locomotives.Clear();
        _session.SelectedProject = new ProjectViewModel(new Project());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notifications, Is.Zero);
            Assert.That(_page.SelectedLocomotive, Is.SameAs(locomotive));
            Assert.That(_page.Management.Passport!.Name, Is.Not.EqualTo("Detached"));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task BrowsePhoto_CancelledPickerLeavesModelAndStorageUntouched(string? path)
    {
        _page.AddLocomotiveCommand.Execute(null);
        var locomotive = _page.SelectedLocomotive!;
        _io.Setup(value => value.BrowseForPhotoAsync()).ReturnsAsync(path);
        _io.Invocations.Clear();
        await locomotive.BrowsePhotoCommand!.ExecuteAsync(null).ConfigureAwait(false);
        Assert.That(locomotive.PhotoPath, Is.Null);
        _io.Verify(value => value.SavePhotoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task BrowsePhoto_DisposedDuringCopyDoesNotApplyOrPersistTheResult()
    {
        _page.AddLocomotiveCommand.Execute(null);
        var locomotive = _page.SelectedLocomotive!;
        var copy = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _io.Setup(value => value.BrowseForPhotoAsync()).ReturnsAsync("source.png");
        _io.Setup(value => value.SavePhotoAsync("source.png", "locomotives", locomotive.Model.Id)).Returns(copy.Task);
        var pending = locomotive.BrowsePhotoCommand!.ExecuteAsync(null);
        Assert.That(pending.IsCompleted, Is.False);
        _page.Dispose();
        _io.Invocations.Clear();
        copy.SetResult("copied.png");
        await pending.ConfigureAwait(false);
        Assert.That(locomotive.PhotoPath, Is.Null);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void PhotoExplorer_UsesTheHostAdapterForTheResolvedPath()
    {
        var temporaryPhoto = Path.GetTempFileName();
        try
        {
            _page.AddLocomotiveCommand.Execute(null);
            var locomotive = _page.SelectedLocomotive!;
            locomotive.PhotoPath = "relative.png";
            _io.Setup(value => value.GetPhotoFullPath("relative.png")).Returns(temporaryPhoto);
            locomotive.ShowInExplorerCommand!.Execute(null);
            _explorer.Verify(value => value(temporaryPhoto), Times.Once);
        }
        finally
        {
            File.Delete(temporaryPhoto);
        }
    }
}
