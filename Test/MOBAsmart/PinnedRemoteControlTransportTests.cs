#if !SKIP_ANDROID_TESTS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBAsmart;

using Moba.MAUI.Service;
using System.Reflection;

[TestFixture]
internal sealed class PinnedRemoteControlTransportTests
{
    [Test]
    public void ClientRelease_ShouldMatchPublishedApplicationDisplayVersion()
    {
        var displayVersion = typeof(PinnedRemoteControlTransport).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "MOBAsmart.ApplicationDisplayVersion")
            .Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(displayVersion, Is.Not.Null.And.Not.Empty);
            Assert.That(PinnedRemoteControlTransport.ClientRelease, Is.EqualTo($"MOBAsmart {displayVersion}"));
        }
    }
}
#endif
