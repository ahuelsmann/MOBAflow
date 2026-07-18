// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Validation;

using Common.Multiplex;

using Domain;

/// <summary>
/// Separates address spaces that may legitimately reuse the same numeric value.
/// </summary>
public enum DigitalAddressDomain
{
    Locomotive,
    Accessory,
    Feedback
}

/// <summary>
/// Machine-readable categories emitted by the detector.
/// </summary>
public enum DigitalAddressFindingKind
{
    Conflict,
    OutOfRange,
    UnknownMultiplexerMapping
}

/// <summary>
/// Configurable protocol limits. Defaults match the currently supported Z21 commands.
/// </summary>
public sealed record DigitalAddressLimits(
    long LocomotiveMinimum = 1,
    long LocomotiveMaximum = 9999,
    long AccessoryMinimum = 1,
    long AccessoryMaximum = 2044,
    long FeedbackMinimum = 1,
    long FeedbackMaximum = int.MaxValue)
{
    internal (long Minimum, long Maximum) For(DigitalAddressDomain domain) => domain switch
    {
        DigitalAddressDomain.Locomotive => (LocomotiveMinimum, LocomotiveMaximum),
        DigitalAddressDomain.Accessory => (AccessoryMinimum, AccessoryMaximum),
        DigitalAddressDomain.Feedback => (FeedbackMinimum, FeedbackMaximum),
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
    };
}

/// <summary>
/// Stable project object identity carried to presentation code for navigation.
/// </summary>
public sealed record DigitalAddressOwner(Guid Id, string Name, string ObjectType);

/// <summary>
/// One inclusive address range occupied by a project object.
/// </summary>
public sealed record DigitalAddressAllocation(
    DigitalAddressDomain Domain,
    long Start,
    long End,
    DigitalAddressOwner Owner);

/// <summary>
/// Structured result that UI layers can render without reimplementing address rules.
/// </summary>
public sealed record DigitalAddressFinding(
    string Id,
    DigitalAddressFindingKind Kind,
    DigitalAddressDomain Domain,
    long Start,
    long End,
    IReadOnlyList<DigitalAddressOwner> Owners,
    string Message);

public sealed record DigitalAddressConflictReport(
    IReadOnlyList<DigitalAddressAllocation> Allocations,
    IReadOnlyList<DigitalAddressFinding> Findings)
{
    public bool HasConflicts => Findings.Any(finding => finding.Kind == DigitalAddressFindingKind.Conflict);

    public bool IsValid => Findings.Count == 0;
}

public interface IDigitalAddressConflictDetector
{
    DigitalAddressConflictReport Detect(Project project);
}

/// <summary>
/// Platform-neutral address validation. It deliberately has no UI dependency.
/// Locomotive primary addresses are exclusive. Wagon function-decoder addresses are deliberately
/// not allocated here because sharing one address is a supported way to control coach lighting.
/// Multiple traction should keep unique locomotive primary addresses and group commands at train level.
/// UI consumers should render <see cref="DigitalAddressConflictReport"/> and use owner IDs for navigation.
/// </summary>
public sealed class DigitalAddressConflictDetector : IDigitalAddressConflictDetector
{
    private readonly IMultiplexerProvider _multiplexers;
    private readonly DigitalAddressLimits _limits;

    public DigitalAddressConflictDetector(
        IMultiplexerProvider multiplexers,
        DigitalAddressLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(multiplexers);
        _multiplexers = multiplexers;
        _limits = limits ?? new DigitalAddressLimits();
    }

    public DigitalAddressConflictReport Detect(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var allocations = new List<DigitalAddressAllocation>();
        var findings = new List<DigitalAddressFinding>();

        foreach (var locomotive in project.Locomotives)
        {
            if (locomotive.DigitalAddress is { } address)
            {
                AddAllocation(
                    DigitalAddressDomain.Locomotive,
                    address,
                    address,
                    new DigitalAddressOwner(locomotive.Id, locomotive.Name, nameof(Locomotive)),
                    allocations,
                    findings);
            }
        }

        foreach (var element in project.SignalBoxPlan?.Elements ?? [])
        {
            switch (element)
            {
                case SbSwitch sbSwitch when sbSwitch.Address != 0:
                    AddAllocation(
                        DigitalAddressDomain.Accessory,
                        sbSwitch.Address,
                        sbSwitch.Address,
                        Owner(sbSwitch),
                        allocations,
                        findings);
                    break;

                case SbSignal signal when signal.BaseAddress != 0:
                    var maximumOffset = ResolveSignalMaximumOffset(signal, findings);
                    AddAllocation(
                        DigitalAddressDomain.Accessory,
                        signal.BaseAddress,
                        (long)signal.BaseAddress + maximumOffset,
                        Owner(signal),
                        allocations,
                        findings);
                    break;

                case SbDetector detector when detector.FeedbackAddress != 0:
                    AddAllocation(
                        DigitalAddressDomain.Feedback,
                        detector.FeedbackAddress,
                        detector.FeedbackAddress,
                        Owner(detector),
                        allocations,
                        findings);
                    break;
            }
        }

        AddConflicts(allocations, findings);

        var orderedAllocations = allocations
            .OrderBy(allocation => allocation.Domain)
            .ThenBy(allocation => allocation.Start)
            .ThenBy(allocation => allocation.End)
            .ThenBy(allocation => allocation.Owner.Id)
            .ToArray();
        var orderedFindings = findings
            .OrderBy(finding => finding.Domain)
            .ThenBy(finding => finding.Start)
            .ThenBy(finding => finding.End)
            .ThenBy(finding => finding.Kind)
            .ThenBy(finding => finding.Id, StringComparer.Ordinal)
            .ToArray();

        return new DigitalAddressConflictReport(orderedAllocations, orderedFindings);
    }

    private int ResolveSignalMaximumOffset(SbSignal signal, List<DigitalAddressFinding> findings)
    {
        if (!signal.IsMultiplexed)
            return 0;

        var article = signal.MultiplexerArticleNumber;
        if (!string.IsNullOrWhiteSpace(article))
        {
            try
            {
                if (_multiplexers.TryGetMaxAddressOffset(article, signal.MainSignalArticleNumber, out var offset))
                    return Math.Max(0, offset);
            }
            catch (ArgumentException)
            {
                // Converted below into a stable structured diagnostic.
            }
        }

        var owner = Owner(signal);
        findings.Add(new DigitalAddressFinding(
            FindingId(
                DigitalAddressFindingKind.UnknownMultiplexerMapping,
                DigitalAddressDomain.Accessory,
                signal.BaseAddress,
                signal.BaseAddress,
                [owner]),
            DigitalAddressFindingKind.UnknownMultiplexerMapping,
            DigitalAddressDomain.Accessory,
            signal.BaseAddress,
            signal.BaseAddress,
            [owner],
            "The multiplex address range cannot be calculated from the configured decoder and signal articles."));

        return 0;
    }

    private void AddAllocation(
        DigitalAddressDomain domain,
        long start,
        long end,
        DigitalAddressOwner owner,
        List<DigitalAddressAllocation> allocations,
        List<DigitalAddressFinding> findings)
    {
        var allocation = new DigitalAddressAllocation(domain, start, end, owner);
        allocations.Add(allocation);

        var (minimum, maximum) = _limits.For(domain);
        if (start >= minimum && end <= maximum)
            return;

        findings.Add(new DigitalAddressFinding(
            FindingId(DigitalAddressFindingKind.OutOfRange, domain, start, end, [owner]),
            DigitalAddressFindingKind.OutOfRange,
            domain,
            start,
            end,
            [owner],
            $"Address range {start}-{end} is outside the supported {domain} range {minimum}-{maximum}."));
    }

    private static void AddConflicts(
        IReadOnlyList<DigitalAddressAllocation> allocations,
        List<DigitalAddressFinding> findings)
    {
        foreach (var domainGroup in allocations.GroupBy(allocation => allocation.Domain))
        {
            var ordered = domainGroup
                .OrderBy(allocation => allocation.Start)
                .ThenBy(allocation => allocation.End)
                .ThenBy(allocation => allocation.Owner.Id)
                .ToArray();

            for (var leftIndex = 0; leftIndex < ordered.Length; leftIndex++)
            {
                var left = ordered[leftIndex];
                for (var rightIndex = leftIndex + 1; rightIndex < ordered.Length; rightIndex++)
                {
                    var right = ordered[rightIndex];
                    if (right.Start > left.End)
                        break;
                    if (right.End < left.Start)
                        continue;

                    var overlapStart = Math.Max(left.Start, right.Start);
                    var overlapEnd = Math.Min(left.End, right.End);
                    var owners = new[] { left.Owner, right.Owner }
                        .OrderBy(owner => owner.Id)
                        .ToArray();

                    var message = domainGroup.Key == DigitalAddressDomain.Locomotive
                        ? $"Locomotive primary address {overlapStart} is assigned to multiple locomotives. " +
                          "Keep primary addresses unique and use a train-level traction group for coordinated control."
                        : $"Address range {overlapStart}-{overlapEnd} is used by multiple {domainGroup.Key} objects.";

                    findings.Add(new DigitalAddressFinding(
                        FindingId(
                            DigitalAddressFindingKind.Conflict,
                            domainGroup.Key,
                            overlapStart,
                            overlapEnd,
                            owners),
                        DigitalAddressFindingKind.Conflict,
                        domainGroup.Key,
                        overlapStart,
                        overlapEnd,
                        owners,
                        message));
                }
            }
        }
    }

    private static DigitalAddressOwner Owner(SbElement element) =>
        new(element.Id, element.Name, element.GetType().Name);

    private static string FindingId(
        DigitalAddressFindingKind kind,
        DigitalAddressDomain domain,
        long start,
        long end,
        IEnumerable<DigitalAddressOwner> owners)
    {
        var ownerIds = string.Join(
            ",",
            owners.Select(owner => owner.Id.ToString("N")).Order(StringComparer.Ordinal));
        return $"{kind}:{domain}:{start}-{end}:{ownerIds}";
    }
}
