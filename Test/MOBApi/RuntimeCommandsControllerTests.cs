// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moba.MOBApi.Controllers;
using Moba.MOBApi.Service;

using Moq;

using System.Net;

/// <summary>
/// Verifies the REST contract for remote commands: 202 when accepted, 400 when invalid, 429 when the queue is full.
/// </summary>
[TestFixture]
internal sealed class RuntimeCommandsControllerTests
{
    [Test]
    public void Drive_WithValidValues_ReturnsAccepted()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveDrive(new RuntimeCommandsController.SetLocomotiveDriveRequest(3, 40, true));

        Assert.That(result, Is.InstanceOf<AcceptedResult>());
    }

    [TestCase(0, 40)]
    [TestCase(3, 127)]
    public void Drive_WithInvalidValues_ReturnsBadRequest(int address, int speed)
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveDrive(
            new RuntimeCommandsController.SetLocomotiveDriveRequest(address, speed, true));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Function_WithInvalidIndex_ReturnsBadRequest()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveFunction(
            new RuntimeCommandsController.SetLocomotiveFunctionRequest(3, 32, true));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void SignalAspect_WithUndefinedAspect_ReturnsBadRequest()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueSignalAspect(
            new RuntimeCommandsController.SetSignalAspectRequest(Guid.NewGuid(), (SignalAspect)999));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Drive_WhenQueueIsFull_ReturnsTooManyRequests()
    {
        var controller = CreateController(capacity: 1);
        controller.EnqueueLocomotiveDrive(new RuntimeCommandsController.SetLocomotiveDriveRequest(3, 40, true));

        var result = controller.EnqueueLocomotiveDrive(new RuntimeCommandsController.SetLocomotiveDriveRequest(4, 40, true));

        Assert.That((result as ObjectResult)?.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
    }

    [Test]
    public void JourneyReset_WhenQueueIsFull_ReturnsTooManyRequests()
    {
        var admission = new RuntimeCommandAdmission(new RuntimeCommandQueue(capacity: 1));
        var controller = new JourneyProgressController(new Mock<IRuntimeSnapshotCache>().Object, admission)
        {
            ControllerContext = LoopbackContext()
        };

        var first = controller.Reset(Guid.NewGuid());
        var second = controller.Reset(Guid.NewGuid());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.InstanceOf<AcceptedResult>());
            Assert.That((second as ObjectResult)?.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
        }
    }

    private static RuntimeCommandsController CreateController(int capacity)
    {
        var queue = new RuntimeCommandQueue(capacity);
        return new RuntimeCommandsController(new RuntimeCommandAdmission(queue));
    }

    private static ControllerContext LoopbackContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        return new ControllerContext { HttpContext = httpContext };
    }
}
