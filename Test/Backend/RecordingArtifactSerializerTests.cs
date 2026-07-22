// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Service.Recording;
using Moba.Common.Recording;
using System.Text;
using System.Text.Json;

[TestFixture]
[Category("Unit")]
internal sealed class RecordingArtifactSerializerTests
{
    [Test]
    public void Serialize_Should_MatchGoldenFile()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var expectedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Recording", "recording-v1-golden.json");
        var expected = File.ReadAllText(expectedPath).ReplaceLineEndings("\n").TrimEnd();

        // Act
        var actual = serializer.Serialize(CreateGoldenArtifact());

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Serialize_Should_BeByteForByteDeterministicAndCanonicalizePayloadProperties()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var artifact = CreateArtifact("unknown.future.event", """{"z":1,"a":2}""", RecordingReplayApplicability.DisplayOnly);

        // Act
        var first = serializer.SerializeToUtf8(artifact);
        var second = serializer.SerializeToUtf8(artifact);
        var json = Encoding.UTF8.GetString(first);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(json, Does.Contain("\"payload\": {\n        \"a\": 2,\n        \"z\": 1\n      }"));
        });
    }

    [Test]
    public void Serialize_Should_NormalizeEquivalentPayloadNumbers()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var integerArtifact = CreateArtifact("unknown.future.event", """{"value":1}""", RecordingReplayApplicability.DisplayOnly);
        var decimalArtifact = CreateArtifact("unknown.future.event", """{"value":1.0}""", RecordingReplayApplicability.DisplayOnly);

        // Act
        var integerJson = serializer.SerializeToUtf8(integerArtifact);
        var decimalJson = serializer.SerializeToUtf8(decimalArtifact);

        // Assert
        Assert.That(decimalJson, Is.EqualTo(integerJson));
    }

    [Test]
    public void Import_Should_RoundTripKnownReplayApplicableEntry()
    {
        // Arrange
        var validator = new RequiredStringPayloadValidator("runtime.state.changed", "state", RecordingReplayApplicability.ReplayApplicable);
        var serializer = new RecordingArtifactSerializer([validator]);
        var artifact = CreateArtifact("runtime.state.changed", """{"state":"ready"}""", RecordingReplayApplicability.ReplayApplicable);
        var serialized = serializer.SerializeToUtf8(artifact);

        // Act
        var result = serializer.Import(serialized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Artifact, Is.Not.Null);
            Assert.That(result.Artifact!.Entries.Select(entry => entry.Sequence), Is.EqualTo(new long[] { 1 }));
            Assert.That(result.Artifact.Entries[0].ReplayApplicability, Is.EqualTo(RecordingReplayApplicability.ReplayApplicable));
            Assert.That(serializer.SerializeToUtf8(result.Artifact), Is.EqualTo(serialized));
        });
    }

    [Test]
    public void Import_Should_PreserveUnknownTypeAsDisplayOnly()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.SerializeToUtf8(
            CreateArtifact("future.signal.aspect", """{"aspect":"x1"}""", RecordingReplayApplicability.DisplayOnly));

        // Act
        var result = serializer.Import(serialized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Artifact!.Entries[0].TypeKey, Is.EqualTo("future.signal.aspect"));
            Assert.That(result.Artifact.Entries[0].ReplayApplicability, Is.EqualTo(RecordingReplayApplicability.DisplayOnly));
        });
    }

    [Test]
    public void Import_Should_RejectMalformedKnownPayloadWithPrecisePath()
    {
        // Arrange
        var validator = new RequiredStringPayloadValidator("runtime.state.changed", "state", RecordingReplayApplicability.DisplayOnly);
        var serializer = new RecordingArtifactSerializer([validator]);
        var serialized = serializer.SerializeToUtf8(
            CreateArtifact("runtime.state.changed", "{}", RecordingReplayApplicability.DisplayOnly));

        // Act
        var result = serializer.Import(serialized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("invalid-known-payload"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.entries[0].payload"));
            Assert.That(result.Errors.Single().Message, Does.Contain("state"));
        });
    }

    [Test]
    public void Import_Should_RejectDuplicateRootProperty()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("\"formatVersion\"", "\"format\": \"duplicate\",\n  \"formatVersion\"", StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("duplicate-property"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.format"));
        });
    }

    [Test]
    public void Import_Should_RejectUnknownEnvelopeProperty()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("\"formatVersion\"", "\"unexpected\": true,\n  \"formatVersion\"", StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("unknown-property"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.unexpected"));
        });
    }

    [Test]
    public void Import_Should_RejectJsonBeyondDepthLimit()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var nestedPayload = $"{{\"nested\":{new string('[', 40)}0{new string(']', 40)}}}";
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("{\n        \"label\": \"Start\"\n      }", nestedPayload, StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.That(result.Errors.Single().Code, Is.EqualTo("invalid-json"));
    }

    [Test]
    public void Import_Should_RejectOversizePayloadBeforeIntegrityValidation()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("\"Start\"", $"\"{new string('x', RecordingFormat.MaxPayloadBytes + 1)}\"", StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("payload-too-large"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.entries[0].payload"));
        });
    }

    [Test]
    public void Import_Should_RejectPayloadTotalBeyondDeclaredLimit()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace(
                $"\"estimatedPayloadByteLimit\": {RecordingFormat.DefaultMaxArtifactBytes}",
                "\"estimatedPayloadByteLimit\": 8",
                StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("payload-total-limit"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.entries"));
        });
    }

    [Test]
    public void Import_Should_RejectInvalidSequenceBeforeIntegrityValidation()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("\"sequence\": 1", "\"sequence\": 0", StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("invalid-sequence"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.entries[0].sequence"));
        });
    }

    [Test]
    public void Import_Should_RejectIntegrityMismatch()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("\"displayText\": \"Start\"", "\"displayText\": \"Changed\"", StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Single().Code, Is.EqualTo("integrity-mismatch"));
            Assert.That(result.Errors.Single().Path, Is.EqualTo("$.integrity.entriesSha256"));
        });
    }

    [Test]
    public void Import_Should_RejectUnsupportedVersion()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var serialized = serializer.Serialize(CreateGoldenArtifact())
            .Replace("\"formatVersion\": \"1.0\"", "\"formatVersion\": \"1.1\"", StringComparison.Ordinal);

        // Act
        var result = serializer.Import(Encoding.UTF8.GetBytes(serialized));

        // Assert
        Assert.That(result.Errors.Single().Code, Is.EqualTo("unsupported-version"));
    }

    [Test]
    public void Import_Should_RejectArtifactBeyondConfiguredLimitBeforeParsing()
    {
        // Arrange
        var writer = new RecordingArtifactSerializer();
        var serialized = writer.SerializeToUtf8(CreateGoldenArtifact());
        var serializer = new RecordingArtifactSerializer(importLimits: new RecordingArtifactImportLimits(10, 128));

        // Act
        var result = serializer.Import(serialized);

        // Assert
        Assert.That(result.Errors.Single().Code, Is.EqualTo("artifact-too-large"));
    }

    [Test]
    public void Import_Should_RejectMalformedMutationCorpusWithoutThrowing()
    {
        // Arrange
        var serializer = new RecordingArtifactSerializer();
        var validArtifact = serializer.SerializeToUtf8(CreateGoldenArtifact());
        var mutations = new (string Name, byte[] Content)[]
        {
            ("empty", []),
            ("whitespace", Encoding.UTF8.GetBytes(" \r\n\t")),
            ("null-root", Encoding.UTF8.GetBytes("null")),
            ("array-root", Encoding.UTF8.GetBytes("[]")),
            ("opening-brace-only", validArtifact[..1]),
            ("truncated-halfway", validArtifact[..(validArtifact.Length / 2)]),
            ("invalid-utf8", [0xc3, 0x28]),
            ("trailing-data", [.. validArtifact, 0x00])
        };

        foreach (var mutation in mutations)
        {
            // Act
            var result = serializer.Import(mutation.Content);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False, mutation.Name);
                Assert.That(result.Artifact, Is.Null, mutation.Name);
                Assert.That(result.Errors, Is.Not.Empty, mutation.Name);
            });
        }
    }

    private static RecordingArtifact CreateGoldenArtifact(RecordingArtifactOptions? options = null)
    {
        using var payload = JsonDocument.Parse("""{"label":"Start"}""");
        var entry = new RecordingEntry(
            1,
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            TimeSpan.Zero,
            "recorder",
            "operator",
            "recorder.marker",
            "information",
            null,
            [new RecordingEntityReference("locomotive", Guid.Parse("11111111-1111-1111-1111-111111111111"))],
            payload.RootElement,
            "Start",
            RecordingReplayApplicability.DisplayOnly);
        return new RecordingArtifact(
            new RecordingSessionMetadata(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Golden session",
                new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 2, 3, 5, 5, TimeSpan.Zero)),
            "1.2.3",
            new RecordingProjectIdentity(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Demo"),
            options ?? new RecordingArtifactOptions(),
            [entry]);
    }

    private static RecordingArtifact CreateArtifact(
        string typeKey,
        string payloadJson,
        RecordingReplayApplicability replayApplicability)
    {
        using var payload = JsonDocument.Parse(payloadJson);
        var entry = new RecordingEntry(
            1,
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            TimeSpan.Zero,
            "runtime",
            "test",
            typeKey,
            "information",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            [],
            payload.RootElement,
            "State changed",
            replayApplicability);
        return new RecordingArtifact(
            new RecordingSessionMetadata(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Round trip",
                new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 2, 3, 5, 5, TimeSpan.Zero)),
            "1.2.3",
            null,
            new RecordingArtifactOptions(),
            [entry]);
    }

    private sealed class RequiredStringPayloadValidator(
        string typeKey,
        string propertyName,
        RecordingReplayApplicability replayApplicability) : IRecordingPayloadValidator
    {
        public string TypeKey { get; } = typeKey;

        public RecordingReplayApplicability ReplayApplicability { get; } = replayApplicability;

        public RecordingPayloadValidationResult Validate(JsonElement payload)
        {
            return payload.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? RecordingPayloadValidationResult.Success()
                : RecordingPayloadValidationResult.Failure($"Payload requires string property '{propertyName}'.");
        }
    }
}