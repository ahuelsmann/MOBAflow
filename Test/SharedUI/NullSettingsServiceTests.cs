// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;

/// <summary>
/// Tests for ISettingsService interface using a test implementation.
/// Verifies that the interface contract can be properly implemented.
/// </summary>
[TestFixture]
internal class SettingsServiceInterfaceTests
{
    private TestSettingsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new TestSettingsService();
    }

    [Test]
    public void GetSettings_ReturnsAppSettings()
    {
        var settings = _service.GetSettings();

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings, Is.TypeOf<AppSettings>());
    }

    [Test]
    public async Task LoadSettingsAsync_CompletesSuccessfully()
    {
        await _service.LoadSettingsAsync();

        Assert.That(_service.LoadCalled, Is.True);
    }

    [Test]
    public async Task SaveSettingsAsync_CompletesSuccessfully()
    {
        var settings = new AppSettings();

        await _service.SaveSettingsAsync(settings);

        Assert.That(_service.SaveCalled, Is.True);
        Assert.That(_service.LastSavedSettings, Is.SameAs(settings));
    }

    [Test]
    public async Task ResetToDefaultsAsync_CompletesSuccessfully()
    {
        await _service.ResetToDefaultsAsync();

        Assert.That(_service.ResetCalled, Is.True);
    }

    [Test]
    public void LastSolutionPath_DefaultsToNull()
    {
        Assert.That(_service.LastSolutionPath, Is.Null);
    }

    [Test]
    public void LastSolutionPath_CanBeSet()
    {
        _service.LastSolutionPath = "/some/path/solution.json";

        Assert.That(_service.LastSolutionPath, Is.EqualTo("/some/path/solution.json"));
    }

    [Test]
    public void AutoLoadLastSolution_DefaultsToFalse()
    {
        Assert.That(_service.AutoLoadLastSolution, Is.False);
    }

    [Test]
    public void AutoLoadLastSolution_CanBeSet()
    {
        _service.AutoLoadLastSolution = true;

        Assert.That(_service.AutoLoadLastSolution, Is.True);
    }

    /// <summary>
    /// Test implementation of ISettingsService for unit testing.
    /// </summary>
    private class TestSettingsService : ISettingsService
    {
        private readonly AppSettings _settings = new();
        
        public bool LoadCalled { get; private set; }
        public bool SaveCalled { get; private set; }
        public bool ResetCalled { get; private set; }
        public AppSettings? LastSavedSettings { get; private set; }

        public AppSettings GetSettings() => _settings;

        public Task LoadSettingsAsync()
        {
            LoadCalled = true;
            return Task.CompletedTask;
        }

        public Task SaveSettingsAsync(AppSettings settings)
        {
            SaveCalled = true;
            LastSavedSettings = settings;
            return Task.CompletedTask;
        }

        public Task ResetToDefaultsAsync()
        {
            ResetCalled = true;
            return Task.CompletedTask;
        }

        public string? LastSolutionPath { get; set; }
        public bool AutoLoadLastSolution { get; set; }
    }
}

