// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using System.Text.Json;

[TestFixture]
internal sealed class PersistedDomainDefaultsTests
{
    [Test]
    public void DeserializeInterlocking_OmittedProperties_PreservesOperationalDefaults()
    {
        const string json = """
            {
              "turnouts": [{}],
              "signals": [{}],
              "blocks": [{"feedbackInputs": [{}]}],
              "connections": [{}]
            }
            """;

        var definition = JsonSerializer.Deserialize<InterlockingDefinition>(json, JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(definition.Turnouts.Single().Name, Is.Empty);
            Assert.That(definition.Signals.Single().Name, Is.Empty);
            Assert.That(definition.Blocks.Single().Name, Is.Empty);
            Assert.That(definition.Blocks.Single().FeedbackInputs.Single().ActiveState, Is.True);
            Assert.That(definition.Connections.Single().IsBidirectional, Is.True);
        }
    }

    [Test]
    public void DeserializeTurnoutCommand_ActivationDefaultsToTrue_AndExplicitFalseIsPreserved()
    {
        var omitted = JsonSerializer.Deserialize<TurnoutAccessoryCommand>("{}", JsonOptions.Default)!;
        var configured = JsonSerializer.Deserialize<TurnoutAccessoryCommand>("""{"activate":false}""", JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(omitted.Activate, Is.True);
            Assert.That(configured.Activate, Is.False);
        }
    }

    [Test]
    public void DeserializeSignalPayload_OmittedArticleNumbers_UseSupportedHardwareDefaults()
    {
        var payload = JsonSerializer.Deserialize<SelectSignalAspectActionPayload>("{}", JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(payload.MultiplexerArticleNumber, Is.EqualTo("5229"));
            Assert.That(payload.SignalArticleNumber, Is.EqualTo("4046"));
            Assert.That(payload.SignalAspect, Is.EqualTo(SignalAspect.Hp0));
        }
    }

    [Test]
    public void DeserializeWorkflowPayloads_OmittedFlags_EnableClearAndNextStop()
    {
        var display = JsonSerializer.Deserialize<TrainDestinationDisplayActionPayload>("{}", JsonOptions.Default)!;
        var transition = JsonSerializer.Deserialize<ChangeJourneyStopActionPayload>("{}", JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(display.ClearBeforeRender, Is.True);
            Assert.That(transition.MoveToNextStop, Is.True);
            Assert.That(transition.TargetStationId, Is.Null);
        }
    }

    [Test]
    public void DeserializeLocomotiveInventory_OmittedText_UsesEmptyValues()
    {
        var series = JsonSerializer.Deserialize<LocomotiveSeries>("{}", JsonOptions.Default)!;
        var snapshot = JsonSerializer.Deserialize<DecoderCvSnapshot>("{}", JsonOptions.Default)!;
        var rule = JsonSerializer.Deserialize<LocomotiveWhistleRule>("{}", JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Name, Is.Empty);
            Assert.That(series.Type, Is.Empty);
            Assert.That(series.Epoch, Is.Empty);
            Assert.That(series.Description, Is.Empty);
            Assert.That(snapshot.Name, Is.Empty);
            Assert.That(rule.Name, Is.Empty);
            Assert.That(rule.Enabled, Is.True);
        }
    }

    [Test]
    public void DeserializeSpeechConfiguration_OmittedProperties_SelectsDefaultEngineAndVoice()
    {
        var engine = JsonSerializer.Deserialize<SpeakerEngineConfiguration>("{}", JsonOptions.Default)!;
        var voice = JsonSerializer.Deserialize<Voice>("{}", JsonOptions.Default)!;
        var connection = JsonSerializer.Deserialize<ConnectingService>("{}", JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(engine.Name, Is.EqualTo("PiperTts"));
            Assert.That(engine.Type, Is.EqualTo("Moba.Sound.PiperSpeechEngine"));
            Assert.That(engine.Settings, Is.Empty);
            Assert.That(voice.Name, Is.EqualTo("ElkeNeural"));
            Assert.That(connection.Name, Is.EqualTo("New Connecting Service"));
        }
    }
}
