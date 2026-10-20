'use strict';

const {test} = require('node:test');
const assert = require('node:assert/strict');
const {analyze, render, collect} = require('./dependency-review.cjs');
const input = extra => ({base: 'a'.repeat(40), head: 'b'.repeat(40), observedAt: '2026-10-20T00:00:00Z',
    files: [], changes: [], ...extra});
const advisory = {advisory_ghsa_id: 'GHSA-test-fixture-only', severity: 'high'};
const row = (change_type, extra = {}) => ({change_type, manifest: 'app.csproj', ecosystem: 'nuget',
    name: 'Fixture.Package', version: '1.0.0', vulnerabilities: [], ...extra});

test('empty delta is scoped and does not certify absent unchanged baseline findings', () => {
    const report = analyze(input());
    assert.equal(report.state, 'NO_NEW_FINDINGS');
    assert.equal(report.newFindings.length, 0);
    assert.match(render(report), /unchanged existing findings are not counted as new or certified absent/);
    assert.match(render(report), /does not prove graph coverage/);
});
test('vulnerable addition retains advisory, version and manifest with partial coverage', () => {
    const report = analyze(input({files: ['app.csproj'], changes: [row('added', {vulnerabilities: [advisory]})]}));
    assert.equal(report.state, 'FINDINGS');
    assert.equal(report.newFindings[0].id, advisory.advisory_ghsa_id);
    assert.equal(report.newFindings[0].manifest, 'app.csproj');
    assert.equal(report.coverage, 'INCOMPLETE');
});
test('vulnerable removal is separate from new findings', () => {
    const report = analyze(input({changes: [row('removed', {vulnerabilities: [advisory]})]}));
    assert.equal(report.state, 'NO_NEW_FINDINGS');
    assert.equal(report.newFindings.length, 0);
    assert.equal(report.removedFindings.length, 1);
    assert.equal(report.changes[0].kind, 'removed');
});
test('version change pairs removed and added versions', () => {
    const report = analyze(input({changes: [row('removed'), row('added', {version: '2.0.0'})]}));
    assert.equal(report.changes.length, 1);
    assert.equal(report.changes[0].kind, 'changed');
    assert.deepEqual(report.changes[0].removed, ['1.0.0']);
    assert.deepEqual(report.changes[0].added, ['2.0.0']);
});
test('advisory deduplication retains different affected manifests', () => {
    const a = row('added', {vulnerabilities: [advisory]});
    const report = analyze(input({changes: [a, a, {...a, manifest: 'other.csproj'}]}));
    assert.equal(report.newFindings.length, 2);
});
test('missing central NuGet data cannot produce a clean result', () => {
    const report = analyze(input({files: ['Directory.Packages.props']}));
    assert.equal(report.state, 'INCOMPLETE');
    assert.match(report.reasons.join(' '), /No dependency-diff data/);
    assert.match(report.reasons.join(' '), /transitive coverage is unverified/);
});
test('direct NuGet rows do not prove restored transitive coverage', () => {
    assert.equal(analyze(input({files: ['app.csproj'], changes: [row('added')]})).state, 'INCOMPLETE');
});
test('missing snapshots and bounded data are incomplete, never clean', () => {
    for (const flags of [{snapshotWarnings: true}, {truncated: true}]) {
        const report = analyze(input(flags));
        assert.equal(report.state, 'INCOMPLETE');
        assert.equal(report.officialActionAllowed, false);
    }
});
test('API, auth and rate failures are not zero findings', () => {
    for (const status of [401, 403, 404, 429, 503]) {
        const report = analyze(input({error: 'HTTP ' + status}));
        assert.equal(report.state, 'FAILED');
        assert.match(render(report), /Not evaluated/);
    }
});
test('malformed responses and unrecognized change types fail', () => {
    for (const changes of [null, {}, [row('unchanged')], [row('added', {vulnerabilities: null})]])
        assert.equal(analyze(input({changes})).state, 'FAILED');
});
test('summary escapes untrusted paths and package names', () => {
    const report = analyze(input({changes: [row('added', {name: '<script>|bad\nname <SCRIPT>upper</SCRIPT>'})]}));
    assert.equal(render(report).includes('<'), false);
    assert.match(render(report), /&lt;script&gt;&#124;bad name/);
    assert.ok(render(report).includes('&lt;SCRIPT&gt;upper&lt;/SCRIPT&gt;'));
});
test('API failure cannot enable the official action with empty data', () => {
    assert.notEqual(analyze(input({error: 'missing data'})).officialActionAllowed, true);
});
test('collector retains snapshot diagnostics and access failures', async () => {
    const iterator = async function* () {yield {data: [row('added')], headers: {'x-github-dependency-graph-snapshot-warnings': 'pending'}};};
    const github = {rest: {pulls: {get: async () => ({data: {base: {sha: 'a'}, head: {sha: 'b'}}}), listFiles: 'files'}},
        paginate: Object.assign(async () => [{filename: 'app.csproj'}], {iterator})};
    const context = {repo: {owner: 'fixture', repo: 'fixture'}, payload: {pull_request: {number: 1, base: {sha: 'a'}, head: {sha: 'b'}}}};
    assert.equal(analyze(await collect({github, context})).state, 'INCOMPLETE');
    github.rest.pulls.get = async () => {throw Object.assign(new Error('fixture'), {status: 403});};
    assert.equal(analyze(await collect({github, context})).state, 'FAILED');
});
test('stale PR event fails before obtaining graph data', async () => {
    const github = {rest: {pulls: {get: async () => ({data: {base: {sha: 'new'}, head: {sha: 'b'}}})}}};
    const context = {repo: {}, payload: {pull_request: {number: 1, base: {sha: 'a'}, head: {sha: 'b'}}}};
    assert.match((await collect({github, context})).error, /stale/);
});
