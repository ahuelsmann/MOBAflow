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
using System.Diagnostics.CodeAnalysis;

[TestFixture]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "NUnit creates this fixture through reflection.")]
internal sealed partial class WagonsPageViewModelTests : IDisposable
{
    private SolutionSession _session = null!;
    private Mock<ISolutionIoService> _io = null!;
    private Mock<ISettingsService> _persistence = null!;
    private AppSettings _settings = null!;
    private WagonsPageViewModel _page = null!;
    private Mock<Action<string>> _explorer = null!;

    [SetUp]
    public void SetUp()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _io = TestFileServices.CreateMock();
        _io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        _session = TestSolutionSessions.Create(new Solution { Projects = [new Project()] }, dispatcher.Object, runtime.Object, _io.Object);
        _session.CurrentSolutionPath = "wagons-test.json";
        _settings = new AppSettings();
        _settings.Layout.PassengerWagonPage.IsListExpanded = false;
        _settings.Layout.GoodsWagonPage.IsPropertiesExpanded = false;
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        _explorer = new Mock<Action<string>>();
        _page = new WagonsPageViewModel(_session, TestFileServices.Photos(_io.Object), _persistence.Object,
            new LayoutColumnWidthsViewModel(), NullLogger<WagonsPageViewModel>.Instance, _explorer.Object);
    }

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void Layout_UsesPersistedDefaultsAndWritesOnlyRealChanges()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.IsPassengerWagonListExpanded, Is.False);
            Assert.That(_page.IsPassengerWagonPropertiesExpanded, Is.True);
            Assert.That(_page.IsGoodsWagonListExpanded, Is.True);
            Assert.That(_page.IsGoodsWagonPropertiesExpanded, Is.False);
        }
        _page.IsPassengerWagonListExpanded = false;
        _page.IsPassengerWagonPropertiesExpanded = true;
        _page.IsGoodsWagonListExpanded = true;
        _page.IsGoodsWagonPropertiesExpanded = false;
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        _page.IsPassengerWagonListExpanded = true;
        _page.IsPassengerWagonPropertiesExpanded = false;
        _page.IsGoodsWagonListExpanded = false;
        _page.IsGoodsWagonPropertiesExpanded = true;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout.PassengerWagonPage.IsListExpanded, Is.True);
            Assert.That(_settings.Layout.PassengerWagonPage.IsPropertiesExpanded, Is.False);
            Assert.That(_settings.Layout.GoodsWagonPage.IsListExpanded, Is.False);
            Assert.That(_settings.Layout.GoodsWagonPage.IsPropertiesExpanded, Is.True);
        }
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Exactly(4));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void RemovedSelection_DetachesTrackingAndDisablesDeletion(bool passenger)
    {
        var wagon = AddWagon(passenger);
        var project = _session.SelectedProject!;
        if (passenger)
            project.PassengerWagons.Clear();
        else
            project.GoodsWagons.Clear();
        _io.Invocations.Clear();
        wagon.Name = "Detached";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(passenger ? _page.SelectedPassengerWagon : (WagonViewModel?)_page.SelectedGoodsWagon, Is.Null);
            Assert.That(passenger ? _page.DeletePassengerWagonCommand.CanExecute(null) : _page.DeleteGoodsWagonCommand.CanExecute(null), Is.False);
        }
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void Dispose_DetachesBothSelectionsProjectAndSessionWithoutChangingBoundState()
    {
        var passenger = AddWagon(true);
        var goods = AddWagon(false);
        var project = _session.SelectedProject!;
        _page.Dispose();
        _page.Dispose();
        var notifications = 0;
        _page.PropertyChanged += (_, _) => notifications++;
        _io.Invocations.Clear();
        passenger.Name = "Detached passenger";
        goods.Name = "Detached goods";
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        project.PassengerWagons.Clear();
        project.GoodsWagons.Clear();
        _session.SelectedProject = new ProjectViewModel(new Project());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notifications, Is.Zero);
            Assert.That(_page.SelectedPassengerWagon, Is.SameAs(passenger));
            Assert.That(_page.SelectedGoodsWagon, Is.SameAs(goods));
            Assert.That(_page.AssignUploadedPassengerWagonPhoto("late.png"), Is.EqualTo(Moba.Common.Events.PhotoAssignmentTarget.None));
            Assert.That(_page.AssignUploadedGoodsWagonPhoto("late.png"), Is.EqualTo(Moba.Common.Events.PhotoAssignmentTarget.None));
        }
    }

    [TestCase(true, null)]
    [TestCase(true, "")]
    [TestCase(false, null)]
    [TestCase(false, "")]
    public async Task BrowsePhoto_CancelledPickerLeavesModelAndStorageUntouched(bool passenger, string? path)
    {
        var wagon = AddWagon(passenger);
        _io.As<IFilePickerService>().Setup(value => value.BrowseForPhotoAsync()).ReturnsAsync(path);
        _io.Invocations.Clear();
        await wagon.BrowsePhotoCommand!.ExecuteAsync(null).ConfigureAwait(false);
        Assert.That(wagon.PhotoPath, Is.Null);
        _io.As<IPhotoStorageService>().Verify(value => value.SavePhotoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [TestCase(true, false)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    [TestCase(false, true)]
    public async Task BrowsePhoto_DisposedDuringPickerOrCopyDoesNotApplyOrPersist(bool passenger, bool duringCopy)
    {
        var wagon = AddWagon(passenger);
        var picker = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var copy = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var copyStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _io.As<IFilePickerService>().Setup(value => value.BrowseForPhotoAsync()).Returns(picker.Task);
        _io.As<IPhotoStorageService>().Setup(value => value.SavePhotoAsync("source.png", "wagons", wagon.Model.Id))
            .Callback(() => copyStarted.SetResult(true)).Returns(copy.Task);
        var pending = wagon.BrowsePhotoCommand!.ExecuteAsync(null);
        if (duringCopy)
        {
            picker.SetResult("source.png");
            await copyStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        _page.Dispose();
        _io.Invocations.Clear();
        if (!duringCopy)
            picker.SetResult("source.png");
        copy.SetResult("copied.png");
        await pending.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.That(wagon.PhotoPath, Is.Null);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        if (!duringCopy)
            _io.As<IPhotoStorageService>().Verify(value => value.SavePhotoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task BrowsePhoto_SameStoredPathRefreshesTheImageBindingAndPersists(bool passenger)
    {
        var wagon = AddWagon(passenger);
        wagon.PhotoPath = "stored.png";
        var previousBinding = wagon.PhotoPathWithVersion;
        _io.As<IFilePickerService>().Setup(value => value.BrowseForPhotoAsync()).ReturnsAsync("source.png");
        _io.As<IPhotoStorageService>().Setup(value => value.SavePhotoAsync("source.png", "wagons", wagon.Model.Id)).ReturnsAsync("stored.png");
        _io.Invocations.Clear();
        await wagon.BrowsePhotoCommand!.ExecuteAsync(null).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(wagon.PhotoPath, Is.EqualTo("stored.png"));
            Assert.That(wagon.PhotoPathWithVersion, Is.Not.EqualTo(previousBinding));
        }
        // The unchanged main baseline performs three saves: two image notifications plus the explicit photo save.
        _io.Verify(value => value.SaveAsync(_session.Solution, "wagons-test.json"), Times.Exactly(3));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void PhotoExplorer_UsesTheHostAdapterForTheResolvedPath(bool passenger)
    {
        var temporaryPhoto = Path.GetTempFileName();
        try
        {
            var wagon = AddWagon(passenger);
            wagon.PhotoPath = "relative.png";
            _io.As<IPhotoStorageService>().Setup(value => value.GetPhotoFullPath("relative.png")).Returns(temporaryPhoto);
            wagon.ShowInExplorerCommand!.Execute(null);
            _explorer.Verify(value => value(temporaryPhoto), Times.Once);
        }
        finally
        {
            File.Delete(temporaryPhoto);
        }
    }

    private WagonViewModel AddWagon(bool passenger)
    {
        if (passenger)
        {
            _page.AddPassengerWagonCommand.Execute(null);
            return _page.SelectedPassengerWagon!;
        }
        _page.AddGoodsWagonCommand.Execute(null);
        return _page.SelectedGoodsWagon!;
    }
}
