# Research: interfaces, options and limits

Source issue: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337). Researched 2026-10-18. Read-only public documentation and scoped local configuration inspection only; no account probe, credential access, cloud run or feasibility test. Decisions here resolve design questions into explicit options/constraints, not provider selections.

## Execution interfaces

| Interface / primary source | Established by documentation | Still unproven for our account/environment |
| --- | --- | --- |
| [Codex noninteractive execution](https://learn.chatgpt.com/docs/non-interactive-mode) | Scriptable exec and structured event/final outputs; explicit permissions | Entitlement, model availability, actual event/model provenance and remote termination |
| [Codex authentication](https://learn.chatgpt.com/docs/auth) and [pricing](https://learn.chatgpt.com/docs/pricing) | API-auth programmatic workflows and subscription usage are distinct; enterprise automation access has separate permission | Our approved auth route and included/paid usage; do not copy desktop credentials |
| [OpenAI self-hosted environments](https://developers.openai.com/api/docs/guides/agents-api/environments/self-hosted) | Application and environment keys can be separated; environment key has narrow purpose | Current account API access, security boundary, lifecycle reconciliation and cost |
| [Claude routines](https://code.claude.com/docs/en/web-scheduled-tasks) | Cloud execution; preview interface; independent event sessions, provider limits and possible dropped events | Existing routine identity/trigger/model/permissions and stale-run fencing; preserve rather than recreate it |
| [Opus 5.5](https://platform.claude.com/docs/en/models/opus-5-5/overview) | Documented model ID and API pricing | Actual independent reviewer model and approved subscription access at execution time |
| [Antigravity headless](https://antigravity.google/docs/cli/headless/) | Noninteractive output envelopes, status/response/usage | Supported remote access to the established Argon conversation and required actual model; no silent substitute |
| [Sonar CLI auth](https://docs.sonarsource.com/sonarqube-cli/quickstart-guide) / [automation variables](https://docs.sonarsource.com/sonarqube-cli/using-sonarqube-cli/environment-variables) | Native CLI and documented headless authentication variables; CLI auth differs from SonarScanner | Authenticated scanner availability per environment and strict per-read coverage; local Windows success does not prove cloud success |

Decision: keep execution/model/access/scanner evidence separate. CLI presence, advertised model and a subscription are insufficient. Existing Claude routine remains the route; lost/duplicate events require reconciliation without second review dispatch.

## Architecture options (no choice made)

| Option | Shape and documented basis | Benefits | Costs/limits and selection prerequisite |
| --- | --- | --- | --- |
| A: managed durable control | Small .NET isolated service with [Durable Task](https://learn.microsoft.com/en-us/azure/durable-task/common/programming-model-overview), transactional authoritative state and evidence storage in approved region | Managed checkpoint/replay mechanisms and cloud availability | Activities may repeat; deterministic orchestration required; external fencing still custom; regional data/services/ops need approval |
| B: small self-hosted control | One .NET service + PostgreSQL + evidence backup on approved cloud host; [transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html) supports transactional concurrency design; [Hetzner cloud](https://www.hetzner.com/cloud/) is an example host option | Small controllable deployment and explicit regional host choice | Patching, backups, restore, HA and incident duties are ours; one VM is not HA; database isolation does not fence GitHub |
| Worker alternative for A or B | Codex CLI in isolated workers or documented provider-managed/self-hosted agent environment; existing Claude routine and GitHub CI retained | Compare hosted lifecycle convenience with control of scanner/toolchain/region | Any environment that cannot demonstrate strict scans, actual model, revocation/reconciliation and evidence export is inadmissible |
| Existing CI as complete controller | Rejected as baseline replacement | Reuses jobs for qualified checks | Scheduled jobs/check status alone do not provide durable human decisions, lease/fencing or single queue; CI remains execution evidence |

Decision: prefer the smallest option that passes all authority/scanner/operation qualifications and approved geography/cost. Present A and B to the human via Senior; do not select or provision. No dedicated Windows worker until existing CI has an evidenced gap. [Durable replay constraints](https://learn.microsoft.com/en-us/azure/durable-task/common/durable-task-code-constraints) reinforce that replay safety is not external exactly-once behavior.

## Data location and retention options

[OpenAI data controls](https://developers.openai.com/api/docs/guides/your-data) and [Claude geographic controls](https://platform.claude.com/docs/en/manage-claude/data-residency) distinguish processing/storage controls and account-dependent availability. A German controller host does not establish all-provider EU-only processing, backups or support access. Public repository code may still be combined with private operational metadata.

Option L1: approved EU control/evidence store with separately documented provider processing exceptions. Option L2: approved provider-default locations after explicit data decision. Strict all-processing-in-EU is not established for the complete Codex/Claude/Argon chain and cannot be promised. D2 must list code/prompts/transcripts/authority/cost metadata, provider logs, backups, retention/deletion and export rights per service. Proposed operational event/evidence retention is 90 days plus preserve-until-accepted-obligations; final duration remains D2. No private model-railway project data enters the pilot.

## Illustrative cost model, not a quote or approval

Rates observed 2026-10-18: [OpenAI API pricing](https://developers.openai.com/api/docs/pricing), standard short-context gpt-6.1-sol input USD 2 / output USD 10 per million tokens; [Opus 5.5](https://platform.claude.com/docs/en/models/opus-5-5/overview), input USD 4 / output USD 20. These are illustrative model choices for arithmetic only, not selected worker models. Long context, caching, tools, speed tiers, regional uplift, taxes and rework change totals. Subscription consumption must be measured in its own units, not priced as included API calls.

Illustration per issue: 2M aggregate uncached Codex input + 0.2M output across short-context requests = USD 6; two separate review runs each 0.5M input + 0.05M output = USD 6 total. Combined USD 12 before fixes/tools/infrastructure/adviser/CI. At 10 such issues: USD 120 model component. Doubling tokens/review rounds doubles those components; this is no measured delivery cost.

Infrastructure planning assumptions, NOT provider list prices: small self-hosted control/store/backup EUR 15-60/month, managed control/store/logging EUR 40-150/month at low volume, workers/Windows/CI minutes additional. Obtain region/SKU/retention quotes from [Azure Functions pricing](https://azure.microsoft.com/en-us/pricing/details/functions/) or the host calculator before D3; these ranges are sensitivity inputs, not procurement evidence.

Total attributable issue cost = model usage + cache/tool/adviser charges + worker time + CI minutes/artifacts + allocated controller/storage/log/backup overhead + rework. Record currency/date and actual versus estimated amounts; do not silently sum USD/EUR. Reserved exposure includes running sessions and delayed billing. A provider dashboard soft cap is not a proven hard stop.

D3 must set approved per-issue/global daily/monthly ceilings, maximum duration/tokens/retries, alert threshold, running-cost reserve and safe stop budget. Proposed warning at 80%, deny uncovered new paid starts at 100%; no numerical spend ceiling is authorized now. WaitingHuman stops new task work but existing workers, storage and infrastructure may still cost money. Unknown metered exposure blocks further starts.

## External operations

[Mutation matrix](contracts/mutations.md) enumerates supported endpoint facts and unsupported guarantees. Key research conclusions:

- Git protocol carries old/new object IDs; a non-force push protects against incompatible concurrent branch updates, not stale-owner authority by itself.
- REST ref update documents sha/force but no expected-old-SHA field. A wrapper's expected_sha must not be misrepresented as native endpoint CAS.
- Merge endpoint can bind the expected head; it does not pin an expected base SHA. Required server protection and exclusive actual integration ownership must also be proved.
- Ready, comments, metadata and CI starts do not acquire fencing from clientMutationId or a prior GET.
- Scoped/revocable credentials help exclude future calls; documentation does not prove cancellation of already accepted in-flight operations.
- Until endpoint/broker/old-controller exclusion is qualified, the operation is disabled for autonomous admission. A compensating log is not prevention.

Additional primary endpoints: [GitHub commit-on-branch](https://docs.github.com/en/graphql/reference/commits) documents expectedHeadOid and is a candidate for supported patch publication. [Claude routine fire](https://platform.claude.com/docs/en/api/claude-code/routines-fire) explicitly has no idempotency key: successful calls create new sessions, and its fire token has no read scope or public token-management API. Therefore an uncertain fire cannot be safely repeated using the same token alone. [OpenAI session lifecycle](https://developers.openai.com/api/docs/guides/agents-api/sessions) distinguishes terminal turn events and cancel input; [session management](https://developers.openai.com/api/docs/guides/agents-api/sessions/manage) distinguishes cancel/retained state from deletion. These describe possible adapters, not qualification or a new runtime commitment.

## Reproducible qualification evidence

Static at base cc8c382d6d2c90dfe364dae581f5c3dff062e343: global.json SDK 10.0.402, latestPatch roll-forward, prerelease false; Quality Android job explicitly installs workload set 10.0.402. Actual resolved SDK must be recorded, not inferred from requested version. Windows app targets net10.0-windows10.0.22621.0, Android net10.0-android; Test also has net10.0.

Existing Windows CI uses windows-latest (a moving label): later evidence must capture actual image version, OS, VS/MSBuild, Windows SDK, JDK/Android SDK/build tools, MAUI/workload manifests, CLI/scanner versions, source/lock hashes and path. Short proposed Windows root C:/w/i337 is a qualification choice, not a settings/LongPathsEnabled change now. Existing Android Release job only runs on explicitly requested workflow dispatch. No image/canary or negative test was started here.

Original session reports about SARIF, staged line-ending exits, foreign writes and Windows paths are reported/unverified. Plan meaningful negative reproduction in isolation before a separate fix proposal; do not alter baselines to make failures green.
