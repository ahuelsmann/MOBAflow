// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS

namespace Moba.Test.MOBAflow;

using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moba.WinUI.ViewModel;

using Moq;

/// <summary>
/// Deleting a matrix image asks first; only a confirmation for the still selected project and a still listed
/// matrix lets the page remove and save it. The page view model creates WinUI brushes, which need the WinUI runtime
/// and a UI thread, so the decision is tested through <see cref="MatrixDeleteConfirmation"/>.
/// </summary>
[TestFixture]
internal sealed class MatrixPageViewModelTests
{
    private sealed class Fixture
    {
        public Mock<ISolutionSession> Session { get; } = new();
        public Mock<IDialogService> Dialogs { get; } = new();
        public ProjectViewModel? Selected { get; set; }
        public bool IsListed { get; set; } = true;

        public Fixture()
        {
            Selected = new ProjectViewModel(new Project { Name = "Yard" }, TestViewModelServices.Default);
            Session.SetupGet(session => session.SelectedProject).Returns(() => Selected);
        }

        public Task<bool> ConfirmAsync(string? name) =>
            MatrixDeleteConfirmation.ConfirmAsync(Dialogs.Object, Session.Object, name, () => IsListed);

        public void SetupDialog(bool result) =>
            Dialogs.Setup(dialogs => dialogs.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(result);
    }

    [Test]
    public async Task Confirm_UserConfirms_AllowsTheDelete()
    {
        var fixture = new Fixture();
        fixture.SetupDialog(true);

        Assert.That(await fixture.ConfirmAsync("Signal").ConfigureAwait(true), Is.True);
    }

    [Test]
    public async Task Confirm_UserCancels_BlocksTheDelete()
    {
        var fixture = new Fixture();
        fixture.SetupDialog(false);

        Assert.That(await fixture.ConfirmAsync("Signal").ConfigureAwait(true), Is.False);
    }

    [Test]
    public async Task Confirm_AsksWithTheMatrixNameAndCancelAsDefault()
    {
        var fixture = new Fixture();
        fixture.SetupDialog(false);

        await fixture.ConfirmAsync("Signal").ConfigureAwait(true);

        fixture.Dialogs.Verify(dialogs => dialogs.ShowConfirmationAsync(
            "Delete Matrix", "Delete matrix image 'Signal'?", "Delete", "Cancel", true), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task Confirm_WithoutName_AsksWithThePlaceholderName(string? name)
    {
        var fixture = new Fixture();
        fixture.SetupDialog(false);

        await fixture.ConfirmAsync(name).ConfigureAwait(true);

        fixture.Dialogs.Verify(dialogs => dialogs.ShowConfirmationAsync(
            "Delete Matrix", "Delete matrix image 'New Matrix'?", "Delete", "Cancel", true), Times.Once);
    }

    [Test]
    public async Task Confirm_ProjectSwitchedWhileAsking_BlocksTheDelete()
    {
        var fixture = new Fixture();
        var pending = new TaskCompletionSource<bool>();
        fixture.Dialogs.Setup(dialogs => dialogs.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(pending.Task);

        var confirming = fixture.ConfirmAsync("Signal");
        fixture.Selected = new ProjectViewModel(new Project { Name = "Other" }, TestViewModelServices.Default);
        pending.SetResult(true);

        Assert.That(await confirming.ConfigureAwait(true), Is.False);
    }

    [Test]
    public async Task Confirm_MatrixNoLongerListedAfterAsking_BlocksTheDelete()
    {
        var fixture = new Fixture();
        var pending = new TaskCompletionSource<bool>();
        fixture.Dialogs.Setup(dialogs => dialogs.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(pending.Task);

        var confirming = fixture.ConfirmAsync("Signal");
        fixture.IsListed = false;
        pending.SetResult(true);

        Assert.That(await confirming.ConfigureAwait(true), Is.False);
    }

    [Test]
    public async Task Confirm_DialogFails_CountsAsCancel()
    {
        var fixture = new Fixture();
        fixture.Dialogs.Setup(dialogs => dialogs.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("Another dialog is open."));

        Assert.That(await fixture.ConfirmAsync("Signal").ConfigureAwait(true), Is.False);
    }
}

#endif
