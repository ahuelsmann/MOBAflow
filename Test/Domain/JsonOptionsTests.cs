// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using System.Text.Json;
using System.Text.Json.Serialization;

[TestFixture]
internal sealed class JsonOptionsTests
{
    [Test]
    public void Default_HasExpectedSerializationBehavior()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonOptions.Default.WriteIndented, Is.True);
            Assert.That(JsonOptions.Default.DefaultIgnoreCondition, Is.EqualTo(JsonIgnoreCondition.WhenWritingNull));
            Assert.That(JsonOptions.Default.PropertyNameCaseInsensitive, Is.True);
            Assert.That(JsonOptions.Default.PropertyNamingPolicy, Is.SameAs(JsonNamingPolicy.CamelCase));
        });
    }

    [Test]
    public void Compact_HasExpectedSerializationBehavior()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonOptions.Compact.WriteIndented, Is.False);
            Assert.That(JsonOptions.Compact.DefaultIgnoreCondition, Is.EqualTo(JsonIgnoreCondition.WhenWritingNull));
            Assert.That(JsonOptions.Compact.PropertyNameCaseInsensitive, Is.True);
            Assert.That(JsonOptions.Compact.PropertyNamingPolicy, Is.SameAs(JsonNamingPolicy.CamelCase));
        });
    }

    [Test]
    public void InitializeConverters_AddsConverterToBothOptionSets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonOptions.Default.Converters, Does.Contain(JsonOptionsTestSetup.Converter));
            Assert.That(JsonOptions.Compact.Converters, Does.Contain(JsonOptionsTestSetup.Converter));
        });
    }
}
