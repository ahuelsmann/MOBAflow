// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;

/// <summary>
/// Tests for default configuration values so config refactors don't break apps silently.
/// </summary>
[TestFixture]
internal class AppSettingsDefaultsTests
{
    [Test]
    public void Application_PhotoStoragePath_default_is_empty()
    {
        var app = new ApplicationSettings();
        Assert.That(app.PhotoStoragePath, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Application_AutoStartWebApp_default_is_true()
    {
        var app = new ApplicationSettings();
        Assert.That(app.AutoStartWebApp, Is.True);
    }

    [Test]
    public void RestApi_Port_default_is_5001()
    {
        var rest = new RestApiSettings();
        Assert.That(rest.Port, Is.EqualTo(5001));
    }

    [Test]
    public void RestApi_CurrentIpAddress_default_is_non_empty()
    {
        var rest = new RestApiSettings();
        Assert.That(rest.CurrentIpAddress, Is.Not.Empty);
    }

    [Test]
    public void Z21_DefaultPort_default_is_21105()
    {
        var z21 = new Z21Settings();
        Assert.That(z21.DefaultPort, Is.EqualTo("21105"));
    }

    [Test]
    public void AppSettings_Application_default_is_not_null()
    {
        var settings = new AppSettings();
        Assert.That(settings.Application, Is.Not.Null);
    }

    [Test]
    public void AppSettings_RestApi_default_is_not_null()
    {
        var settings = new AppSettings();
        Assert.That(settings.RestApi, Is.Not.Null);
    }

    [Test]
    public void Counter_TimerIntervalSeconds_default_is_10()
    {
        var counter = new CounterSettings();
        Assert.That(counter.TimerIntervalSeconds, Is.EqualTo(10.0));
    }

    [Test]
    public void Counter_TargetLapCount_default_is_10()
    {
        var counter = new CounterSettings();
        Assert.That(counter.TargetLapCount, Is.EqualTo(10));
    }

    [Test]
    public void Counter_UseTimerFilter_default_is_true()
    {
        var counter = new CounterSettings();
        Assert.That(counter.UseTimerFilter, Is.True);
    }

    [Test]
    public void Z21_CurrentIpAddress_default_is_non_empty()
    {
        var z21 = new Z21Settings();
        Assert.That(z21.CurrentIpAddress, Is.Not.Empty);
    }

    [Test]
    public void Application_IsDarkMode_default_is_true()
    {
        var app = new ApplicationSettings();
        Assert.That(app.IsDarkMode, Is.True);
    }
}
