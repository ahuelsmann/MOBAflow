// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Common.Path;

namespace Moba.Test.Common;

[TestFixture]
internal sealed class RemotePhotoUriBuilderTests
{
    [Test]
    public void BuildRelativeApiPath_ReturnsEncodedPathWithoutServerAddress()
    {
        var path = RemotePhotoUriBuilder.BuildRelativeApiPath("photos/latest/abc.jpg?v=2");

        Assert.That(path, Is.EqualTo("api/photos/file?path=photos%2Flatest%2Fabc.jpg&v=2"));
    }
}