// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.Validation;

using Domain;

/// <summary>
/// Severity of a structured interlocking definition finding.
/// </summary>
public enum InterlockingValidationSeverity
{
    Warning,
    Error
}

/// <summary>
/// Stable validation result that presentation code can render without duplicating rules.
/// </summary>
public sealed record InterlockingValidationFinding(
    string Code,
    InterlockingValidationSeverity Severity,
    Guid EntityId,
    IReadOnlyList<Guid> RelatedIds,
    string Message)
{
    public string Id => $"{Code}:{EntityId:N}:{string.Join(",", RelatedIds.Order().Select(id => id.ToString("N")))}";
}

/// <summary>
/// Complete validation result for one project's interlocking definition.
/// </summary>
public sealed record InterlockingValidationReport(IReadOnlyList<InterlockingValidationFinding> Findings)
{
    public bool IsValid => Findings.All(finding => finding.Severity != InterlockingValidationSeverity.Error);
}

public interface IInterlockingDefinitionValidator
{
    InterlockingValidationReport Validate(Project project);
}

/// <summary>
/// Validates structural interlocking references without accessing hardware or UI state.
/// </summary>
public sealed class InterlockingDefinitionValidator : IInterlockingDefinitionValidator
{
    public InterlockingValidationReport Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var findings = new List<InterlockingValidationFinding>();
        var definition = project.Interlocking;
        if (definition == null)
        {
            Add(findings, "interlocking.missing", Guid.Empty, "The project must define an interlocking configuration.");
            return CreateReport(findings);
        }

        var operationalIds = CollectOperationalIds(definition, findings);
        var blockBoundaryIds = definition.Turnouts.Select(turnout => turnout.Id)
            .Concat(definition.Signals.Select(signal => signal.Id))
            .ToHashSet();
        ValidateTurnouts(definition.Turnouts, findings);
        ValidateSignals(definition.Signals, findings);
        ValidateBlocks(definition.Blocks, blockBoundaryIds, findings);
        ValidateConnections(definition.Connections, blockBoundaryIds.Concat(definition.Blocks.Select(block => block.Id)).ToHashSet(), findings);
        ValidateRoutes(definition, findings);
        ValidateBindings(project, definition, operationalIds, findings);
        return CreateReport(findings);
    }

    private static HashSet<Guid> CollectOperationalIds(
        InterlockingDefinition definition,
        List<InterlockingValidationFinding> findings)
    {
        var entities = definition.Turnouts.Select(item => (item.Id, "turnout"))
            .Concat(definition.Signals.Select(item => (item.Id, "signal")))
            .Concat(definition.Blocks.Select(item => (item.Id, "block")))
            .Concat(definition.Routes.Select(item => (item.Id, "route")))
            .ToArray();

        foreach (var entityType in entities.Where(entity => entity.Id == Guid.Empty).Select(entity => entity.Item2))
            Add(findings, $"{entityType}.id.empty", Guid.Empty, $"Every {entityType} requires a stable non-empty ID.");

        foreach (var duplicate in entities.Where(entity => entity.Id != Guid.Empty).GroupBy(entity => entity.Id).Where(group => group.Count() > 1))
            Add(findings, "operational.id.duplicate", duplicate.Key, "An operational ID is assigned to multiple definitions.");

        return entities.Where(entity => entity.Id != Guid.Empty).Select(entity => entity.Id).ToHashSet();
    }

    private static void ValidateTurnouts(
        IReadOnlyList<TurnoutDefinition> turnouts,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var turnout in turnouts)
        {
            RequireName(turnout.Id, turnout.Name, "turnout", findings);
            if (turnout.DecoderAddress is < 1 or > 2044)
                Add(findings, "turnout.address.range", turnout.Id, "Turnout decoder address must be between 1 and 2044.");

            ValidateTurnoutCommands(turnout, findings);
            ValidateTurnoutConfirmations(turnout, findings);
        }
    }

    private static void ValidateTurnoutCommands(
        TurnoutDefinition turnout,
        List<InterlockingValidationFinding> findings)
    {
        var required = turnout.Kind == TurnoutKind.ThreeWay
            ? new[] { TurnoutPosition.Straight, TurnoutPosition.DivergingLeft, TurnoutPosition.DivergingRight }
            : new[] { TurnoutPosition.Straight, TurnoutPosition.DivergingLeft };

        foreach (var position in required.Where(position => turnout.Commands.All(command => command.Position != position)))
            Add(findings, "turnout.command.missing", turnout.Id, $"Turnout command mapping for {position} is missing.");

        foreach (var duplicate in turnout.Commands.GroupBy(command => command.Position).Where(group => group.Count() > 1))
            Add(findings, "turnout.command.duplicate", turnout.Id, $"Turnout position {duplicate.Key} has multiple command mappings.");

        ValidateTurnoutCommandSequences(turnout, findings);

        if (turnout.Kind == TurnoutKind.TwoWay && turnout.Commands.Any(command => command.Position == TurnoutPosition.DivergingRight))
            Add(findings, "turnout.command.unsupported", turnout.Id, "A two-way turnout cannot define DivergingRight.");
    }

    private static void ValidateTurnoutCommandSequences(
        TurnoutDefinition turnout,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var mapping in turnout.Commands)
        {
            if (mapping.Commands.Count == 0)
                Add(findings, "turnout.command.sequence.missing", turnout.Id, $"Turnout position {mapping.Position} requires at least one accessory command.");

            foreach (var command in mapping.Commands)
            {
                var address = (long)turnout.DecoderAddress + command.AddressOffset;
                if (command.AddressOffset < 0 || address is < 1 or > 2044)
                    Add(findings, "turnout.command.address", turnout.Id, $"Turnout command address for {mapping.Position} must be between 1 and 2044.");
                if (command.Output is < 0 or > 1)
                    Add(findings, "turnout.command.output", turnout.Id, $"Turnout output for {mapping.Position} must be 0 or 1.");
            }
        }
    }

    private static void ValidateTurnoutConfirmations(
        TurnoutDefinition turnout,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var duplicate in turnout.Confirmations.GroupBy(item => item.Position).Where(group => group.Count() > 1))
            Add(findings, "turnout.confirmation.duplicate", turnout.Id, $"Turnout position {duplicate.Key} has multiple confirmation mappings.");

        foreach (var confirmation in turnout.Confirmations)
        {
            if (confirmation.Conditions.Count == 0)
                Add(findings, "turnout.confirmation.conditions.missing", turnout.Id, $"Confirmation for {confirmation.Position} requires at least one feedback condition.");

            foreach (var condition in confirmation.Conditions.Where(item => item.FunctionAddress is < 1 or > 2044))
                Add(findings, "turnout.confirmation.address", turnout.Id, $"Confirmation address for {confirmation.Position} must be between 1 and 2044.");
        }
    }

    private static void ValidateSignals(
        IReadOnlyList<SignalDefinition> signals,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var signal in signals)
        {
            RequireName(signal.Id, signal.Name, "signal", findings);
            if (signal.BaseAddress is < 1 or > 2044)
                Add(findings, "signal.address.range", signal.Id, "Signal base address must be between 1 and 2044.");
            if (signal.IsMultiplexed && string.IsNullOrWhiteSpace(signal.MultiplexerArticleNumber))
                Add(findings, "signal.multiplexer.missing", signal.Id, "A multiplexed signal requires a multiplexer article number.");
            if (signal.IsMultiplexed && string.IsNullOrWhiteSpace(signal.MainSignalArticleNumber))
                Add(findings, "signal.article.missing", signal.Id, "A multiplexed signal requires a main signal article number.");
        }
    }

    private static void ValidateBlocks(
        IReadOnlyList<BlockDefinition> blocks,
        IReadOnlySet<Guid> operationalIds,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var block in blocks)
        {
            RequireName(block.Id, block.Name, "block", findings);
            AddDuplicateIds(block.Id, block.BoundaryElementIds, "block.boundary.duplicate", findings);
            AddMissingReferences(block.Id, block.BoundaryElementIds, operationalIds, "block.boundary.missing", findings);
            if (block.FeedbackInputs.Count == 0)
                Add(findings, "block.feedback.missing", block.Id, "A protected block requires explicit occupancy feedback inputs.");
            if (block.FeedbackInputs.All(input => input.Role != BlockFeedbackRole.Occupied))
                Add(findings, "block.feedback.occupied.missing", block.Id, "A protected block requires an explicit occupied observation.");
            if (block.FeedbackInputs.All(input => input.Role != BlockFeedbackRole.Clear))
                Add(findings, "block.feedback.clear.missing", block.Id, "A protected block requires an explicit clear observation.");
            foreach (var input in block.FeedbackInputs.Where(input => input.InPort is < 1 or > 512))
                Add(findings, "block.feedback.range", block.Id, $"Feedback input {input.InPort} must be between 1 and 512.");
            foreach (var duplicate in block.FeedbackInputs
                         .GroupBy(input => (input.InPort, input.Role))
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
                Add(findings, "block.feedback.duplicate", block.Id, $"Feedback input {duplicate.InPort} with role {duplicate.Role} is duplicated.");
            foreach (var contradiction in block.FeedbackInputs
                         .GroupBy(input => (input.InPort, input.ActiveState))
                         .Where(group => group.Select(input => input.Role).Distinct().Count() > 1))
                Add(findings, "block.feedback.contradictory", block.Id, $"Feedback input {contradiction.Key.InPort} maps the same active state to occupied and clear.");
        }
    }

    private static void ValidateRoutes(
        InterlockingDefinition definition,
        List<InterlockingValidationFinding> findings)
    {
        var routeIds = definition.Routes.Select(route => route.Id).ToHashSet();
        var routeResourceIds = definition.Turnouts.Select(turnout => turnout.Id)
            .Concat(definition.Signals.Select(signal => signal.Id))
            .Concat(definition.Blocks.Select(block => block.Id))
            .ToHashSet();
        foreach (var route in definition.Routes)
        {
            RequireName(route.Id, route.Name, "route", findings);
            if (route.EntryElementId == route.ExitElementId)
                Add(findings, "route.endpoint.same", route.Id, "Route entry and exit must be different operational elements.");

            ValidateRouteReferences(route, definition, routeResourceIds, routeIds, findings);
            ValidateRouteConnectivity(route, definition.Connections, findings);
        }
    }

    private static void ValidateConnections(
        IReadOnlyList<OperationalConnection> connections,
        IReadOnlySet<Guid> resourceIds,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var connection in connections)
        {
            if (connection.FromOperationalId == connection.ToOperationalId)
                Add(findings, "connection.endpoint.same", connection.FromOperationalId, "An operational connection must join two different objects.");

            AddMissingReferences(
                connection.FromOperationalId,
                [connection.FromOperationalId, connection.ToOperationalId],
                resourceIds,
                "connection.endpoint.missing",
                findings);
        }

        foreach (var duplicate in connections
                     .GroupBy(ConnectionKey)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            Add(
                findings,
                "connection.duplicate",
                duplicate.From,
                "The same operational connection is defined more than once.",
                [duplicate.To]);
        }
    }

    private static void ValidateRouteConnectivity(
        RouteDefinition route,
        IReadOnlyList<OperationalConnection> connections,
        List<InterlockingValidationFinding> findings)
    {
        var path = new[] { route.EntryElementId }
            .Concat(route.PathElementIds)
            .Append(route.ExitElementId)
            .ToArray();

        foreach (var duplicate in path.GroupBy(id => id).Where(group => group.Count() > 1))
            Add(findings, "route.path.repeated", route.Id, "A route cannot traverse the same operational object more than once.", [duplicate.Key]);

        for (var index = 0; index < path.Length - 1; index++)
        {
            var from = path[index];
            var to = path[index + 1];
            if (connections.Any(connection => Connects(connection, from, to)))
                continue;

            Add(findings, "route.path.disconnected", route.Id, "Adjacent route objects are not connected in the operational topology.", [from, to]);
        }
    }

    private static (Guid From, Guid To, bool Directed) ConnectionKey(OperationalConnection connection)
    {
        if (!connection.IsBidirectional || connection.FromOperationalId.CompareTo(connection.ToOperationalId) <= 0)
            return (connection.FromOperationalId, connection.ToOperationalId, !connection.IsBidirectional);

        return (connection.ToOperationalId, connection.FromOperationalId, false);
    }

    private static bool Connects(OperationalConnection connection, Guid from, Guid to) =>
        (connection.FromOperationalId == from && connection.ToOperationalId == to)
        || (connection.IsBidirectional && connection.FromOperationalId == to && connection.ToOperationalId == from);

    private static void ValidateRouteReferences(
        RouteDefinition route,
        InterlockingDefinition definition,
        IReadOnlySet<Guid> operationalIds,
        IReadOnlySet<Guid> routeIds,
        List<InterlockingValidationFinding> findings)
    {
        AddMissingReferences(route.Id, [route.EntryElementId, route.ExitElementId], operationalIds, "route.endpoint.missing", findings);
        AddDuplicateIds(route.Id, route.PathElementIds, "route.path.duplicate", findings);
        AddMissingReferences(route.Id, route.PathElementIds, operationalIds, "route.path.missing", findings);
        AddDuplicateIds(route.Id, route.ProtectedBlockIds, "route.block.duplicate", findings);
        AddMissingReferences(route.Id, route.ProtectedBlockIds, definition.Blocks.Select(block => block.Id).ToHashSet(), "route.block.missing", findings);
        AddDuplicateIds(route.Id, route.SignalRequirements.Select(requirement => requirement.SignalId), "route.signal.duplicate", findings);
        AddMissingReferences(
            route.Id,
            route.SignalRequirements.Select(requirement => requirement.SignalId),
            definition.Signals.Select(signal => signal.Id).ToHashSet(),
            "route.signal.missing",
            findings);
        var signals = definition.Signals.ToDictionary(signal => signal.Id);
        foreach (var requirement in route.SignalRequirements)
        {
            if (!Enum.IsDefined(requirement.ProceedAspect))
                Add(findings, "route.signal.aspect.invalid", route.Id, "A route signal requirement contains an unknown proceed aspect.", [requirement.SignalId]);
            else if (signals.TryGetValue(requirement.SignalId, out var signal)
                     && requirement.ProceedAspect == signal.SafeAspect)
                Add(findings, "route.signal.proceed.safe", route.Id, "A route proceed aspect must differ from the signal's safe stop aspect.", [requirement.SignalId]);
        }
        AddDuplicateIds(route.Id, route.ConflictingRouteIds, "route.conflict.duplicate", findings);
        AddMissingReferences(route.Id, route.ConflictingRouteIds, routeIds, "route.conflict.missing", findings);

        if (route.ConflictingRouteIds.Contains(route.Id))
            Add(findings, "route.conflict.self", route.Id, "A route cannot explicitly conflict with itself.");

        foreach (var duplicate in route.TurnoutRequirements.GroupBy(requirement => requirement.TurnoutId).Where(group => group.Count() > 1))
        {
            var positions = duplicate.Select(requirement => requirement.Position).Distinct().ToArray();
            var code = positions.Length > 1 ? "route.turnout.contradictory" : "route.turnout.duplicate";
            var message = positions.Length > 1
                ? "A route cannot require contradictory positions for the same turnout."
                : "The same turnout requirement is listed more than once.";
            Add(findings, code, route.Id, message, [duplicate.Key]);
        }
        AddMissingReferences(route.Id, route.TurnoutRequirements.Select(item => item.TurnoutId), definition.Turnouts.Select(turnout => turnout.Id).ToHashSet(), "route.turnout.missing", findings);
    }

    private static void ValidateBindings(
        Project project,
        InterlockingDefinition definition,
        IReadOnlySet<Guid> operationalIds,
        List<InterlockingValidationFinding> findings)
    {
        var trackIds = project.TrackPlan?.Segments.Select(segment => segment.Id).ToHashSet() ?? [];
        var signalBoxIds = project.SignalBoxPlan?.Elements.Select(element => element.Id).ToHashSet() ?? [];

        foreach (var binding in definition.Bindings)
        {
            if (!operationalIds.Contains(binding.OperationalId))
                Add(findings, "binding.operational.missing", binding.OperationalId, "Binding references an unknown operational object.");
            if (binding.TrackSegmentIds.Count == 0 && binding.SignalBoxElementIds.Count == 0)
                Add(findings, "binding.representation.missing", binding.OperationalId, "Binding must reference at least one page representation.");
            AddDuplicateIds(binding.OperationalId, binding.TrackSegmentIds, "binding.track.duplicate", findings);
            AddMissingReferences(binding.OperationalId, binding.TrackSegmentIds, trackIds, "binding.track.missing", findings);
            AddDuplicateIds(binding.OperationalId, binding.SignalBoxElementIds, "binding.signalbox.duplicate", findings);
            AddMissingReferences(binding.OperationalId, binding.SignalBoxElementIds, signalBoxIds, "binding.signalbox.missing", findings);
        }

        foreach (var duplicate in definition.Bindings
                     .GroupBy(binding => binding.OperationalId)
                     .Where(group => group.Key != Guid.Empty && group.Count() > 1))
            Add(findings, "binding.operational.duplicate", duplicate.Key, "An operational object has multiple binding records.");

        AddRepresentationConflicts(
            definition.Bindings.SelectMany(binding => binding.TrackSegmentIds.Select(id => (binding.OperationalId, RepresentationId: id))),
            "binding.track.conflict",
            findings);
        AddRepresentationConflicts(
            definition.Bindings.SelectMany(binding => binding.SignalBoxElementIds.Select(id => (binding.OperationalId, RepresentationId: id))),
            "binding.signalbox.conflict",
            findings);
    }

    private static void AddRepresentationConflicts(
        IEnumerable<(Guid OperationalId, Guid RepresentationId)> bindings,
        string code,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var duplicate in bindings
                     .GroupBy(binding => binding.RepresentationId)
                     .Where(group => group.Key != Guid.Empty && group.Select(binding => binding.OperationalId).Distinct().Count() > 1))
        {
            Add(
                findings,
                code,
                duplicate.Key,
                "A representation is bound to multiple operational objects.",
                duplicate.Select(binding => binding.OperationalId).Distinct().Order().ToArray());
        }
    }

    private static void AddDuplicateIds(
        Guid entityId,
        IEnumerable<Guid> ids,
        string code,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var duplicate in ids.GroupBy(id => id).Where(group => group.Count() > 1))
            Add(findings, code, entityId, "The same reference is listed more than once.", [duplicate.Key]);
    }

    private static void AddMissingReferences(
        Guid entityId,
        IEnumerable<Guid> references,
        IReadOnlySet<Guid> available,
        string code,
        List<InterlockingValidationFinding> findings)
    {
        foreach (var missing in references.Where(reference => !available.Contains(reference)).Distinct().Order())
            Add(findings, code, entityId, "A referenced operational object does not exist.", [missing]);
    }

    private static void RequireName(
        Guid entityId,
        string name,
        string entityType,
        List<InterlockingValidationFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(name))
            Add(findings, $"{entityType}.name.missing", entityId, $"Every {entityType} requires a name.");
    }

    private static void Add(
        List<InterlockingValidationFinding> findings,
        string code,
        Guid entityId,
        string message,
        IReadOnlyList<Guid>? relatedIds = null)
    {
        findings.Add(new InterlockingValidationFinding(
            code,
            InterlockingValidationSeverity.Error,
            entityId,
            relatedIds ?? [],
            message));
    }

    private static InterlockingValidationReport CreateReport(List<InterlockingValidationFinding> findings) =>
        new(findings
            .OrderBy(finding => finding.Code, StringComparer.Ordinal)
            .ThenBy(finding => finding.EntityId)
            .ThenBy(finding => finding.Id, StringComparer.Ordinal)
            .ToArray());
}
