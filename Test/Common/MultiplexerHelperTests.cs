// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Multiplex;
using Moba.Domain;

/// <summary>
/// Unit tests for MultiplexerHelper and MultiplexerDefinition.
/// Tests address calculation and aspect mapping for supported multiplexers.
/// </summary>
[TestFixture]
public class MultiplexerHelperTests
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
    public void GetAddressForAspect_5229_Hp0_ShouldReturn201()
    {
        // Act
        int address = MultiplexerHelper.GetAddressForAspect("5229", 201, SignalAspect.Hp0);

        // Assert
        Assert.That(address, Is.EqualTo(201));
    }

    [Test]
    public void GetAddressForAspect_5229_Ks1_ShouldReturn202()
    {
        // Act
        int address = MultiplexerHelper.GetAddressForAspect("5229", 201, SignalAspect.Ks1);

        // Assert
        Assert.That(address, Is.EqualTo(202));
    }

    [Test]
    public void GetAddressForAspect_5229_Ks2_ShouldReturn203()
    {
        // Act
        int address = MultiplexerHelper.GetAddressForAspect("5229", 201, SignalAspect.Ks2);

        // Assert
        Assert.That(address, Is.EqualTo(203));
    }

    [Test]
    public void GetAddressForAspect_5229_Ks1Blink_ShouldReturn204()
    {
        // Act
        int address = MultiplexerHelper.GetAddressForAspect("5229", 201, SignalAspect.Ks1Blink);

        // Assert
        Assert.That(address, Is.EqualTo(204));
    }

    [Test]
    [TestCase(100, SignalAspect.Hp0, 100)]
    [TestCase(100, SignalAspect.Ks1, 101)]
    [TestCase(100, SignalAspect.Ks2, 102)]
    [TestCase(100, SignalAspect.Ks1Blink, 103)]
    [TestCase(500, SignalAspect.Hp0, 500)]
    [TestCase(500, SignalAspect.Ks1, 501)]
    public void GetAddressForAspect_VariousBaseAddresses_ShouldCalculateCorrectly(
        int baseAddress,
        SignalAspect aspect,
        int expectedAddress)
    {
        // Act
        int address = MultiplexerHelper.GetAddressForAspect("5229", baseAddress, aspect);

        // Assert
        Assert.That(address, Is.EqualTo(expectedAddress));
    }

    [Test]
    public void GetCommandValue_5229_Hp0_ShouldReturn0()
    {
        // Act
        int value = MultiplexerHelper.GetCommandValue("5229", SignalAspect.Hp0);

        // Assert
        Assert.That(value, Is.EqualTo(0));
    }

    [Test]
    public void GetCommandValue_5229_Ks1_ShouldReturn1()
    {
        // Act
        int value = MultiplexerHelper.GetCommandValue("5229", SignalAspect.Ks1);

        // Assert
        Assert.That(value, Is.EqualTo(1));
    }

    [Test]
    public void GetCommandValue_5229_Ks2_ShouldReturn2()
    {
        // Act
        int value = MultiplexerHelper.GetCommandValue("5229", SignalAspect.Ks2);

        // Assert
        Assert.That(value, Is.EqualTo(2));
    }

    [Test]
    public void GetCommandValue_5229_Ks1Blink_ShouldReturn3()
    {
        // Act
        int value = MultiplexerHelper.GetCommandValue("5229", SignalAspect.Ks1Blink);

        // Assert
        Assert.That(value, Is.EqualTo(3));
    }

    [Test]
    public void SupportsAspect_5229_Ks1_ShouldReturnTrue()
    {
        // Act
        bool supports = MultiplexerHelper.SupportsAspect("5229", SignalAspect.Ks1);

        // Assert
        Assert.That(supports, Is.True);
    }

    [Test]
    public void SupportsAspect_5229_UnsupportedAspect_ShouldReturnFalse()
    {
        // Act
        bool supports = MultiplexerHelper.SupportsAspect("5229", SignalAspect.Ra12);

        // Assert
        Assert.That(supports, Is.False);
    }

    [Test]
    public void SupportsAspect_InvalidMultiplexer_ShouldReturnFalse()
    {
        // Act
        bool supports = MultiplexerHelper.SupportsAspect("9999", SignalAspect.Ks1);

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
        var articles = MultiplexerHelper.GetSupportedArticles();

        // Assert
        Assert.That(articles, Contains.Item("5229"));
        Assert.That(articles, Contains.Item("52292"));
    }

    [Test]
    public void MultiplexerDefinition_GetAddressForAspect_5229_ShouldWork()
    {
        // Arrange
        var def = MultiplexerHelper.GetDefinition("5229");

        // Act
        int address = def.GetAddressForAspect(201, SignalAspect.Ks1);

        // Assert
        Assert.That(address, Is.EqualTo(202));
    }

    [Test]
    public void MultiplexerDefinition_GetCommandValue_ShouldReturnAspectOffset()
    {
        // Arrange
        var def = MultiplexerHelper.GetDefinition("5229");

        // Act
        int value = def.GetCommandValue(SignalAspect.Ks2);

        // Assert
        Assert.That(value, Is.EqualTo(2));
    }
}
