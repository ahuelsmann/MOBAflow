// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Explains why anonymous-read enforcement cannot be enabled yet.
/// </summary>
public enum CompatibilityReadBlockingReason
{
    None,
    NoStableClientRelease,
    CriticalDefectOpen,
    ObservationWindowIncomplete,
    NoAuthenticatedTraffic,
    EvidenceNotRecorded
}

/// <summary>
/// Identifies the transport that produced a compatibility-read observation.
/// </summary>
public enum CompatibilityReadTransport
{
    Rest,
    SignalR
}

/// <summary>
/// Defines the non-secret protocol header used to bind readiness evidence to a stable client release.
/// </summary>
public static class CompatibilityReadHeaders
{
    /// <summary>Contains the stable client release identifier, for example <c>MOBAsmart/1.0.0</c>.</summary>
    public const string ClientRelease = "X-MOBAflow-Client-Release";
}

/// <summary>
/// Describes how an anonymous read request is handled during migration.
/// </summary>
public enum CompatibilityReadDecision
{
    AllowedCompatibility,
    AllowedRollback,
    UpgradeRequired
}

/// <summary>
/// Identifies the safe outcome recorded for a compatibility read.
/// </summary>
public enum CompatibilityReadOutcome
{
    AuthenticatedAllowed,
    AnonymousCompatibilityAllowed,
    AnonymousRollbackAllowed,
    UpgradeRequired
}

/// <summary>
/// Aggregates read outcomes without exposing a reusable credential identifier.
/// </summary>
public sealed record CompatibilityReadOutcomeCount(
    CompatibilityReadTransport Transport,
    CompatibilityReadOutcome Outcome,
    string PseudonymousClientId,
    long Count);

/// <summary>
/// Contains bounded, safe compatibility telemetry.
/// </summary>
public sealed record CompatibilityReadTelemetry(
    IReadOnlyList<CompatibilityReadOutcomeCount> Outcomes);

/// <summary>
/// Safe operational state for the anonymous-read migration gate.
/// </summary>
public sealed record CompatibilityReadMigrationStatus(
    bool IsReadyForEnforcement,
    CompatibilityReadBlockingReason BlockingReason,
    string? StableClientRelease,
    DateTimeOffset? ObservationStartedAt,
    long AuthenticatedReadCount,
    int OpenCriticalDefectCount,
    bool EvidenceRecorded,
    DateTimeOffset? AuthenticatedReadsEnforcedAt,
    DateTimeOffset? RollbackExpiresAt);

/// <summary>
/// Coordinates the measured transition from anonymous compatibility reads to authenticated reads.
/// </summary>
public interface ICompatibilityReadMigration
{
    /// <summary>Starts a fourteen-day observation window for an already published stable client release.</summary>
    Task BeginReadinessWindowAsync(string stableClientRelease, CancellationToken cancellationToken = default);

    /// <summary>Records an authenticated outcome and stable-release transport evidence.</summary>
    Task RecordAuthenticatedReadAsync(
        string credentialId,
        CompatibilityReadTransport transport,
        string? clientRelease = null,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a critical migration defect as open so enforcement remains blocked.</summary>
    Task RecordCriticalDefectAsync(
        string defectCode,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a critical defect fixed and restarts the complete observation window.</summary>
    Task RecordCriticalDefectFixedAsync(
        string defectCode,
        CancellationToken cancellationToken = default);

    /// <summary>Records the exact issue comment containing readiness evidence.</summary>
    Task RecordIssueEvidenceAsync(string evidenceReference, CancellationToken cancellationToken = default);

    /// <summary>Enables authenticated-only reads after every readiness condition passes.</summary>
    Task<bool> EnableAuthenticatedReadsAsync(CancellationToken cancellationToken = default);

    /// <summary>Activates the read-only rollback for no more than seven days.</summary>
    Task<bool> ActivateAnonymousReadRollbackAsync(
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>Evaluates an anonymous read without granting control or administrative capability.</summary>
    Task<CompatibilityReadDecision> EvaluateAnonymousReadAsync(
        CompatibilityReadTransport transport,
        CancellationToken cancellationToken = default);

    /// <summary>Returns bounded, process-local pseudonymous outcome counters.</summary>
    Task<CompatibilityReadTelemetry> GetTelemetryAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the persisted migration gate state.</summary>
    Task<CompatibilityReadMigrationStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

internal sealed class CompatibilityReadMigration : ICompatibilityReadMigration
{
    private const string StorePurpose = "MOBApi.ControlPlane.CompatibilityReadMigration.v1";
    private const int MaximumPseudonymousClients = 64;
    private static readonly EventId RollbackActivatedEvent = new(5002, "CompatibilityReadRollbackActivated");
    private static readonly TimeSpan RequiredObservationWindow = TimeSpan.FromDays(14);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<CompatibilityReadOutcomeCounter> _outcomeCounts = [];
    private readonly byte[] _pseudonymKey = RandomNumberGenerator.GetBytes(32);
    private readonly ILogger<CompatibilityReadMigration> _logger;
    private readonly CompatibilityReadMetrics _metrics;
    private readonly ProtectedDocumentStore<CompatibilityReadMigrationDocument> _store;
    private readonly TimeProvider _timeProvider;
    private CompatibilityReadMigrationDocument? _cachedDocument;

    public CompatibilityReadMigration(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider,
        CompatibilityReadMetrics metrics,
        ILogger<CompatibilityReadMigration> logger)
    {
        _timeProvider = timeProvider;
        _metrics = metrics;
        _logger = logger;
        var path = Path.Combine(options.Value.ResolveStorageDirectory(), "read-migration.dat");
        _store = new ProtectedDocumentStore<CompatibilityReadMigrationDocument>(
            dataProtectionProvider,
            StorePurpose,
            path);
    }

    public async Task BeginReadinessWindowAsync(
        string stableClientRelease,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableClientRelease);
        if (stableClientRelease.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(stableClientRelease));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            EnsureWindowMutable(document);
            document.StableClientRelease = stableClientRelease.Trim();
            document.ObservationStartedAt = _timeProvider.GetUtcNow();
            document.AuthenticatedReadCount = 0;
            document.HasAuthenticatedRestRead = false;
            document.HasAuthenticatedSignalRRead = false;
            document.EvidenceReference = null;
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordIssueEvidenceAsync(
        string evidenceReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        if (evidenceReference.Length > 512 ||
            !Uri.TryCreate(evidenceReference, UriKind.Absolute, out var evidenceUri) ||
            evidenceUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(evidenceUri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                evidenceUri.AbsolutePath.TrimEnd('/'),
                "/ahuelsmann/MOBAflow/issues/50",
                StringComparison.OrdinalIgnoreCase) ||
            !evidenceUri.Fragment.StartsWith("#issuecomment-", StringComparison.Ordinal) ||
            !long.TryParse(
                evidenceUri.Fragment["#issuecomment-".Length..],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out _))
        {
            throw new ArgumentException(
                "Readiness evidence must link to a concrete comment in MOBAflow GitHub issue #50 over HTTPS.",
                nameof(evidenceReference));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            EnsureWindowMutable(document);
            document.EvidenceReference = evidenceReference.Trim();
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordAuthenticatedReadAsync(
        string credentialId,
        CompatibilityReadTransport transport,
        string? clientRelease = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            RecordOutcome(
                transport,
                CompatibilityReadOutcome.AuthenticatedAllowed,
                CreatePseudonymousClientId(credentialId));
            if (document.ObservationStartedAt is null ||
                !string.Equals(document.StableClientRelease, clientRelease?.Trim(), StringComparison.Ordinal))
                return;

            var alreadyObserved = transport == CompatibilityReadTransport.Rest
                ? document.HasAuthenticatedRestRead
                : document.HasAuthenticatedSignalRRead;
            if (alreadyObserved)
                return;

            if (transport == CompatibilityReadTransport.Rest)
                document.HasAuthenticatedRestRead = true;
            else
                document.HasAuthenticatedSignalRRead = true;
            document.AuthenticatedReadCount++;
            try
            {
                await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (transport == CompatibilityReadTransport.Rest)
                    document.HasAuthenticatedRestRead = false;
                else
                    document.HasAuthenticatedSignalRRead = false;
                document.AuthenticatedReadCount--;
                _logger.LogWarning(
                    new EventId(5003, "CompatibilityReadEvidencePersistenceFailed"),
                    exception,
                    "Authenticated read evidence could not be persisted. The read remains authorized, but the migration gate was not advanced.");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordCriticalDefectAsync(
        string defectCode,
        CancellationToken cancellationToken = default)
    {
        ValidateDefectCode(defectCode);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            EnsureWindowMutable(document);
            if (document.ObservationStartedAt is null)
                throw new InvalidOperationException("Start the readiness window before recording a critical defect.");

            var normalizedCode = defectCode.Trim();
            if (!document.OpenCriticalDefectCodes.Contains(normalizedCode, StringComparer.Ordinal))
                document.OpenCriticalDefectCodes.Add(normalizedCode);
            document.EvidenceReference = null;
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordCriticalDefectFixedAsync(
        string defectCode,
        CancellationToken cancellationToken = default)
    {
        ValidateDefectCode(defectCode);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            EnsureWindowMutable(document);
            if (document.ObservationStartedAt is null)
                throw new InvalidOperationException("Start the readiness window before fixing a critical defect.");

            var normalizedCode = defectCode.Trim();
            if (!document.OpenCriticalDefectCodes.Remove(normalizedCode))
                throw new InvalidOperationException("The critical defect is not currently open.");

            document.ObservationStartedAt = _timeProvider.GetUtcNow();
            document.AuthenticatedReadCount = 0;
            document.HasAuthenticatedRestRead = false;
            document.HasAuthenticatedSignalRRead = false;
            document.EvidenceReference = null;
            document.LastCriticalDefectCode = normalizedCode;
            document.LastCriticalDefectFixedAt = document.ObservationStartedAt;
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> EnableAuthenticatedReadsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            if (GetBlockingReason(document) != CompatibilityReadBlockingReason.None)
                return false;

            document.AuthenticatedReadsEnforcedAt ??= _timeProvider.GetUtcNow();
            document.RollbackExpiresAt = null;
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            _metrics.SetRollbackExpiry(null);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> ActivateAnonymousReadRollbackAsync(
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromDays(7))
            return false;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            if (document.AuthenticatedReadsEnforcedAt is null)
                return false;

            document.RollbackExpiresAt = _timeProvider.GetUtcNow().Add(duration);
            await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            _metrics.SetRollbackExpiry(document.RollbackExpiresAt);
            _logger.LogWarning(
                RollbackActivatedEvent,
                "The anonymous read-only rollback was activated for {RollbackDuration} and expires at {RollbackExpiresAt}. Anonymous control and administration remain disabled.",
                duration,
                document.RollbackExpiresAt);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CompatibilityReadDecision> EvaluateAnonymousReadAsync(
        CompatibilityReadTransport transport,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            _metrics.SetRollbackExpiry(document.RollbackExpiresAt);
            var decision = document.AuthenticatedReadsEnforcedAt is null
                ? CompatibilityReadDecision.AllowedCompatibility
                : document.RollbackExpiresAt > _timeProvider.GetUtcNow()
                    ? CompatibilityReadDecision.AllowedRollback
                    : CompatibilityReadDecision.UpgradeRequired;
            var outcome = decision switch
            {
                CompatibilityReadDecision.AllowedCompatibility => CompatibilityReadOutcome.AnonymousCompatibilityAllowed,
                CompatibilityReadDecision.AllowedRollback => CompatibilityReadOutcome.AnonymousRollbackAllowed,
                _ => CompatibilityReadOutcome.UpgradeRequired
            };
            RecordOutcome(transport, outcome, "anonymous");
            return decision;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CompatibilityReadTelemetry> GetTelemetryAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return new CompatibilityReadTelemetry(_outcomeCounts
                .Select(outcome => new CompatibilityReadOutcomeCount(
                    outcome.Transport,
                    outcome.Outcome,
                    outcome.PseudonymousClientId,
                    outcome.Count))
                .OrderBy(outcome => outcome.Transport)
                .ThenBy(outcome => outcome.Outcome)
                .ThenBy(outcome => outcome.PseudonymousClientId, StringComparer.Ordinal)
                .ToArray());
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CompatibilityReadMigrationStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            var blockingReason = GetBlockingReason(document);
            return new CompatibilityReadMigrationStatus(
                blockingReason == CompatibilityReadBlockingReason.None,
                blockingReason,
                document.StableClientRelease,
                document.ObservationStartedAt,
                document.AuthenticatedReadCount,
                document.OpenCriticalDefectCodes.Count,
                !string.IsNullOrWhiteSpace(document.EvidenceReference),
                document.AuthenticatedReadsEnforcedAt,
                document.RollbackExpiresAt);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CompatibilityReadMigrationDocument> LoadDocumentAsync(
        CancellationToken cancellationToken)
    {
        _cachedDocument ??= await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return _cachedDocument;
    }

    private static void EnsureWindowMutable(CompatibilityReadMigrationDocument document)
    {
        if (document.AuthenticatedReadsEnforcedAt is not null)
        {
            throw new InvalidOperationException(
                "The readiness window cannot be changed after authenticated-read enforcement.");
        }
    }

    private static void ValidateDefectCode(string defectCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defectCode);
        if (defectCode.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(defectCode));
    }

    private CompatibilityReadBlockingReason GetBlockingReason(CompatibilityReadMigrationDocument document)
    {
        if (document.ObservationStartedAt is null || string.IsNullOrWhiteSpace(document.StableClientRelease))
            return CompatibilityReadBlockingReason.NoStableClientRelease;
        if (document.OpenCriticalDefectCodes.Count > 0)
            return CompatibilityReadBlockingReason.CriticalDefectOpen;
        if (_timeProvider.GetUtcNow() - document.ObservationStartedAt < RequiredObservationWindow)
            return CompatibilityReadBlockingReason.ObservationWindowIncomplete;
        if (!document.HasAuthenticatedRestRead || !document.HasAuthenticatedSignalRRead)
            return CompatibilityReadBlockingReason.NoAuthenticatedTraffic;
        return !string.IsNullOrWhiteSpace(document.EvidenceReference)
            ? CompatibilityReadBlockingReason.None
            : CompatibilityReadBlockingReason.EvidenceNotRecorded;
    }

    private string CreatePseudonymousClientId(string credentialId)
    {
        var digest = HMACSHA256.HashData(_pseudonymKey, Encoding.UTF8.GetBytes(credentialId));
        return Convert.ToHexString(digest)[..16];
    }

    private void RecordOutcome(
        CompatibilityReadTransport transport,
        CompatibilityReadOutcome outcome,
        string pseudonymousClientId)
    {
        if (pseudonymousClientId != "anonymous" &&
            !_outcomeCounts.Any(candidate => string.Equals(
                candidate.PseudonymousClientId,
                pseudonymousClientId,
                StringComparison.Ordinal)) &&
            _outcomeCounts.Select(candidate => candidate.PseudonymousClientId)
                .Where(candidate => candidate != "anonymous" && candidate != "overflow")
                .Distinct(StringComparer.Ordinal)
                .Count() >= MaximumPseudonymousClients)
        {
            pseudonymousClientId = "overflow";
        }

        var counter = _outcomeCounts.SingleOrDefault(candidate =>
            candidate.Transport == transport &&
            candidate.Outcome == outcome &&
            string.Equals(candidate.PseudonymousClientId, pseudonymousClientId, StringComparison.Ordinal));
        if (counter is null)
        {
            _outcomeCounts.Add(new CompatibilityReadOutcomeCounter
            {
                Transport = transport,
                Outcome = outcome,
                PseudonymousClientId = pseudonymousClientId,
                Count = 1
            });
            _metrics.RecordReadOutcome(transport, outcome);
            return;
        }

        counter.Count++;
        _metrics.RecordReadOutcome(transport, outcome);
    }

    private sealed class CompatibilityReadMigrationDocument
    {
        public string? StableClientRelease { get; set; }

        public DateTimeOffset? ObservationStartedAt { get; set; }

        public long AuthenticatedReadCount { get; set; }

        public string? EvidenceReference { get; set; }

        public string? LastCriticalDefectCode { get; set; }

        public DateTimeOffset? LastCriticalDefectFixedAt { get; set; }

        public DateTimeOffset? AuthenticatedReadsEnforcedAt { get; set; }

        public DateTimeOffset? RollbackExpiresAt { get; set; }

        public bool HasAuthenticatedRestRead { get; set; }

        public bool HasAuthenticatedSignalRRead { get; set; }

        public List<string> OpenCriticalDefectCodes { get; set; } = [];
    }

    private sealed class CompatibilityReadOutcomeCounter
    {
        public CompatibilityReadTransport Transport { get; set; }

        public CompatibilityReadOutcome Outcome { get; set; }

        public string PseudonymousClientId { get; set; } = string.Empty;

        public long Count { get; set; }
    }
}