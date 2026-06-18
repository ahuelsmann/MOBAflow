// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Display;

using Domain;

[TestFixture]
internal sealed class KsSignalAspectNamesTests
{
    [Test]
    public void ResolvePreviewSignalArticleNumber_Should_Return4046_OnlyFor4046()
    {
        Assert.That(KsSignalAspectNames.ResolvePreviewSignalArticleNumber("4046"), Is.EqualTo("4046"));
        Assert.That(KsSignalAspectNames.ResolvePreviewSignalArticleNumber("4032"), Is.EqualTo(string.Empty));
        Assert.That(KsSignalAspectNames.ResolvePreviewSignalArticleNumber(null), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetAspectLabel_Should_Use4046Names_WhenSignalIs4046()
    {
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks2, is4046: true), Is.EqualTo("Ks2+K"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Dunkel, is4046: true), Is.EqualTo("GrBl+K+G"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks2, is4046: false), Is.EqualTo("Ks2"));
    }
}
