// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class LocomotivePassportHtmlRendererTests
{
    [Test]
    public void Render_EscapesUserInputAndExcludesPrivateTransportData()
    {
        var service = new LocomotiveLibraryService();
        var renderer = new LocomotivePassportHtmlRenderer();
        var locomotive = new Locomotive
        {
            Name = "<script>alert('x')</script>",
            PhotoPath = @"C:\Users\owner\secret.jpg",
            Decoder = new LocomotiveDecoderProfile
            {
                Manufacturer = "A&B",
                CvSnapshots = [new DecoderCvSnapshot { Name = "Backup" }]
            }
        };

        var html = renderer.Render(service.BuildPassport(locomotive));

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("&lt;script&gt;"));
            Assert.That(html, Does.Contain("A&amp;B"));
            Assert.That(html, Does.Not.Contain("<script>alert"));
            Assert.That(html, Does.Not.Contain("secret.jpg"));
            Assert.That(html, Does.Not.Contain("http://"));
            Assert.That(html, Does.Not.Contain("https://"));
        });
    }
}
