// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Runtime;

/// <summary>
/// Protects the value ranges that remote commands must satisfy before they reach the Z21 runtime.
/// </summary>
[TestFixture]
internal sealed class RuntimeCommandValidatorTests
{
    [TestCase(1, 0)]
    [TestCase(9999, 126)]
    [TestCase(3, 64)]
    public void Drive_WithinLimits_IsValid(int address, int speed)
    {
        var command = Drive(address, speed);

        Assert.That(RuntimeCommandValidator.TryValidate(command, out var error), Is.True, error);
    }

    [TestCase(0, 10, "Address")]
    [TestCase(10000, 10, "Address")]
    [TestCase(3, -1, "Speed")]
    [TestCase(3, 127, "Speed")]
    public void Drive_OutOfRange_IsRejected(int address, int speed, string field)
    {
        var valid = RuntimeCommandValidator.TryValidate(Drive(address, speed), out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(valid, Is.False);
            Assert.That(error, Does.StartWith(field));
        }
    }

    [Test]
    public void Drive_WithoutDirection_IsRejected()
    {
        var command = Drive(3, 10) with { Forward = null };

        Assert.That(RuntimeCommandValidator.TryValidate(command, out _), Is.False);
    }

    [TestCase(0, true)]
    [TestCase(31, true)]
    [TestCase(-1, false)]
    [TestCase(32, false)]
    public void Function_IndexRange_IsEnforced(int functionIndex, bool expected)
    {
        var command = new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveFunction,
            LocomotiveAddress = 3,
            FunctionIndex = functionIndex,
            FunctionIsOn = true
        };

        Assert.That(RuntimeCommandValidator.TryValidate(command, out _), Is.EqualTo(expected));
    }

    [Test]
    public void SignalAspect_WithKnownAspect_IsValid()
    {
        var command = SignalAspectCommand(Guid.NewGuid(), SignalAspect.Hp0);

        Assert.That(RuntimeCommandValidator.TryValidate(command, out var error), Is.True, error);
    }

    [Test]
    public void SignalAspect_WithEmptyIdentifier_IsRejected()
    {
        var command = SignalAspectCommand(Guid.Empty, SignalAspect.Hp0);

        Assert.That(RuntimeCommandValidator.TryValidate(command, out _), Is.False);
    }

    [Test]
    public void SignalAspect_WithUndefinedValue_IsRejected()
    {
        var command = SignalAspectCommand(Guid.NewGuid(), (SignalAspect)999);

        Assert.That(RuntimeCommandValidator.TryValidate(command, out _), Is.False);
    }

    [Test]
    public void ResetJourney_RequiresIdentifier()
    {
        var withId = new RuntimeCommandEnvelope { Type = RuntimeCommandType.ResetJourney, JourneyId = Guid.NewGuid() };
        var withoutId = new RuntimeCommandEnvelope { Type = RuntimeCommandType.ResetJourney, JourneyId = Guid.Empty };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(RuntimeCommandValidator.TryValidate(withId, out _), Is.True);
            Assert.That(RuntimeCommandValidator.TryValidate(withoutId, out _), Is.False);
        }
    }

    [Test]
    public void UnknownCommandType_IsRejected()
    {
        var command = new RuntimeCommandEnvelope { Type = (RuntimeCommandType)42 };

        Assert.That(RuntimeCommandValidator.TryValidate(command, out _), Is.False);
    }

    [TestCase(RuntimeCommandType.SetLocomotiveDrive, "Address", "Address is required.")]
    [TestCase(RuntimeCommandType.SetLocomotiveDrive, "Speed", "Speed is required.")]
    [TestCase(RuntimeCommandType.SetLocomotiveDrive, "Forward", "Forward is required.")]
    [TestCase(RuntimeCommandType.SetLocomotiveFunction, "Address", "Address is required.")]
    [TestCase(RuntimeCommandType.SetLocomotiveFunction, "FunctionIndex", "FunctionIndex is required.")]
    [TestCase(RuntimeCommandType.SetLocomotiveFunction, "FunctionIsOn", "FunctionIsOn is required.")]
    [TestCase(RuntimeCommandType.SetSignalAspect, "SignalId", "SignalId is required.")]
    [TestCase(RuntimeCommandType.SetSignalAspect, "SignalAspect", "SignalAspect is required.")]
    [TestCase(RuntimeCommandType.ResetJourney, "JourneyId", "JourneyId is required.")]
    public void MissingRequiredField_IsRejectedWithHelpfulReason(RuntimeCommandType type, string field, string expectedError)
    {
        var command = new RuntimeCommandEnvelope
        {
            Type = type,
            LocomotiveAddress = 3,
            Speed = 0,
            Forward = false,
            FunctionIndex = 0,
            FunctionIsOn = false,
            SignalId = Guid.NewGuid(),
            SignalAspect = SignalAspect.Hp0,
            JourneyId = Guid.NewGuid()
        };
        command = field switch
        {
            "Address" => command with { LocomotiveAddress = null },
            "Speed" => command with { Speed = null },
            "Forward" => command with { Forward = null },
            "FunctionIndex" => command with { FunctionIndex = null },
            "FunctionIsOn" => command with { FunctionIsOn = null },
            "SignalId" => command with { SignalId = null },
            "SignalAspect" => command with { SignalAspect = null },
            "JourneyId" => command with { JourneyId = null },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(RuntimeCommandValidator.TryValidate(command, out var error), Is.False);
            Assert.That(error, Is.EqualTo(expectedError));
        }
    }

    [TestCase(0)]
    [TestCase(10000)]
    public void Function_InvalidLocomotiveAddress_IsRejected(int address)
    {
        var command = new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveFunction,
            LocomotiveAddress = address,
            FunctionIndex = 0,
            FunctionIsOn = false
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RuntimeCommandValidator.TryValidate(command, out var error), Is.False);
            Assert.That(error, Is.EqualTo("Address must be between 1 and 9999."));
        }
    }

    [Test]
    public void Drive_ReverseDirectionAndZeroSpeed_AreValid()
    {
        var command = Drive(3, 0) with { Forward = false };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RuntimeCommandValidator.TryValidate(command, out var error), Is.True);
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public void Function_SwitchingOff_IsValid()
    {
        var command = new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveFunction,
            LocomotiveAddress = 3,
            FunctionIndex = 0,
            FunctionIsOn = false
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RuntimeCommandValidator.TryValidate(command, out var error), Is.True);
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public void NullCommand_IsRejectedBeforeValidation()
    {
        Assert.Throws<ArgumentNullException>(() => RuntimeCommandValidator.TryValidate(null!, out _));
    }

    private static RuntimeCommandEnvelope Drive(int address, int speed) => new()
    {
        Type = RuntimeCommandType.SetLocomotiveDrive,
        LocomotiveAddress = address,
        Speed = speed,
        Forward = true
    };

    private static RuntimeCommandEnvelope SignalAspectCommand(Guid signalId, SignalAspect aspect) => new()
    {
        Type = RuntimeCommandType.SetSignalAspect,
        SignalId = signalId,
        SignalAspect = aspect
    };
}
