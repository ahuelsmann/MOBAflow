'use strict';

const MAX_CHANGES = 200;
const MAX_BYTES = 32768;
const dependencyFile = name => /(?:^|\/)(?:Directory\.(?:Packages|Build)\.(?:props|targets)|[Nn]u[Gg]et\.[Cc]onfig|global\.json|packages\.(?:config|lock\.json)|package(?:-lock)?\.json|yarn\.lock|pnpm-lock\.yaml|requirements\.txt|poetry\.lock|Cargo\.(?:toml|lock)|go\.(?:mod|sum))$|\.(?:csproj|fsproj|vbproj|vcxproj|nuspec)$|^\.github\/workflows\/[^/]+\.ya?ml$/.test(name);
const nugetFile = name => /Directory\.(?:Packages|Build)\.(?:props|targets)$|\.(?:csproj|fsproj|vbproj|vcxproj|nuspec)$|(?:^|\/)(?:packages\.(?:config|lock\.json)|[Nn]u[Gg]et\.[Cc]onfig|global\.json)$/.test(name);
const escape = value => String(value ?? '').replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('|', '&#124;').replace(/[\r\n]/g, ' ').slice(0, 600);

function analyze(input) {
    const report = {base: input.base, head: input.head, observedAt: input.observedAt,
        state: 'FAILED', reasons: [], changes: [], newFindings: [], removedFindings: [],
        baseline: 'Not supplied by the dependency-diff API; unchanged existing findings are not counted as new or certified absent.'};
    if (input.error) {
        report.reasons.push(input.error);
        return report;
    }
    if (!Array.isArray(input.changes) || !Array.isArray(input.files) ||
        input.changes.some(row => !row || !['added', 'removed'].includes(row.change_type) ||
            !['manifest', 'ecosystem', 'name', 'version'].every(key => typeof row[key] === 'string') ||
            !Array.isArray(row.vulnerabilities) ||
            row.vulnerabilities.some(v => !v || typeof v.advisory_ghsa_id !== 'string' || typeof v.severity !== 'string'))) {
        report.reasons.push('Unexpected API response schema; no reliable comparison.');
        return report;
    }
    const relevant = input.files.filter(dependencyFile);
    const manifests = new Set(input.changes.map(row => row.manifest));
    if (input.snapshotWarnings) report.reasons.push('Missing/pending graph snapshots reported by GitHub.');
    if (input.truncated) report.reasons.push('Comparison exceeds the bounded report; not all rows were evaluated.');
    for (const file of relevant.filter(name => !manifests.has(name)))
        report.reasons.push('No dependency-diff data for changed input: ' + file);
    if (relevant.some(nugetFile))
        report.reasons.push('Resolved NuGet/central-package/transitive coverage is unverified. The existing Quality restore-graph audit remains authoritative.');

    const groups = new Map();
    for (const row of input.changes) {
        const key = JSON.stringify([row.manifest, row.ecosystem, row.name]);
        if (!groups.has(key)) groups.set(key, {manifest: row.manifest, ecosystem: row.ecosystem, name: row.name, added: [], removed: []});
        groups.get(key)[row.change_type].push(row.version);
        for (const finding of row.vulnerabilities) {
            const list = row.change_type === 'added' ? report.newFindings : report.removedFindings;
            if (!list.some(v => v.id === finding.advisory_ghsa_id && v.manifest === row.manifest && v.name === row.name && v.version === row.version))
                list.push({id: finding.advisory_ghsa_id, severity: finding.severity, manifest: row.manifest,
                    name: row.name, version: row.version, scope: row.scope ?? 'not supplied',
                    chain: 'Not supplied by this API'});
        }
    }
    report.changes = [...groups.values()].map(group => ({...group,
        kind: group.added.length && group.removed.length ? 'changed' : group.added.length ? 'added' : 'removed'}));
    report.state = report.newFindings.length ? 'FINDINGS' : report.reasons.length ? 'INCOMPLETE' : 'NO_NEW_FINDINGS';
    report.coverage = report.reasons.length ? 'INCOMPLETE' : 'GitHub-reported dependency delta for the named inputs only';
    report.relevantFiles = relevant;
    report.officialActionAllowed = !input.truncated && !input.snapshotWarnings && input.changes.length <= MAX_CHANGES &&
        Buffer.byteLength(JSON.stringify(input.changes)) <= MAX_BYTES;
    return report;
}

function render(report) {
    const lines = ['## Observational dependency review: ' + report.state, '',
        '- Base SHA: ' + escape(report.base), '- Head SHA: ' + escape(report.head),
        '- Observed: ' + escape(report.observedAt),
        '- Source: GitHub dependency-graph compare API (base...head; GitHub determines the merge base).',
        '- Coverage: ' + escape(report.coverage ?? 'Unavailable'),
        '- This is not a required gate. A green job is not a clean scan or a full-graph audit.',
        '- Unchanged baseline findings: ' + report.baseline, '',
        '### Added, changed and removed dependencies', '',
        '| Change | Manifest / ecosystem | Package | Before | After |',
        '| --- | --- | --- | --- | --- |'];
    for (const row of report.changes)
        lines.push('| ' + [row.kind, row.manifest + ' / ' + row.ecosystem, row.name,
            row.removed.join(', '), row.added.join(', ')].map(escape).join(' | ') + ' |');
    if (!report.changes.length) lines.push('', 'No dependency delta was returned; this does not prove graph coverage or absence of existing vulnerabilities.');
    for (const [title, list] of [['New potentially vulnerable additions/changes', report.newFindings], ['Removed vulnerable dependencies (not new findings)', report.removedFindings]]) {
        lines.push('', '### ' + title, '', '| Advisory | Severity | Manifest | Package / version | Scope / chain |',
            '| --- | --- | --- | --- | --- |');
        for (const row of list) lines.push('| ' + [row.id, row.severity, row.manifest,
            row.name + '@' + row.version, row.scope + ' / ' + row.chain].map(escape).join(' | ') + ' |');
        if (!list.length) lines.push('', report.state === 'FAILED' ? 'Not evaluated.' : 'None returned in this delta.');
    }
    lines.push('', '### Limits and ownership', ...report.reasons.map(reason => '- ' + escape(reason)),
        '- NuGet CPM and transitive restore-graph coverage is not certified by this check, even when rows are returned.',
        '- No new full-graph audit was run. Existing NuGet audit failures and required checks are unchanged.',
        '- Head of Quality triages findings; Senior PM assigns priority and the fix owner. The delivery owner proves fixes.',
        '- Material risk acceptance remains with the human, prepared by HQ and Senior PM. No automatic exceptions or suppressions.',
        '- Summaries/logs only; no configured cache/artifact upload. The official action has a one-day oversized-summary fallback.');
    return lines.join('\n') + '\n';
}

async function collect({github, context}) {
    const pr = context.payload.pull_request;
    const input = {base: pr.base.sha, head: pr.head.sha, observedAt: new Date().toISOString(), files: [], changes: []};
    try {
        const live = (await github.rest.pulls.get({...context.repo, pull_number: pr.number})).data;
        if (live.head.sha !== input.head || live.base.sha !== input.base) {
            input.error = 'PR comparison changed after this event; this run is stale, not a clean result.';
            return input;
        }
        input.files = [...new Set((await github.paginate(github.rest.pulls.listFiles,
            {...context.repo, pull_number: pr.number, per_page: 100}))
            .flatMap(file => [file.filename, file.previous_filename].filter(Boolean)))];
        if (input.files.length >= 3000) input.truncated = true;
        const params = {...context.repo, basehead: input.base + '...' + input.head, per_page: 100};
        for await (const response of github.paginate.iterator('GET /repos/{owner}/{repo}/dependency-graph/compare/{basehead}', params)) {
            if (!Array.isArray(response.data)) throw new Error('schema');
            if (response.headers['x-github-dependency-graph-snapshot-warnings']) input.snapshotWarnings = true;
            input.changes.push(...response.data);
            if (input.changes.length > MAX_CHANGES || Buffer.byteLength(JSON.stringify(input.changes)) > MAX_BYTES) {
                input.changes = [];
                input.truncated = true;
                break;
            }
        }
    } catch (error) {
        input.error = 'Dependency comparison unavailable: HTTP ' + (Number.isInteger(error.status) ? error.status : 'unknown') +
            ' / API or parse failure. Check graph availability, access and the exact revisions; no findings count is certified.';
    }
    return input;
}

async function run({github, context, core}) {
    const report = analyze(await collect({github, context}));
    await core.summary.addRaw(render(report)).write();
    if (report.state !== 'NO_NEW_FINDINGS') core.warning('Dependency review diagnostic: ' + report.state + '. Read the job summary; existing gates remain unchanged.');
    core.setOutput('official-action-allowed', String(report.officialActionAllowed === true));
    core.setOutput('diagnostic', report.state);
}
module.exports = {analyze, render, collect, run};
