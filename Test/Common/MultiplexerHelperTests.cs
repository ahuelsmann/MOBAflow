// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Multiplex;

/// <summary>
/// Unit tests for MultiplexerHelper and MultiplexerDefinition.
/// Tests address calculation and aspect mapping for supported multiplexers.
/// </summary>
[TestFixture]
internal class MultiplexerHelperTests
{
    [Test]
    public void GetDefinition_5229_ShouldReturnCorrectDefinition()
    {
        // Act
        var def = MultiplexerHelper.GetDefinition("5229");

        // Assert
        Assert.That(def, Is.Not.Null);
        Assert.That(def.ArticleNumber, Is.EqualTo("5229"));
        Assert.That(def.MainSignalCount, Is.EqualTo(1));
        Assert.That(def.AddressesPerSignal, Is.EqualTo(4));
        Assert.That(def.MainSignalArticleNumber, Is.EqualTo("4046"));
        Assert.That(def.DistantSignalArticleNumber, Is.EqualTo("4040"));
    }

    [Test]
    public void GetDefinition_52292_ShouldReturnCorrectDefinition()
    {
        // Act
        var def = MultiplexerHelper.GetDefinition("52292");

        // Assert
        Assert.That(def, Is.Not.Null);
        Assert.That(def.ArticleNumber, Is.EqualTo("52292"));
        Assert.That(def.MainSignalCount, Is.EqualTo(2));
        Assert.That(def.AddressesPerSignal, Is.EqualTo(4));
    }

    [Test]
    public void GetDefinition_UnknownArticle_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MultiplexerHelper.GetDefinition("9999"));
    }

    [Test]
    public void TryGetMaxAddressOffset_5229_4046_ShouldReturn3()
    {
        var ok = MultiplexerHelper.TryGetMaxAddressOffset("5229", "4046", out var max);

        Assert.That(ok, Is.True);
        Assert.That(max, Is.EqualTo(3));
    }

    [Test]
    public void TryGetMaxAddressOffset_5229_4042_ShouldReturn0()
    {
        var ok = MultiplexerHelper.TryGetMaxAddressOffset("5229", "4042", out var max);

        Assert.That(ok, Is.True);
        Assert.That(max, Is.EqualTo(0));
    }

    [Test]
    public void GetDefinition_EmptyArticle_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MultiplexerHelper.GetDefinition(""));
    }

    [Test]
    [TestCase("4046", SignalAspect.Hp0, 0, 0, true)]
    [TestCase("4046", SignalAspect.Ks1, 0, 1, true)]
    [TestCase("4046", SignalAspect.Ks1Blink, 2, 1, true)]
    [TestCase("4046", SignalAspect.Ra12, 1, 0, true)]
    [TestCase("4046", SignalAspect.Ks2, 2, 0, true)]
    [TestCase("4040", SignalAspect.Ks2, 0, 0, false)]
    [TestCase("4040", SignalAspect.Ks1, 0, 0, true)]
    [TestCase("4040", SignalAspect.Ks1Blink, 1, 0, true)]
    public void TryGetTurnoutCommand_5229_ShouldReturnExpectedMapping(
        string signalArticleNumber,
        SignalAspect aspect,
        int expectedOffset,
        int expectedOutput,
        bool expectedActivate)
    {
        // Act
        var result = MultiplexerHelper.TryGetTurnoutCommand("5229", signalArticleNumber, aspect, out var command);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(command.AddressOffset, Is.EqualTo(expectedOffset));
        Assert.That(command.Output, Is.EqualTo(expectedOutput));
        Assert.That(command.Activate, Is.EqualTo(expectedActivate));
    }

    [Test]
    public void TryGetTurnoutCommand_5229_4046_Zs1_ShouldReturnExpectedMapping()
    {
        // Act
        var result = MultiplexerHelper.TryGetTurnoutCommand("5229", "4046", SignalAspect.Zs1, out var command);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(command.AddressOffset, Is.EqualTo(1));
        Assert.That(command.Output, Is.EqualTo(1));
        Assert.That(command.Activate, Is.True);
    }

    [Test]
    public void SupportsAspect_5229_Ks1_ShouldReturnTrue()
    {
        // Act
        bool supports = MultiplexerHelper.SupportsAspect("5229", "4046", SignalAspect.Ks1);

        // Assert
        Assert.That(supports, Is.True);
    }

    [Test]
    public void SupportsAspect_5229_4046_Zs1_ShouldReturnTrue()
    {
        // Act
        bool supports = MultiplexerHelper.SupportsAspect("5229", "4046", SignalAspect.Zs1);

        // Assert
        Assert.That(supports, Is.True);
    }

    [Test]
    public void SupportsAspect_InvalidMultiplexer_ShouldReturnFalse()
    {
        // Act
        bool supports = MultiplexerHelper.SupportsAspect("9999", "4046", SignalAspect.Ks1);

        // Assert
        Assert.That(supports, Is.False);
    }

    [Test]
    public void GetAllDefinitions_ShouldReturnAtLeastTwoDefinitions()
    {
        // Act
        var definitions = MultiplexerHelper.GetAllDefinitions();

        // Assert
        Assert.That(definitions.Count(), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GetSupportedArticles_ShouldInclude5229And52292()
    {
        // Act
        var articles = MultiplexerHelper.GetSupportedArticles().ToList();

        // Assert
        Assert.That(articles, Contains.Item("5229"));
        Assert.That(articles, Contains.Item("52292"));
    }

    /// <summary>
    /// Regression test: No aspect for signal 4046 should ever map to Activate=false,
    /// as deactivate-only commands do not switch the Viessmann multiplexer.
    /// </summary>
    [Test]
    public void TryGetTurnoutCommand_5229_4046_NoAspectIsPureDeactivate()
    {
        var aspects = MultiplexerHelper.GetSupportedAspects("5229", "4046");
        Assert.That(aspects, Is.Not.Empty);

        foreach (var aspect in aspects)
        {
            var result = MultiplexerHelper.TryGetTurnoutCommand("5229", "4046", aspect, out var command);
            Assert.That(result, Is.True, $"Aspect {aspect} should have a mapping");
            Assert.That(command.Activate, Is.True,
                $"Aspect {aspect} must use Activate=true (found Activate=false)");
        }
    }

    /// <summary>
    /// Hp0 and Ks1 share the same DCC address (Offset 0) but use distinct outputs.
    /// This test ensures both are activation commands on different outputs.
    /// </summary>
    [Test]
    public void TryGetTurnoutCommand_5229_4046_Hp0AndKs1_UseDistinctOutputs_BothActivate()
    {
        var hp0Result = MultiplexerHelper.TryGetTurnoutCommand("5229", "4046", SignalAspect.Hp0, out var hp0Cmd);
        var ks1Result = MultiplexerHelper.TryGetTurnoutCommand("5229", "4046", SignalAspect.Ks1, out var ks1Cmd);

        Assert.That(hp0Result, Is.True);
        Assert.That(ks1Result, Is.True);

        Assert.That(hp0Cmd.AddressOffset, Is.EqualTo(ks1Cmd.AddressOffset), "Hp0 and Ks1 must share the same offset");
        Assert.That(hp0Cmd.Output, Is.Not.EqualTo(ks1Cmd.Output), "Hp0 and Ks1 must use different outputs");
        Assert.That(hp0Cmd.Activate, Is.True, "Hp0 must use Activate=true");
        Assert.That(ks1Cmd.Activate, Is.True, "Ks1 must use Activate=true");
    }
}
