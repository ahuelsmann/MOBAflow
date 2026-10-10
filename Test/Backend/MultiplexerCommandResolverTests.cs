// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Multiplex;
using Moba.Domain;

/// <summary>
/// Unit tests for resolving Viessmann multiplexer signal aspects into Z21 turnout commands.
/// </summary>
[TestFixture]
internal sealed class MultiplexerCommandResolverTests
{
    [Test]
    [TestCase(201, "5229", "4046", SignalAspect.Hp0, 201, 0, true)]
    [TestCase(201, "5229", "4046", SignalAspect.Ks1, 201, 1, true)]
    [TestCase(201, "5229", "4046", SignalAspect.Ra12, 202, 0, true)]
    [TestCase(201, "5229", "4046", SignalAspect.Ks1Blink, 203, 1, true)]
    public void Resolve_ShouldReturnExpectedCommand(
        int baseAddress,
        string multiplexerArticle,
        string signalArticle,
        SignalAspect aspect,
        int expectedDccAddress,
        int expectedOutput,
        bool expectedActivate)
    {
        var command = MultiplexerCommandResolver.Resolve(baseAddress, multiplexerArticle, signalArticle, aspect);

        Assert.That(command.DccAddress, Is.EqualTo(expectedDccAddress));
        Assert.That(command.Output, Is.EqualTo(expectedOutput));
        Assert.That(command.Activate, Is.EqualTo(expectedActivate));
    }

    [Test]
    public void Resolve_ShouldInvertActivate_WhenSignalBoxSettingsInvertOffset()
    {
        var settings = new SignalBoxSettings
        {
            InvertPolarityOffset2 = true
        };

        var command = MultiplexerCommandResolver.Resolve(201, "5229", "4046", SignalAspect.Ks2, settings);

        Assert.That(command.AddressOffset, Is.EqualTo(2));
        Assert.That(command.OriginalActivate, Is.True);
        Assert.That(command.Activate, Is.False);
    }

    [Test]
    public void Resolve_ShouldSupportEveryAspectAdvertisedByMultiplexerHelper()
    {
        foreach (var definition in MultiplexerHelper.GetAllDefinitions())
        {
            foreach (var signalArticle in definition.SignalAspectCommandsBySignalArticle.Keys)
            {
                foreach (var aspect in MultiplexerHelper.GetSupportedAspects(definition.ArticleNumber, signalArticle))
                {
                    var command = MultiplexerCommandResolver.Resolve(201, definition.ArticleNumber, signalArticle, aspect);

                    Assert.That(command.DccAddress, Is.InRange(201, 204));
                    Assert.That(command.Output, Is.InRange(0, 1));
                    Assert.That(command.AddressOffset, Is.InRange(0, 3));
                }
            }
        }
    }

    [Test]
    public void Resolve_ShouldUseDefaultMainSignalArticle_WhenSignalArticleNumberIsNull()
    {
        var command = MultiplexerCommandResolver.Resolve(201, "5229", null, SignalAspect.Ks1);

        Assert.That(command.DccAddress, Is.EqualTo(201));
        Assert.That(command.Output, Is.EqualTo(1));
        Assert.That(command.Activate, Is.True);
    }

    [Test]
    [TestCase(0, SignalAspect.Hp0)]
    [TestCase(1, SignalAspect.Ra12)]
    [TestCase(2, SignalAspect.Ks2)]
    [TestCase(3, SignalAspect.Kennlicht)]
    public void Resolve_ShouldInvertOnlyConfiguredOffset(int offset, SignalAspect aspect)
    {
        var settings = new SignalBoxSettings
        {
            InvertPolarityOffset0 = offset == 0,
            InvertPolarityOffset1 = offset == 1,
            InvertPolarityOffset2 = offset == 2,
            InvertPolarityOffset3 = offset == 3
        };

        var command = MultiplexerCommandResolver.Resolve(201, "5229", "4046", aspect, settings);

        Assert.That(command.AddressOffset, Is.EqualTo(offset));
        Assert.That(command.Activate, Is.EqualTo(!command.OriginalActivate));
    }

    [Test]
    public void Resolve_ShouldThrow_WhenBaseAddressIsEven()
    {
        Assert.Throws<ArgumentException>(() =>
            MultiplexerCommandResolver.Resolve(202, "5229", "4046", SignalAspect.Hp0));
    }

    [Test]
    public void Resolve_ShouldThrow_WhenBaseAddressPlusOffsetExceedsDccLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MultiplexerCommandResolver.Resolve(2043, "5229", "4046", SignalAspect.Kennlicht));
    }

    [Test]
    public void Resolve_ShouldAllowHighestBaseAddressThatKeepsOffsetWithinDccLimit()
    {
        var command = MultiplexerCommandResolver.Resolve(2041, "5229", "4046", SignalAspect.Kennlicht);

        Assert.That(command.DccAddress, Is.EqualTo(2044));
    }

    [Test]
    public void Resolve_ShouldThrow_WhenMultiplexerIsUnknown()
    {
        Assert.Throws<ArgumentException>(() =>
            MultiplexerCommandResolver.Resolve(201, "9999", "4046", SignalAspect.Hp0));
    }

    [Test]
    public void Resolve_ShouldThrow_WhenAspectIsUnsupported()
    {
        Assert.Throws<ArgumentException>(() =>
            MultiplexerCommandResolver.Resolve(201, "5229", "4042", SignalAspect.Zs1));
    }

    [TestCase(1, "4046", SignalAspect.Hp0, 1, 0, true)]
    [TestCase(2041, "4046", SignalAspect.Dunkel, 2044, 1, true)]
    [TestCase(2043, "4040", SignalAspect.Ks1Blink, 2044, 0, true)]
    [TestCase(2043, "4042", SignalAspect.Hp0, 2043, 0, false)]
    public void Resolve_ValidAddressBoundariesPreserveCommand(
        int baseAddress, string article, SignalAspect aspect, int address, int output, bool activate)
    {
        var command = MultiplexerCommandResolver.Resolve(baseAddress, "5229", article, aspect);

        Assert.That(command.DccAddress, Is.EqualTo(address));
        Assert.That(command.AddressOffset, Is.EqualTo(address - baseAddress));
        Assert.That(command.Output, Is.EqualTo(output));
        Assert.That(command.Activate, Is.EqualTo(activate));
        Assert.That(command.OriginalActivate, Is.EqualTo(activate));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(2045)]
    [TestCase(int.MaxValue)]
    public void Resolve_RejectsBaseAddressOutsideDccRange(int baseAddress)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MultiplexerCommandResolver.Resolve(baseAddress, "5229", "4042", SignalAspect.Hp0));

        Assert.That(exception!.ParamName, Is.EqualTo("baseAddress"));
    }

    [Test]
    public void Resolve_RejectsOverflowEvenWhenSelectedAspectUsesBaseAddress()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MultiplexerCommandResolver.Resolve(2043, "5229", "4046", SignalAspect.Hp0));
    }

    [Test]
    public void Resolve_InvertedInactiveCommandActivatesAndKeepsOriginalValue()
    {
        var settings = new SignalBoxSettings { InvertPolarityOffset0 = true };

        var command = MultiplexerCommandResolver.Resolve(201, "5229", "4042", SignalAspect.Hp0, settings);

        Assert.That(command.DccAddress, Is.EqualTo(201));
        Assert.That(command.Output, Is.Zero);
        Assert.That(command.OriginalActivate, Is.False);
        Assert.That(command.Activate, Is.True);
    }

    [Test]
    public void Resolve_InversionForOtherOffsetDoesNotChangeSelectedCommand()
    {
        var settings = new SignalBoxSettings { InvertPolarityOffset0 = true };

        var command = MultiplexerCommandResolver.Resolve(201, "5229", "4046", SignalAspect.Dunkel, settings);

        Assert.That(command.DccAddress, Is.EqualTo(204));
        Assert.That(command.Output, Is.EqualTo(1));
        Assert.That(command.AddressOffset, Is.EqualTo(3));
        Assert.That(command.OriginalActivate, Is.True);
        Assert.That(command.Activate, Is.True);
    }
}
