// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.ViewModel;

[TestFixture]
public sealed class DisplayViewModelTests
{
    [Test]
    public void Constructor_Should_SelectWaveshareLcdTouch700Inch_ByDefault()
    {
        var viewModel = new DisplayViewModel();

        Assert.That(viewModel.SelectedConfiguration, Is.SameAs(viewModel.WaveshareLcdTouch700InchConfiguration));
        Assert.That(viewModel.IsWaveshareLcdTouch700InchSelected, Is.True);
        Assert.That(viewModel.IsWaveshareLcd169InchSelected, Is.False);
        Assert.That(viewModel.IsWaveshareLcd147InchSelected, Is.False);
    }

    [TestCase(DisplayConfigurationKind.WaveshareLcdTouch700Inch)]
    [TestCase(DisplayConfigurationKind.WaveshareLcd169Inch)]
    [TestCase(DisplayConfigurationKind.WaveshareLcd147Inch)]
    public void SelectConfiguration_Should_UpdateSelectedConfiguration(DisplayConfigurationKind kind)
    {
        var viewModel = new DisplayViewModel();

        viewModel.SelectConfiguration(kind);

        Assert.That(viewModel.SelectedConfiguration.Kind, Is.EqualTo(kind));
        Assert.That(viewModel.IsWaveshareLcdTouch700InchSelected, Is.EqualTo(kind == DisplayConfigurationKind.WaveshareLcdTouch700Inch));
        Assert.That(viewModel.IsWaveshareLcd169InchSelected, Is.EqualTo(kind == DisplayConfigurationKind.WaveshareLcd169Inch));
        Assert.That(viewModel.IsWaveshareLcd147InchSelected, Is.EqualTo(kind == DisplayConfigurationKind.WaveshareLcd147Inch));
    }

    [Test]
    public void SelectConfiguration_Should_RaisePropertyChanged_ForSelectedConfigurationAndDependentSelectionProperties()
    {
        var viewModel = new DisplayViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.SelectConfiguration(DisplayConfigurationKind.WaveshareLcd169Inch);

        Assert.That(changedProperties, Does.Contain(nameof(DisplayViewModel.SelectedConfiguration)));
        Assert.That(changedProperties, Does.Contain(nameof(DisplayViewModel.IsWaveshareLcdTouch700InchSelected)));
        Assert.That(changedProperties, Does.Contain(nameof(DisplayViewModel.IsWaveshareLcd169InchSelected)));
        Assert.That(changedProperties, Does.Contain(nameof(DisplayViewModel.IsWaveshareLcd147InchSelected)));
    }
}