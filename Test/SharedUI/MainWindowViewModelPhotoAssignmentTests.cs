// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed class MainWindowViewModelPhotoAssignmentTests
{
    [Test]
    public void AssignUploadedPhotoToSelectedEntity_AssignsToSelectedLocomotive()
    {
        var viewModel = CreateViewModel();
        var locomotive = new LocomotiveViewModel(new Locomotive { Name = "BR 218" });
        viewModel.UpdateActivePhotoAssignmentPageTag("locomotives");
        viewModel.SelectedLocomotive = locomotive;

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/a.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.Locomotive));
        Assert.That(locomotive.PhotoPath, Is.EqualTo("photos/temp/a.jpg"));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_AssignsToSelectedPassengerWagon()
    {
        var viewModel = CreateViewModel();
        var passengerWagon = new PassengerWagonViewModel(new PassengerWagon { Name = "Avmz" });
        viewModel.UpdateActivePhotoAssignmentPageTag("passengerwagons");
        viewModel.SelectedPassengerWagon = passengerWagon;

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/b.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.PassengerWagon));
        Assert.That(passengerWagon.PhotoPath, Is.EqualTo("photos/temp/b.jpg"));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_AssignsToSelectedGoodsWagon()
    {
        var viewModel = CreateViewModel();
        var goodsWagon = new GoodsWagonViewModel(new GoodsWagon { Name = "Eaos" });
        viewModel.UpdateActivePhotoAssignmentPageTag("goodswagons");
        viewModel.SelectedGoodsWagon = goodsWagon;

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/c.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.GoodsWagon));
        Assert.That(goodsWagon.PhotoPath, Is.EqualTo("photos/temp/c.jpg"));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_ReturnsNoneWhenNothingSelected()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateActivePhotoAssignmentPageTag("locomotives");

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/d.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.None));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_UsesCurrentPageAndIgnoresStaleOtherSelection()
    {
        var viewModel = CreateViewModel();
        var locomotive = new LocomotiveViewModel(new Locomotive { Name = "BR 218" });
        var passengerWagon = new PassengerWagonViewModel(new PassengerWagon { Name = "Avmz" });
        viewModel.SelectedLocomotive = locomotive;
        viewModel.SelectedPassengerWagon = passengerWagon;
        viewModel.UpdateActivePhotoAssignmentPageTag("passengerwagons");

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/e.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.PassengerWagon));
        Assert.That(passengerWagon.PhotoPath, Is.EqualTo("photos/temp/e.jpg"));
        Assert.That(locomotive.PhotoPath, Is.Null);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntimeMock.Object,
            eventBusMock.Object,
            uiDispatcherMock.Object,
            new AppSettings(),
            new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object);
    }
}
