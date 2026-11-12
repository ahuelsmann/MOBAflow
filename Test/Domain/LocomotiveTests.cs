// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;

/// <summary>
/// The F0-F31 function range and the stored function symbols and colours of a locomotive.
/// </summary>
[TestFixture]
internal sealed class LocomotiveTests
{
    private static readonly string[] SymbolAtIndexTwo = ["", "", "horn.png"];
    private static readonly string[] ClearedAtIndexOne = ["", "none"];
    private static readonly bool[] OnlyFirstAccepted = [true, false, false];
    private static readonly string[] LightSymbol = ["light.png"];
    private static readonly string[] GreenColour = ["#06D6A0"];
    private static readonly string[] ShortListGrownToIndexThree = ["bell.png", "", "", "horn.png"];
    private static readonly string[] ClearedColourAtIndexZero = ["none", "#06D6A0"];

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(31, true)]
    [TestCase(32, false)]
    public void IsFunctionIndex_AcceptsF0ThroughF31(int functionIndex, bool expected)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Locomotive.IsFunctionIndex(functionIndex), Is.EqualTo(expected));
            Assert.That(Locomotive.MaxFunctionIndex, Is.EqualTo(31));
        }
    }

    [Test]
    public void SetFunctionSymbol_GrowsTheListAndRejectsInvalidInput()
    {
        var locomotive = new Locomotive();

        var set = locomotive.SetFunctionSymbol(2, "horn.png");
        var emptySymbol = locomotive.SetFunctionSymbol(1, string.Empty);
        var badIndex = locomotive.SetFunctionSymbol(32, "horn.png");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new[] { set, emptySymbol, badIndex }, Is.EqualTo(OnlyFirstAccepted));
            Assert.That(locomotive.FunctionSymbols, Is.EqualTo(SymbolAtIndexTwo));
        }
    }

    [Test]
    public void SetFunctionSymbol_GrowsAShortListAndKeepsItsEntries()
    {
        var locomotive = new Locomotive { FunctionSymbols = ["bell.png"] };

        var set = locomotive.SetFunctionSymbol(3, "horn.png");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(set, Is.True);
            Assert.That(locomotive.FunctionSymbols, Is.EqualTo(ShortListGrownToIndexThree));
        }
    }

    [Test]
    public void SetFunctionSymbol_AcceptsF31AndRejectsIndexesOutsideTheRange()
    {
        var locomotive = new Locomotive();

        var last = locomotive.SetFunctionSymbol(Locomotive.MaxFunctionIndex, "horn.png");
        var negative = locomotive.SetFunctionSymbol(-1, "horn.png");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(last, Is.True);
            Assert.That(negative, Is.False);
            Assert.That(locomotive.FunctionSymbols, Has.Count.EqualTo(32));
            Assert.That(locomotive.FunctionSymbols![Locomotive.MaxFunctionIndex], Is.EqualTo("horn.png"));
        }
    }

    [Test]
    public void SetFunctionAppearance_IgnoresAnInvalidColour()
    {
        var locomotive = new Locomotive();

        locomotive.SetFunctionAppearance(0, "light.png", "#06D6A0");
        locomotive.SetFunctionAppearance(0, symbol: null, "green");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(locomotive.FunctionSymbols, Is.EqualTo(LightSymbol));
            Assert.That(locomotive.FunctionColors, Is.EqualTo(GreenColour));
        }
    }

    [Test]
    public void SetFunctionAppearance_WithoutSymbolAndColourChangesNothingButReportsTheIndex()
    {
        var locomotive = new Locomotive();

        var inRange = locomotive.SetFunctionAppearance(5, symbol: null, colorHex: null);
        var outOfRange = locomotive.SetFunctionAppearance(32, "horn.png", "#06D6A0");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inRange, Is.True);
            Assert.That(outOfRange, Is.False);
            Assert.That(locomotive.FunctionSymbols, Is.Null);
            Assert.That(locomotive.FunctionColors, Is.Null);
        }
    }

    [Test]
    public void ClearFunctionAppearance_MarksSymbolAndColourAsNone()
    {
        var locomotive = new Locomotive();

        var cleared = locomotive.ClearFunctionAppearance(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cleared, Is.True);
            Assert.That(locomotive.FunctionSymbols, Is.EqualTo(ClearedAtIndexOne));
            Assert.That(locomotive.FunctionColors, Is.EqualTo(ClearedAtIndexOne));
            Assert.That(locomotive.ClearFunctionAppearance(-1), Is.False);
            Assert.That(Locomotive.ClearedFunctionAppearance, Is.EqualTo("none"));
        }
    }

    [Test]
    public void ClearFunctionAppearance_OverwritesAStoredColourAndKeepsTheOthers()
    {
        var locomotive = new Locomotive { FunctionColors = ["#FFD700", "#06D6A0"] };

        var cleared = locomotive.ClearFunctionAppearance(0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cleared, Is.True);
            Assert.That(locomotive.FunctionColors, Is.EqualTo(ClearedColourAtIndexZero));
        }
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase(" ", false)]
    [TestCase("AABBCC", false)]
    [TestCase("XAABBCC", false)]
    [TestCase("#ABC", false)]
    [TestCase("#AABBCCDD", false)]
    [TestCase("##12AB3", false)]
    [TestCase("#12AB3Z", false)]
    [TestCase("#aBc012", true)]
    [TestCase("#ABCDEF", true)]
    [TestCase("#000000", true)]
    public void IsStoredFunctionColor_RequiresHashAndExactlySixAsciiHexDigits(string? value, bool expected)
    {
        Assert.That(Locomotive.IsStoredFunctionColor(value), Is.EqualTo(expected));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void IsStoredFunctionColor_ValidatesEveryDigit(int position)
    {
        var characters = "#AABBCC".ToCharArray();
        characters[position] = 'G';
        Assert.That(Locomotive.IsStoredFunctionColor(new string(characters)), Is.False);
    }
}
