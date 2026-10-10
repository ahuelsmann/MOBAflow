// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Multiplex;

[TestFixture]
internal sealed class MultiplexerContractTests
{
    private readonly IMultiplexerProvider _provider = new DefaultMultiplexerProvider();

    // Fixed expectations for the current software contract, not read from the production registry.
    private static IEnumerable<TestCaseData> SignalCommands()
    {
        (string Article, SignalAspect Aspect, int Offset, int Output, bool Activate)[] commands =
        [
            ("4040", SignalAspect.Hp0, 0, 0, false),
            ("4040", SignalAspect.Ks1, 0, 0, true),
            ("4040", SignalAspect.Ks1Blink, 1, 0, true),
            ("4040", SignalAspect.Ks2, 0, 0, false),
            ("4042", SignalAspect.Hp0, 0, 0, false),
            ("4042", SignalAspect.Ks1, 0, 0, true),
            ("4043", SignalAspect.Hp0, 0, 0, false),
            ("4043", SignalAspect.Ks1, 0, 0, true),
            ("4045", SignalAspect.Hp0, 0, 0, false),
            ("4045", SignalAspect.Ks1, 0, 0, true),
            ("4046", SignalAspect.Hp0, 0, 0, true),
            ("4046", SignalAspect.Ks1, 0, 1, true),
            ("4046", SignalAspect.Ra12, 1, 0, true),
            ("4046", SignalAspect.Zs1, 1, 1, true),
            ("4046", SignalAspect.Ks2, 2, 0, true),
            ("4046", SignalAspect.Ks1Blink, 2, 1, true),
            ("4046", SignalAspect.Kennlicht, 3, 0, true),
            ("4046", SignalAspect.Dunkel, 3, 1, true)
        ];

        foreach (var model in new[] { "5229", "52292" })
        {
            foreach (var command in commands)
            {
                yield return new TestCaseData(model, command.Article, command.Aspect,
                    command.Offset, command.Output, command.Activate);
            }
        }
    }

    [TestCaseSource(nameof(SignalCommands))]
    public void Provider_ResolvesExactSignalCommand(
        string model, string article, SignalAspect aspect, int offset, int output, bool activate)
    {
        var found = _provider.TryGetTurnoutCommand(model, article, aspect, out var command);

        Assert.That(found, Is.True);
        Assert.That(command, Is.EqualTo(new MultiplexerTurnoutCommand(offset, output, activate)));
        Assert.That(_provider.SupportsAspect(model, article, aspect), Is.True);
    }

    [Test]
    public void Provider_ListsRegisteredModels()
    {
        Assert.That(_provider.GetSupportedArticles(), Is.EquivalentTo(new[] { "5229", "52292" }));
        Assert.That(_provider.GetAllDefinitions().Select(definition => definition.ArticleNumber),
            Is.EquivalentTo(new[] { "5229", "52292" }));
    }

    [TestCase("5229", "5229 - Multiplexer for light signals", "4040")]
    [TestCase("52292", "52292 - Double multiplexer for 2 light signals", null)]
    public void Provider_ExposesDecoderIdentityAndDefaultSignals(string model, string name, string? distantArticle)
    {
        var definition = _provider.GetDefinition(model);

        Assert.That(definition.DisplayName, Is.EqualTo(name));
        Assert.That(definition.MainSignalArticleNumber, Is.EqualTo("4046"));
        Assert.That(definition.DistantSignalArticleNumber, Is.EqualTo(distantArticle));
    }

    [Test]
    public void Provider_ListsExactSupportedAspects(
        [Values("5229", "52292")] string model,
        [Values("4040", "4042", "4043", "4045", "4046")] string article)
    {
        SignalAspect[] expected = article switch
        {
            "4040" => [SignalAspect.Hp0, SignalAspect.Ks1, SignalAspect.Ks1Blink, SignalAspect.Ks2],
            "4046" => [SignalAspect.Hp0, SignalAspect.Ks1, SignalAspect.Ra12, SignalAspect.Zs1,
                SignalAspect.Ks2, SignalAspect.Ks1Blink, SignalAspect.Kennlicht, SignalAspect.Dunkel],
            _ => [SignalAspect.Hp0, SignalAspect.Ks1]
        };

        Assert.That(_provider.GetSupportedAspects(model, article), Is.EquivalentTo(expected));
    }

    [Test]
    public void Provider_MissingSignalArticleUsesDefaultMainSignal(
        [Values("5229", "52292")] string model,
        [Values(null, "", " \t")] string? article)
    {
        Assert.That(_provider.TryGetTurnoutCommand(model, article, SignalAspect.Ks1, out var command), Is.True);
        Assert.That(command, Is.EqualTo(new MultiplexerTurnoutCommand(0, 1, true)));
        Assert.That(_provider.GetSupportedAspects(model, article), Does.Contain(SignalAspect.Dunkel));
        Assert.That(_provider.TryGetMaxAddressOffset(model, article, out var offset), Is.True);
        Assert.That(offset, Is.EqualTo(3));
    }

    [Test]
    public void Provider_RejectsUnknownSignalAndUnsupportedAspect(
        [Values("5229", "52292")] string model,
        [Values("unknown", "4042")] string article)
    {
        Assert.That(_provider.TryGetTurnoutCommand(model, article, SignalAspect.Dunkel, out var command), Is.False);
        Assert.That(command, Is.EqualTo(default(MultiplexerTurnoutCommand)));
        Assert.That(_provider.SupportsAspect(model, article, SignalAspect.Dunkel), Is.False);
        Assert.That(_provider.GetSupportedAspects(model, article), Does.Not.Contain(SignalAspect.Dunkel));
    }

    [Test]
    public void Provider_ReturnsExactLargestAddressOffset(
        [Values("5229", "52292")] string model,
        [Values("4040", "4042", "4043", "4045", "4046")] string article)
    {
        var expected = article switch { "4040" => 1, "4046" => 3, _ => 0 };

        Assert.That(_provider.TryGetMaxAddressOffset(model, article, out var offset), Is.True);
        Assert.That(offset, Is.EqualTo(expected));
    }

    [TestCase("5229", "unknown")]
    [TestCase("unknown", "4046")]
    [TestCase("", "4046")]
    [TestCase(" \t", "4046")]
    public void Provider_MissingMappingHasNoAddressOffset(string model, string article)
    {
        Assert.That(_provider.TryGetMaxAddressOffset(model, article, out var offset), Is.False);
        Assert.That(offset, Is.Zero);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("unknown")]
    public void Definition_WithoutDefaultMappingReturnsEmptyResults(string? defaultArticle)
    {
        var definition = new MultiplexerDefinition { MainSignalArticleNumber = defaultArticle! };

        Assert.That(definition.TryGetTurnoutCommand(null, SignalAspect.Hp0, out var command), Is.False);
        Assert.That(command, Is.EqualTo(default(MultiplexerTurnoutCommand)));
        Assert.That(definition.GetSupportedAspects(null), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void Provider_RejectsMissingDecoderArticleWithParameterName(string? article)
    {
        var exception = Assert.Throws<ArgumentException>(() => _provider.GetDefinition(article!));

        Assert.That(exception!.ParamName, Is.EqualTo("articleNumber"));
        Assert.That(_provider.SupportsAspect(article!, "4046", SignalAspect.Hp0), Is.False);
    }

    [Test]
    public void MainSignalOptions_ExcludeDistantSignalAndPutDefaultFirst([Values("5229", "52292")] string model)
    {
        var options = MultiplexerHelper.GetMainSignalOptions(model);

        Assert.That(options.Select(option => option.ArticleNumber),
            Is.EqualTo(new[] { "4046", "4042", "4043", "4045" }));
        Assert.That(options.Select(option => option.DisplayName), Is.EqualTo(new[]
        {
            "4046 - Ks-Ausfahrsignal (Mehrbereich)", "4042 - Ks-Einfahrsignal",
            "4043 - Ks-Ausfahrsignal", "4045 - Ks-Einfahrsignal (Mehrbereich)"
        }));
    }

    [Test]
    public void DistantSignalOptions_5229OffersOnlyDistantSignal()
    {
        Assert.That(MultiplexerHelper.GetDistantSignalOptions("5229"),
            Is.EqualTo(new[] { ("4040", "4040 - Ks-Vorsignal") }));
    }

    [Test]
    public void DistantSignalOptions_DoubleDecoderOffersNoDistantSignal()
    {
        Assert.That(MultiplexerHelper.GetDistantSignalOptions("52292"), Is.Empty);
    }
}
