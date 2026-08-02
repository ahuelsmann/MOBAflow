// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Net;
using System.Text.Json.Serialization;

namespace Moba.MOBApi.Security;

/// <summary>
/// Confirms that an authenticated-read migration reference resolves to current issue evidence.
/// </summary>
public interface ICompatibilityReadEvidenceVerifier
{
    /// <summary>Validates one issue comment against the stable release and completed observation window.</summary>
    Task VerifyAsync(
        Uri evidenceUri,
        string stableClientRelease,
        DateTimeOffset observationCompletedAt,
        CancellationToken cancellationToken);
}

internal sealed class GitHubIssueEvidenceVerifier(IHttpClientFactory httpClientFactory)
    : ICompatibilityReadEvidenceVerifier
{
    internal const string HttpClientName = "CompatibilityReadEvidence";
    private const string ExpectedIssueApiUrl = "https://api.github.com/repos/ahuelsmann/MOBAflow/issues/50";

    public async Task VerifyAsync(
        Uri evidenceUri,
        string stableClientRelease,
        DateTimeOffset observationCompletedAt,
        CancellationToken cancellationToken)
    {
        var commentId = evidenceUri.Fragment["#issuecomment-".Length..];
        using var response = await httpClientFactory
            .CreateClient(HttpClientName)
            .GetAsync(new Uri($"repos/ahuelsmann/MOBAflow/issues/comments/{commentId}", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("The referenced issue #50 evidence comment does not exist.");

        response.EnsureSuccessStatusCode();
        var comment = await response.Content
            .ReadFromJsonAsync<GitHubIssueComment>(cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidDataException("GitHub returned an empty evidence comment.");

        var expectedReleaseLine = $"Stable client release: {stableClientRelease}";
        var evidenceLines = comment.Body.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!Uri.TryCreate(comment.HtmlUrl, UriKind.Absolute, out var resolvedCommentUri) ||
            resolvedCommentUri != evidenceUri ||
            !string.Equals(comment.IssueUrl, ExpectedIssueApiUrl, StringComparison.OrdinalIgnoreCase) ||
            comment.CreatedAt <= observationCompletedAt ||
            !evidenceLines.Contains("Slice 4e readiness evidence", StringComparer.Ordinal) ||
            !evidenceLines.Contains(expectedReleaseLine, StringComparer.Ordinal) ||
            !evidenceLines.Contains("Observation result: passed", StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The referenced issue #50 comment is not valid Slice 4e readiness evidence for the completed observation window.");
        }
    }

    private sealed record GitHubIssueComment(
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("issue_url")] string IssueUrl,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("body")] string Body);
}
