// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

/// <summary>The host-detail scan must find real host names but not common words of a front-end bundle.</summary>
[TestFixture]
internal sealed class HostDetailTests
{
    private const string BundleText =
        "<div id=\"root\"></div><script>const user=1,dev=2,node=3,demo=4,test=5;export const device='root user dev';</script>";

    [TestCase("root")]
    [TestCase("dev")]
    [TestCase("user")]
    [TestCase("demo")]
    [TestCase("node")]
    public void ShortOrGenericName_DoesNotMatchOrdinaryBundleText(string name)
    {
        Assert.That(HostDetail.NamePatterns(name).Any(pattern => pattern.IsMatch(BundleText)), Is.False);
    }

    [TestCase("dev", @"C:\Users\dev\source\app.tsx")]
    [TestCase("dev", "/home/dev/source/app.tsx")]
    [TestCase("dev", "C:/Users/dev/AppData/Local/Temp")]
    [TestCase("dev", @"\\dev\share\file")]
    [TestCase("buildbox", "https://buildbox:5173/")]
    [TestCase("buildbox", "buildbox.local")]
    public void NameInPathOrHostContext_Matches(string name, string text)
    {
        Assert.That(HostDetail.NamePatterns(name).Any(pattern => pattern.IsMatch(text)), Is.True, text);
    }

    [Test]
    public void NameShorterThanThreeCharacters_YieldsNoPattern()
    {
        Assert.That(HostDetail.NamePatterns("ab"), Is.Empty);
    }
}
