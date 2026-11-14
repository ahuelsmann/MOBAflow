// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;
using System.Text.Json;

/// <summary>
/// Tests for default configuration values so config refactors don't break apps silently.
/// </summary>
[TestFixture]
[Category("Unit")]
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
    public void RestApi_CurrentIpAddress_default_is_empty_until_discovered()
    {
        var rest = new RestApiSettings();
        Assert.That(rest.CurrentIpAddress, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RestApi_IsConnectionEnabled_default_is_false()
    {
        var rest = new RestApiSettings();
        Assert.That(rest.IsConnectionEnabled, Is.False);
    }

    [Test]
    public void Speech_Piper_paths_default_to_empty()
    {
        var speech = new SpeechSettings();

        Assert.Multiple(() =>
        {
            Assert.That(speech.Piper.ExecutablePath, Is.EqualTo(string.Empty));
            Assert.That(speech.Piper.ModelPath, Is.EqualTo(string.Empty));
            Assert.That(speech.Piper.ConfigPath, Is.EqualTo(string.Empty));
        });
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase(" ", " ")]
    [TestCase("tools/piper.exe", "tools/piper.exe")]
    [TestCase("C:/Speech Tools/piper.exe", "C:/Speech Tools/piper.exe")]
    public void PiperExecutablePath_NormalizesMissingPathAndPreservesConfiguredPath(string? path, string expected)
    {
        var settings = new AppSettings();
        settings.Speech.Piper.ExecutablePath = path!;

        Assert.That(settings.Speech.Piper.ExecutablePath, Is.EqualTo(expected));
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase(" ", " ")]
    [TestCase("voices/en.onnx", "voices/en.onnx")]
    [TestCase("C:/Speech Models/en.onnx", "C:/Speech Models/en.onnx")]
    public void PiperModelPath_NormalizesMissingPathAndPreservesConfiguredPath(string? path, string expected)
    {
        var settings = new AppSettings();
        settings.Speech.Piper.ModelPath = path!;

        Assert.That(settings.Speech.Piper.ModelPath, Is.EqualTo(expected));
    }

    [Test]
    public void PiperPaths_FollowCurrentSpeechSettingsWithoutCachingOrMixingPaths()
    {
        var settings = new AppSettings
        {
            Speech = new SpeechSettings
            {
                Piper = new PiperSpeechSettings
                {
                    ExecutablePath = "tools/first.exe",
                    ModelPath = "voices/first.onnx"
                }
            }
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Speech.Piper.ExecutablePath, Is.EqualTo("tools/first.exe"));
            Assert.That(settings.Speech.Piper.ModelPath, Is.EqualTo("voices/first.onnx"));
        }

        settings.Speech.Piper.ExecutablePath = "tools/second.exe";
        settings.Speech.Piper.ModelPath = "voices/second.onnx";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Speech.Piper.ExecutablePath, Is.EqualTo("tools/second.exe"));
            Assert.That(settings.Speech.Piper.ModelPath, Is.EqualTo("voices/second.onnx"));
        }

        settings.Speech = new SpeechSettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Speech.Piper.ExecutablePath, Is.Empty);
            Assert.That(settings.Speech.Piper.ModelPath, Is.Empty);
        }
    }

    [Test]
    public void Speech_EnablePronunciationNormalization_default_is_true()
    {
        var speech = new SpeechSettings();
        Assert.That(speech.Piper.EnablePronunciationNormalization, Is.True);
    }

    [Test]
    public void Speech_PiperSentenceSilenceSeconds_default_is_zero()
    {
        var speech = new SpeechSettings();
        Assert.That(speech.Piper.SentenceSilenceSeconds, Is.Zero);
    }

    [Test]
    public void Speech_PronunciationReplacements_default_is_empty()
    {
        var speech = new SpeechSettings();
        Assert.That(speech.Piper.PronunciationReplacements, Is.Not.Null);
        Assert.That(speech.Piper.PronunciationReplacements, Is.Empty);
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

    [TestCase(0, 0, 0, 0)]
    [TestCase(0, -1, 0, 0)]
    [TestCase(0, int.MinValue, 0, 0)]
    [TestCase(0, 1, 1, 1)]
    [TestCase(0, int.MaxValue, int.MaxValue, 1)]
    [TestCase(4, 4, 4, 0)]
    [TestCase(4, 0, 0, 1)]
    [TestCase(4, -1, 0, 1)]
    [TestCase(4, int.MinValue, 0, 1)]
    [TestCase(4, 5, 5, 1)]
    [TestCase(int.MaxValue, int.MaxValue, int.MaxValue, 0)]
    [TestCase(int.MaxValue, 0, 0, 1)]
    public void CounterFeedbackPointsNotifyOnlyWhenNormalizedValueChanges(
        int initialValue, int requestedValue, int expectedValue, int expectedNotifications)
    {
        var counter = new CounterSettings { CountOfFeedbackPoints = initialValue };
        var changes = new List<(object? Sender, EventArgs Args, int Count)>();
        counter.FeedbackPointsChanged += (sender, args) =>
            changes.Add((sender, args, counter.CountOfFeedbackPoints));

        counter.CountOfFeedbackPoints = requestedValue;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counter.CountOfFeedbackPoints, Is.EqualTo(expectedValue));
            Assert.That(changes, Has.Count.EqualTo(expectedNotifications));
            if (changes.Count == 1)
            {
                Assert.That(changes[0].Sender, Is.SameAs(counter));
                Assert.That(changes[0].Args, Is.SameAs(EventArgs.Empty));
                Assert.That(changes[0].Count, Is.EqualTo(expectedValue),
                    "Subscribers must observe the updated feedback point count.");
            }
        }
    }

    [TestCase("{}", 0)]
    [TestCase("{\"Counter\":{}}", 0)]
    [TestCase("{\"Counter\":{\"CountOfFeedbackPoints\":-2147483648}}", 0)]
    [TestCase("{\"Counter\":{\"CountOfFeedbackPoints\":-1}}", 0)]
    [TestCase("{\"Counter\":{\"CountOfFeedbackPoints\":0}}", 0)]
    [TestCase("{\"Counter\":{\"CountOfFeedbackPoints\":3}}", 3)]
    [TestCase("{\"Counter\":{\"CountOfFeedbackPoints\":2147483647}}", int.MaxValue)]
    public void CounterFeedbackPointsLoadFromHandEditedSettings(string json, int expectedValue)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings!.Counter.CountOfFeedbackPoints, Is.EqualTo(expectedValue));
        }
    }

    [Test]
    public void Application_IsDarkMode_default_is_true()
    {
        var app = new ApplicationSettings();
        Assert.That(app.IsDarkMode, Is.True);
    }
}
