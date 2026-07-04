// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Common.Path;

namespace Moba.Test.Common;

[TestFixture]
internal sealed class RemotePhotoUriBuilderTests
{
    [Test]
    public void BuildHttpUri_ReturnsEncodedUri_WhenInputsValid()
    {
        var uri = RemotePhotoUriBuilder.BuildHttpUri(
            "192.168.0.10",
            5001,
            "photos/latest/abc.jpg");

        Assert.That(uri, Is.EqualTo("http://192.168.0.10:5001/api/photos/file?path=photos%2Flatest%2Fabc.jpg"));
    }

    [Test]
    public void BuildHttpUri_AppendsVersionQuery_WhenBindingPathHasVersion()
    {
        var uri = RemotePhotoUriBuilder.BuildHttpUri(
            "192.168.0.10",
            5001,
            "photos/latest/abc.jpg?v=2");

        Assert.That(uri, Is.EqualTo("http://192.168.0.10:5001/api/photos/file?path=photos%2Flatest%2Fabc.jpg&v=2"));
    }

    [Test]
    public void BuildHttpUri_ReturnsNull_WhenServerMissing()
    {
        Assert.That(RemotePhotoUriBuilder.BuildHttpUri(null, 5001, "photos/a.jpg"), Is.Null);
        Assert.That(RemotePhotoUriBuilder.BuildHttpUri(" ", 5001, "photos/a.jpg"), Is.Null);
        Assert.That(RemotePhotoUriBuilder.BuildHttpUri("127.0.0.1", 0, "photos/a.jpg"), Is.Null);
        Assert.That(RemotePhotoUriBuilder.BuildHttpUri("127.0.0.1", 5001, null), Is.Null);
    }
}
