// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moba.Common.Runtime;
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
    private static readonly Guid Project = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public void Drive_WithValidValues_ReturnsAccepted()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveDrive(new RuntimeCommandsController.SetLocomotiveDriveRequest(Project, 3, 40, true));

        Assert.That(result, Is.InstanceOf<AcceptedResult>());
    }

    [TestCase(0, 40)]
    [TestCase(3, 127)]
    public void Drive_WithInvalidValues_ReturnsBadRequest(int address, int speed)
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveDrive(
            new RuntimeCommandsController.SetLocomotiveDriveRequest(Project, address, speed, true));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Function_WithInvalidIndex_ReturnsBadRequest()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveFunction(
            new RuntimeCommandsController.SetLocomotiveFunctionRequest(Project, 3, 32, true));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void SignalAspect_WithUndefinedAspect_ReturnsBadRequest()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueSignalAspect(
            new RuntimeCommandsController.SetSignalAspectRequest(Project, Guid.NewGuid(), (SignalAspect)999));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Drive_ForUnknownProject_ReturnsBadRequest()
    {
        var controller = CreateController(capacity: 4);

        var result = controller.EnqueueLocomotiveDrive(
            new RuntimeCommandsController.SetLocomotiveDriveRequest(Guid.NewGuid(), 3, 40, true));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void Drive_WhenQueueIsFull_ReturnsTooManyRequests()
    {
        var controller = CreateController(capacity: 1);
        controller.EnqueueLocomotiveDrive(new RuntimeCommandsController.SetLocomotiveDriveRequest(Project, 3, 40, true));

        var result = controller.EnqueueLocomotiveDrive(new RuntimeCommandsController.SetLocomotiveDriveRequest(Project, 4, 40, true));

        Assert.That((result as ObjectResult)?.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
    }

    [Test]
    public void JourneyReset_WhenQueueIsFull_ReturnsTooManyRequests()
    {
        var firstJourney = Guid.NewGuid();
        var secondJourney = Guid.NewGuid();
        var queue = new RuntimeCommandQueue(capacity: 1);
        var controller = new JourneyProgressController(
            SnapshotWithJourneys(firstJourney, secondJourney),
            new RuntimeCommandAdmission(queue, KnownProject()))
        {
            ControllerContext = LoopbackContext()
        };

        var first = controller.Reset(firstJourney);
        var second = controller.Reset(secondJourney);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.InstanceOf<AcceptedResult>());
            Assert.That((second as ObjectResult)?.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
            Assert.That(queue.TryDequeue(out var queued), Is.True);
            Assert.That(queued?.ProjectId, Is.EqualTo(Project));
        }
    }

    [Test]
    public void JourneyReset_ForJourneyOfNoProject_ReturnsNotFound()
    {
        var controller = new JourneyProgressController(
            SnapshotWithJourneys(Guid.NewGuid()),
            new RuntimeCommandAdmission(new RuntimeCommandQueue(capacity: 1), KnownProject()))
        {
            ControllerContext = LoopbackContext()
        };

        Assert.That(controller.Reset(Guid.NewGuid()), Is.InstanceOf<NotFoundObjectResult>());
    }

    private static RuntimeCommandsController CreateController(int capacity)
    {
        var queue = new RuntimeCommandQueue(capacity);
        return new RuntimeCommandsController(new RuntimeCommandAdmission(queue, KnownProject()));
    }

    /// <summary>A synchronized solution that contains only <see cref="Project"/>.</summary>
    private static ISolutionCache KnownProject()
    {
        var solutionCache = new Mock<ISolutionCache>();
        solutionCache.Setup(cache => cache.ContainsProject(Project)).Returns(true);
        return solutionCache.Object;
    }

    /// <summary>The runtime snapshot of <see cref="Project"/> with the given journeys.</summary>
    private static RuntimeSnapshotCache SnapshotWithJourneys(params Guid[] journeyIds)
    {
        var cache = new RuntimeSnapshotCache();
        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = Project,
            JourneyStates = journeyIds.ToDictionary(id => id, id => new JourneyRuntimeSnapshot { JourneyId = id })
        }));
        return cache;
    }

    private static ControllerContext LoopbackContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        return new ControllerContext { HttpContext = httpContext };
    }
}
