// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test;

using System.Text.Json;
using System.Text.Json.Serialization;

[SetUpFixture]
internal sealed class JsonOptionsTestSetup
{
    internal static readonly JsonConverter Converter = new TestPayloadConverter();

    [OneTimeSetUp]
    public void RegisterTestConverter()
    {
        JsonOptions.InitializeConverters([Converter]);
    }

    private sealed class TestPayload;

    private sealed class TestPayloadConverter : JsonConverter<TestPayload>
    {
        public override TestPayload? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(
            Utf8JsonWriter writer,
            TestPayload value,
            JsonSerializerOptions options) => throw new NotSupportedException();
    }
}
