// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test;

using CommunityToolkit.Mvvm.Messaging;
using Moba.Backend;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Domain;
using Moba.Domain.Message;
using Moba.Test.Mocks;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Collections.Generic;

/// <summary>
/// Tests for CommunityToolkit.Mvvm Messenger integration with feedback processing.
/// Validates that:
/// 1. Z21 publishes FeedbackReceivedMessage via Messenger
/// 2. Managers subscribe and receive messages correctly
/// 3. Multiple subscribers can process the same feedback
/// 4. Message threading is handled correctly
/// </summary>
[TestFixture]
public class FeedbackMessengerTests
{
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;

    [SetUp]
    public void Setup()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp);
    }

    [TearDown]
    public void TearDown()
    {
        _z21.Dispose();
        
        // Clear all Messenger subscriptions to prevent leaks between tests
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [Test]
    public void FeedbackReceivedMessage_Publishes_WhenFeedbackReceived()
    {
        // Arrange
        bool messageReceived = false;
        FeedbackReceivedMessage? capturedMessage = null;
        
        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (r, message) =>
            {
                messageReceived = true;
                capturedMessage = message;
            }
        );

        // Act
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };  // InPort=5
        _fakeUdp.RaiseReceived(testFeedback);

        // Wait for async processing
        System.Threading.Thread.Sleep(100);

        // Assert
        Assert.That(messageReceived, Is.True, "Message should be received");
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage.Value, Is.EqualTo(5u), "InPort should be 5");
        Assert.That(capturedMessage.RawData, Is.EqualTo(testFeedback), "RawData should match");
    }

    [Test]
    public void MultipleSubscribers_AllReceiveFeedback()
    {
        // Arrange
        int subscriber1Count = 0;
        int subscriber2Count = 0;

        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (r, m) => subscriber1Count++
        );
        
        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (r, m) => subscriber2Count++
        );

        // Act
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
        _fakeUdp.RaiseReceived(testFeedback);
        System.Threading.Thread.Sleep(100);

        // Assert
        Assert.That(subscriber1Count, Is.EqualTo(1), "Subscriber 1 should receive message");
        Assert.That(subscriber2Count, Is.EqualTo(1), "Subscriber 2 should receive message");
    }

    [Test]
    public void JourneyManager_ReceivesFeedback_ViaMessenger()
    {
        // Arrange
        var project = new Project
        {
            Journeys = new List<Journey>
            {
                new Journey { Id = Guid.NewGuid(), Name = "TestJourney", InPort = 5, FirstPos = 1 }
            },
            Workflows = new List<Workflow>()
        };

        var workflowService = new WorkflowService(null);
        var journeyMgr = new JourneyManager(_z21, project, workflowService);

        bool feedbackProcessed = false;
        journeyMgr.FeedbackReceived += (s, e) => feedbackProcessed = true;

        // Act
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
        _fakeUdp.RaiseReceived(testFeedback);
        System.Threading.Thread.Sleep(100);

        // Assert
        Assert.That(feedbackProcessed, Is.True, "JourneyManager should process feedback");
    }

    [Test]
    public void FeedbackReceivedMessage_IncludesTimestamp()
    {
        // Arrange
        FeedbackReceivedMessage? capturedMessage = null;
        var beforeTime = DateTime.UtcNow;

        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (r, m) => capturedMessage = m
        );

        // Act
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
        _fakeUdp.RaiseReceived(testFeedback);
        System.Threading.Thread.Sleep(100);

        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage.ReceivedAt, Is.GreaterThanOrEqualTo(beforeTime));
        Assert.That(capturedMessage.ReceivedAt, Is.LessThanOrEqualTo(afterTime));
    }

    [Test]
    public void Unregister_StopsReceivingMessages()
    {
        // Arrange
        int messageCount = 0;

        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (r, m) => messageCount++
        );

        // Act - Send first feedback
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
        _fakeUdp.RaiseReceived(testFeedback);
        System.Threading.Thread.Sleep(100);

        // Unregister
        WeakReferenceMessenger.Default.Unregister<FeedbackReceivedMessage>(this);

        // Send second feedback
        _fakeUdp.RaiseReceived(testFeedback);
        System.Threading.Thread.Sleep(100);

        // Assert
        Assert.That(messageCount, Is.EqualTo(1), "Should only receive first message after unregister");
    }

    [Test]
    public void BackwardCompatibility_LegacyReceivedEvent_StillWorks()
    {
        // Arrange
        bool legacyEventFired = false;
        _z21.Received += (feedback) => legacyEventFired = true;

        // Act
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
        _fakeUdp.RaiseReceived(testFeedback);
        System.Threading.Thread.Sleep(100);

        // Assert
        Assert.That(legacyEventFired, Is.True, "Legacy Z21.Received event should still work");
    }
}
