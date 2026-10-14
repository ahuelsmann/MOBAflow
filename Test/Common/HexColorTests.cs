// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Display;

/// <summary>
/// Parsing of the hex colours stored for locomotive function buttons.
/// </summary>
[TestFixture]
internal sealed class HexColorTests
{
    [TestCase("#12AB34")]
    [TestCase("12AB34")]
    [TestCase("#FF12AB34")]
    [TestCase("0012ab34")]
    public void TryParseRgb_ReadsRgbAndIgnoresAlpha(string value)
    {
        var parsed = HexColor.TryParseRgb(value, out var red, out var green, out var blue);

        Assert.That((parsed, red, green, blue), Is.EqualTo((true, (byte)0x12, (byte)0xAB, (byte)0x34)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("#12AB3")]
    [TestCase("#12AB345")]
    [TestCase("#ZZZZZZ")]
    [TestCase("# 12AB3")]
    [TestCase("12AB34 ")]
    [TestCase("0x12AB34")]
    [TestCase("none")]
    public void TryParseRgb_RejectsAnythingElse(string? value)
    {
        Assert.That(HexColor.TryParseRgb(value, out _, out _, out _), Is.False);
    }

    [TestCase("#12AB34", true)]
    [TestCase("#12ab34", true)]
    [TestCase("12AB34", false)]
    [TestCase("#FF12AB34", false)]
    [TestCase("##12AB3", false)]
    [TestCase("#12AB3Z", false)]
    public void IsStoredForm_AcceptsOnlyHashAndSixDigits(string value, bool expected)
    {
        Assert.That(HexColor.IsStoredForm(value), Is.EqualTo(expected));
    }
}
