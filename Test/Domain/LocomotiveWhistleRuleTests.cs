// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// The values a locomotive whistle rule needs before it can run.
/// </summary>
[TestFixture]
internal sealed class LocomotiveWhistleRuleTests
{
    [Test]
    public void FindIssues_ReportsEveryInvalidValueInPropertyOrder()
    {
        var rule = new LocomotiveWhistleRule
        {
            LocomotiveId = Guid.Empty,
            InPort = 0,
            FunctionIndex = -1,
            DelayMilliseconds = -1,
            ActiveDurationMilliseconds = 0
        };

        Assert.That(rule.FindIssues(), Is.EqualTo(new[]
        {
            LocomotiveWhistleRuleIssue.MissingLocomotive,
            LocomotiveWhistleRuleIssue.InvalidInPort,
            LocomotiveWhistleRuleIssue.FunctionOutOfRange,
            LocomotiveWhistleRuleIssue.NegativeDelay,
            LocomotiveWhistleRuleIssue.InvalidActiveDuration
        }));
    }

    [TestCase(0)]
    [TestCase(31)]
    public void FindIssues_AcceptsTheFunctionBounds(int functionIndex)
    {
        var rule = new LocomotiveWhistleRule { LocomotiveId = Guid.NewGuid(), InPort = 1, FunctionIndex = functionIndex };

        Assert.That(rule.FindIssues(), Is.Empty);
    }

    [TestCase(-1)]
    [TestCase(32)]
    public void FindIssues_RejectsFunctionsOutsideF0ToF31(int functionIndex)
    {
        var rule = new LocomotiveWhistleRule { LocomotiveId = Guid.NewGuid(), InPort = 1, FunctionIndex = functionIndex };

        Assert.That(rule.FindIssues(), Is.EqualTo(new[] { LocomotiveWhistleRuleIssue.FunctionOutOfRange }));
    }
}
