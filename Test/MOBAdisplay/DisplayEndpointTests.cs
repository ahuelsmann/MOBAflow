// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Transport;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayEndpointTests
{
    [TestCase(null, DisplayEndpointValidationError.MissingAddress)]
    [TestCase("", DisplayEndpointValidationError.MissingAddress)]
    [TestCase("not-an-ip", DisplayEndpointValidationError.InvalidAddress)]
    [TestCase("0.0.0.0", DisplayEndpointValidationError.UnspecifiedAddress)]
    [TestCase("::", DisplayEndpointValidationError.UnspecifiedAddress)]
    public void TryCreate_Should_RejectAddress_WhenUnavailable(
        string? address,
        DisplayEndpointValidationError expectedError)
    {
        var success = DisplayEndpoint.TryCreate(address, 4210, out var endpoint, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(endpoint, Is.Null);
            Assert.That(error, Is.EqualTo(expectedError));
        }
    }

    [TestCase(0)]
    [TestCase(65536)]
    public void TryCreate_Should_RejectPort_WhenOutsideUdpRange(int port)
    {
        var success = DisplayEndpoint.TryCreate("192.168.0.82", port, out var endpoint, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(endpoint, Is.Null);
            Assert.That(error, Is.EqualTo(DisplayEndpointValidationError.InvalidPort));
        }
    }

    [TestCase("192.168.0.82", "192.168.0.82:4210")]
    [TestCase("::1", "[::1]:4210")]
    public void TryCreate_Should_ReturnNormalizedEndpoint_WhenAddressIsValid(
        string address,
        string expectedText)
    {
        var success = DisplayEndpoint.TryCreate(address, 4210, out var endpoint, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(DisplayEndpointValidationError.None));
            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint!.ToString(), Is.EqualTo(expectedText));
        }
    }
}
