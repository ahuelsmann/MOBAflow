// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Multiplex;
using Moba.Domain;

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
    public void GetDefinition_EmptyArticle_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MultiplexerHelper.GetDefinition(""));
    }

    [Test]
    [TestCase("4046", SignalAspect.Hp0, 0, 1, false)]
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
        Assert.That(definitions, Has.Count.GreaterThanOrEqualTo(2));
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
}
