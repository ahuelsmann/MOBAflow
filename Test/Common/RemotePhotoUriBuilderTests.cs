// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Common.Path;

namespace Moba.Test.Common;

[TestFixture]
[Category("Unit")]
internal sealed class RemotePhotoUriBuilderTests
{
    [Test]
    public void BuildRelativeApiPath_ReturnsEncodedPathWithoutServerAddress()
    {
        var path = RemotePhotoUriBuilder.BuildRelativeApiPath("photos/latest/abc.jpg?v=2");

        Assert.That(path, Is.EqualTo("api/photos/file?path=photos%2Flatest%2Fabc.jpg&v=2"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n")]
    [TestCase("../outside.jpg")]
    [TestCase("photos/../outside.jpg")]
    [TestCase("/outside.jpg")]
    [TestCase("photos/")]
    [TestCase("?v=2")]
    [TestCase("?")]
    [TestCase("photos/?v=2")]
    public void BuildRelativeApiPath_RejectsMissingOrUnsafePhotoPath(string? input)
    {
        var path = RemotePhotoUriBuilder.BuildRelativeApiPath(input);

        Assert.That(path, Is.Null);
    }

    [TestCase("photos/latest/abc.jpg", "api/photos/file?path=photos%2Flatest%2Fabc.jpg")]
    [TestCase("locomotives/BR 218 #1.jpg", "api/photos/file?path=photos%2Flocomotives%2FBR%20218%20%231.jpg")]
    [TestCase("photos\\latest\\a+b.jpg", "api/photos/file?path=photos%2Flatest%2Fa%2Bb.jpg")]
    [TestCase("photos/latest/abc.jpg?unused=1", "api/photos/file?path=photos%2Flatest%2Fabc.jpg")]
    [TestCase("photos/latest/abc.jpg?v=build 2&unused=1", "api/photos/file?path=photos%2Flatest%2Fabc.jpg&v=build%202")]
    public void BuildRelativeApiPath_NormalizesAndEncodesPathWithoutForwardingUnrelatedQuery(string input, string expected)
    {
        var path = RemotePhotoUriBuilder.BuildRelativeApiPath(input);

        Assert.That(path, Is.EqualTo(expected));
    }
}