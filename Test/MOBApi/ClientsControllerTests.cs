// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.Mvc;

using Moba.MOBApi.Controllers;
using Moba.MOBApi.Models;
using Moba.MOBApi.Service;

/// <summary>
/// Verifies that clients register and unregister by the ClientId they send, without credentials.
/// </summary>
[TestFixture]
internal sealed class ClientsControllerTests
{
    [Test]
    public void Register_AddsClient_UnderTrimmedClientId()
    {
        var registry = new ClientRegistry();
        var controller = new ClientsController(registry);

        var result = controller.Register(new RegisterClientRequest("  phone-1  ", "Pixel"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(registry.GetAll().Single().ClientId, Is.EqualTo("phone-1"));
            Assert.That(registry.GetAll().Single().DeviceName, Is.EqualTo("Pixel"));
        }
    }

    [Test]
    public void Unregister_RemovesClient()
    {
        var registry = new ClientRegistry();
        var controller = new ClientsController(registry);
        controller.Register(new RegisterClientRequest("phone-1", "Pixel"));

        var result = controller.Unregister(new UnregisterClientRequest("phone-1"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(registry.GetAll(), Is.Empty);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Register_And_Unregister_RejectMissingClientId(string? clientId)
    {
        var registry = new ClientRegistry();
        var controller = new ClientsController(registry);

        var registerResult = controller.Register(new RegisterClientRequest(clientId!, "Pixel"));
        var unregisterResult = controller.Unregister(new UnregisterClientRequest(clientId!));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(registerResult, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(unregisterResult, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(registry.GetAll(), Is.Empty);
        }
    }
}
