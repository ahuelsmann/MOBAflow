// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// Tests for <see cref="ProjectValidator"/> and its <see cref="ProjectValidationResult"/>.
/// Covers the completeness rules (errors vs. warnings vs. info) so that the reference
/// solution.json guard rails do not silently regress.
/// </summary>
[TestFixture]
internal sealed class ProjectValidatorTests
{
    private static ProjectValidator CreateValidator()
        => new(NullLogger<ProjectValidator>.Instance, new InterlockingDefinitionValidator());

    private static Project CreateMinimalValidProject()
    {
        var project = new Project { Name = "Demo" };
        project.Locomotives.Add(new Locomotive());
        var journey = new Journey();
        journey.Stations.Add(new Station());
        project.Journeys.Add(journey);
        return project;
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectValidator(null!, new InterlockingDefinitionValidator()));
    }

    [Test]
    public void Constructor_NullInterlockingValidator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectValidator(NullLogger<ProjectValidator>.Instance, null!));
    }

    [Test]
    public void ValidateCompleteness_NullSolution_Throws()
    {
        var validator = CreateValidator();

        Assert.Throws<ArgumentNullException>(() => validator.ValidateCompleteness(null!));
    }

    [Test]
    public void ValidateCompleteness_EmptySolution_ProducesErrorAndIsInvalid()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCompleteness(new Solution());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Messages.Any(m => m.Level == ValidationLevel.Error), Is.True);
        });
    }

    [Test]
    public void ValidateCompleteness_ProjectWithoutLocomotives_AddsWarning()
    {
        var validator = CreateValidator();
        var solution = new Solution();
        solution.Projects.Add(new Project { Name = "NoLocos" });

        var result = validator.ValidateCompleteness(solution);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasWarnings, Is.True);
            Assert.That(result.IsValid, Is.True, "Missing locomotives is a warning, not an error.");
            Assert.That(result.Messages.Any(m =>
                m.Level == ValidationLevel.Warning && m.Text.Contains("No locomotives")), Is.True);
        });
    }

    [Test]
    public void ValidateCompleteness_JourneyWithoutStations_WarnsAboutStations()
    {
        var validator = CreateValidator();
        var solution = new Solution();
        var project = new Project { Name = "EmptyJourney" };
        project.Locomotives.Add(new Locomotive());
        project.Journeys.Add(new Journey());
        solution.Projects.Add(project);

        var result = validator.ValidateCompleteness(solution);

        Assert.That(result.Messages.Any(m =>
            m.Level == ValidationLevel.Warning && m.Text.Contains("no stations")), Is.True);
    }

    [Test]
    public void ValidateCompleteness_FullyPopulatedProject_HasNoWarnings()
    {
        var validator = CreateValidator();
        var solution = new Solution();
        var project = CreateMinimalValidProject();
        project.Trains.Add(new Train());
        project.Workflows.Add(new Workflow());
        project.PassengerWagons.Add(new PassengerWagon());
        project.GoodsWagons.Add(new GoodsWagon());
        project.SignalBoxPlan = new SignalBoxPlan();
        solution.Projects.Add(project);

        var result = validator.ValidateCompleteness(solution);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasWarnings, Is.False);
            Assert.That(result.Messages.Count(m => m.Level == ValidationLevel.Info), Is.GreaterThanOrEqualTo(6));
        });
    }

    [Test]
    public void ValidateCompleteness_UnnamedProject_UsesIndexedFallbackName()
    {
        var validator = CreateValidator();
        var solution = new Solution();
        solution.Projects.Add(new Project { Name = string.Empty });

        var result = validator.ValidateCompleteness(solution);

        Assert.That(result.Messages.Any(m => m.Text.Contains("Project[0]")), Is.True);
    }

    [Test]
    public void ValidateCompleteness_InvalidFeedbackStep_ProducesErrors()
    {
        var project = CreateMinimalValidProject();
        project.Journeys[0].FeedbackSequence.Add(new JourneyFeedbackStep { InPort = 0, RepeatCount = 0, DelayMs = -1 });
        var solution = new Solution { Projects = [project] };

        var result = CreateValidator().ValidateCompleteness(solution);

        Assert.That(result.Messages.Count(message => message.Level == ValidationLevel.Error), Is.EqualTo(3));
    }

    [Test]
    public void ValidateCompleteness_DirectAndWorkflowStopTransitions_Conflict()
    {
        var project = CreateMinimalValidProject();
        var workflow = new Workflow
        {
            Actions = [new WorkflowAction { Type = ActionType.ChangeJourneyStop, ChangeJourneyStop = new ChangeJourneyStopActionPayload() }]
        };
        project.Workflows.Add(workflow);
        project.Journeys[0].FeedbackSequence.Add(new JourneyFeedbackStep
        {
            InPort = 2,
            WorkflowId = workflow.Id,
            StopTransition = new JourneyStopTransition { Mode = JourneyStopTransitionMode.Next }
        });

        var result = CreateValidator().ValidateCompleteness(new Solution { Projects = [project] });

        Assert.That(result.Messages.Any(message => message.Level == ValidationLevel.Error && message.Text.Contains("conflicts")), Is.True);
    }

    [Test]
    public void ValidateCompleteness_InvalidWorkflowGraph_IncludesStableWorkflowCodeAndStep()
    {
        // Arrange
        var project = CreateMinimalValidProject();
        var stepId = Guid.NewGuid();
        project.Workflows.Add(new Workflow
        {
            EntryStepId = stepId,
            Steps =
            [
                new WorkflowDelayStep
                {
                    Id = stepId,
                    DelayMs = -1,
                    NextStepId = Guid.NewGuid()
                }
            ]
        });

        // Act
        var result = CreateValidator().ValidateCompleteness(new Solution { Projects = [project] });

        // Assert
        Assert.That(result.Messages.Any(message =>
            message.Level == ValidationLevel.Error &&
            message.Text.Contains(WorkflowValidationCodes.InvalidStepPayload) &&
            message.Text.Contains(stepId.ToString())), Is.True);
    }

    [Test]
    public void ValidateCompleteness_InvalidInterlocking_ProducesStructuredError()
    {
        var project = CreateMinimalValidProject();
        project.Interlocking.Turnouts.Add(new TurnoutDefinition
        {
            Name = "W1",
            DecoderAddress = 0
        });

        var result = CreateValidator().ValidateCompleteness(new Solution { Projects = [project] });

        Assert.That(
            result.Messages.Any(message =>
                message.Level == ValidationLevel.Error
                && message.Text.Contains("Interlocking/turnout.address.range", StringComparison.Ordinal)),
            Is.True);
    }

    [Test]
    public void GetSummary_AggregatesCountsPerLevel()
    {
        var result = new ProjectValidationResult();
        result.AddError("boom");
        result.AddWarning("careful");
        result.AddWarning("careful again");
        result.AddInfo("fyi");

        var summary = result.GetSummary();

        Assert.Multiple(() =>
        {
            Assert.That(summary, Does.Contain("[ERRORS] 1"));
            Assert.That(summary, Does.Contain("[WARNINGS] 2"));
            Assert.That(summary, Does.Contain("[INFO] 1"));
        });
    }

    [Test]
    public void GetSummary_NoMessages_ReturnsEmptyString()
    {
        var result = new ProjectValidationResult();

        Assert.That(result.GetSummary(), Is.Empty);
    }

    [Test]
    public void ValidationMessage_ToString_IncludesLevelAndText()
    {
        var message = new ValidationMessage(ValidationLevel.Warning, "hello");

        Assert.Multiple(() =>
        {
            Assert.That(message.ToString(), Is.EqualTo("[Warning] hello"));
            Assert.That(message.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
        });
    }
}
