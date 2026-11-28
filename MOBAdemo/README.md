# MOBAdemo

MOBAdemo is the browser showcase of MOBAflow (issue #430, spec `specs/020-web-demo/`). It has two parts:

| Folder | What it is |
| --- | --- |
| `Host/` | A small ASP.NET Core host that serves a prepared folder of static files (`wwwroot`) and a health answer. It never runs npm or Node, so building it works on a machine without Node. |
| `app/` | The React, TypeScript and Vite frontend. See `app/README.md`. |

## Build the web app

Requirements: PowerShell 7 (`pwsh`), Node.js 24 and npm 11. The script runs on Windows and Linux.

```text
pwsh MOBAdemo/build.ps1 [-Configuration Release] [-OutputPath <folder>]
```

The script:

1. runs `npm ci` and `npm run build` in `MOBAdemo/app` (Vite gives every file under `assets/` a content-hashed name),
2. writes a Brotli (quality 11) and a gzip sibling (`app.js.br`, `app.js.gz`) next to every `.js`, `.css`, `.html`
   and `.svg` file, with the small script `app/scripts/compress-assets.mjs` (Node's built-in `zlib`),
3. stages the complete result in a sibling folder and then swaps it for the output folder (old folder aside, new
   folder in, old folder deleted), so no file of an older build stays behind. A failed build or copy leaves the old
   output untouched; the swap restores it when it fails half way.

The default output is `MOBAdemo/Host/wwwroot-build/`, which Git ignores. `-OutputPath` chooses another folder
(tests and CI use it). Any pre-existing target is replaced only when it is empty or carries the
`.mobademo-build-owned` marker written by this script (which `Host/MOBAdemo.csproj` excludes from build and publish
output); non-empty folders or files without that marker are refused. Inside the repository only the default folder
may be replaced, and any path that is or runs through a symbolic link, junction, or reparse point (including
`wwwroot-build` or any parent) is refused. `-AppPath` builds another copy of the frontend folder (tests use it).
The committed placeholder `Host/wwwroot/index.html` is never changed.

## Run or publish the host

The host project uses `wwwroot-build` as its web root whenever that folder contains an `index.html`; otherwise it
uses the placeholder `wwwroot`. A local run and a publish behave the same:

```text
pwsh MOBAdemo/build.ps1
dotnet run --project MOBAdemo/Host/MOBAdemo.csproj
dotnet publish MOBAdemo/Host/MOBAdemo.csproj -c Release -o <folder>
```

Delete `MOBAdemo/Host/wwwroot-build/` to go back to the placeholder. Every build and publish recreates the `wwwroot`
folder of its output folder, so a reused folder never keeps hashed assets of an older build or a built
`index.html` after you went back to the placeholder.

## Tests

`dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~MOBAdemo"` runs the
host tests and the build tests in `Test/MOBAdemo/Build`, which run the script once into a temporary folder. They are
ignored with a clear reason when `pwsh`, `node` or `npm` is missing. Frontend unit tests: `npm test` in `MOBAdemo/app`.

## Regenerate the example track plan

The committed `app/public/assets/track-plan.svg` is rendered from the shipped
`MOBAflow/track-plan.json` with the same placed-track renderer used by MOBAflow.
Display it as a self-contained image in both Light and Dark. Its opaque dark inset surface fills the viewBox
and provides the contrast background for the opaque rails and labels. The SVG needs no page styles,
inline stylesheet or external resources. The portable asset tests enforce at least 3:1 for rails, 4.5:1
for labels and a 30,000-byte limit.

From the repository root in PowerShell, regenerate it only after an intended sample or renderer change:

```powershell
$env:MOBADEMO_UPDATE_TRACK_PLAN_ASSET = '1'
try {
    dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~CommittedLayout_EqualsTheRenderingOfTheShippedSample"
}
finally {
    Remove-Item Env:MOBADEMO_UPDATE_TRACK_PLAN_ASSET
}
```

Review the SVG diff and run the portable track-plan asset tests without the switch.
Without the switch, the tests only read the sample and SVG and fail on drift.
The app and its build use the committed SVG directly.

## Bundle budget

FR-076 limits the scripts and style sheets together to 200 KB with Brotli. `Test-BundleBudget.ps1` measures every
`.js`, `.mjs` and `.css` file of a built web root (lazy chunks included; the `.br` and `.gz` siblings are ignored),
fails above 200,000 bytes and prints the sizes. `Test-BundleBudget.Tests.ps1` proves it on synthetic folders.

```text
pwsh MOBAdemo/build.ps1
pwsh MOBAdemo/Test-BundleBudget.ps1 -Path MOBAdemo/Host/wwwroot-build
```

## CI

The `mobademo` job of `.github/workflows/quality.yml` (check `MOBAdemo frontend, audit and bundle budget`) runs for
changes below `MOBAdemo/`, `Test/MOBAdemo/` and the shared build inputs. In order: the lockfile audit
(`npm audit` for high and critical vulnerabilities, before any install), the budget script self-tests, `npm ci`,
lint, frontend unit tests, `build.ps1`, the budget check and the `Moba.Test.MOBAdemo` NUnit tests. It needs Node and
the .NET SDK only, no WinUI or Android workload.

## Deployment and operations

### Hosting target (`FR-050`)

MOBAdemo is hosted on Azure App Service at `https://mobaflow-demo.azurewebsites.net/` (resource group
`rg-mobaflow-demo`, web app `mobaflow-demo`), Free tier (`F1`), France Central, running ASP.NET Core on .NET 10 under
Linux:

- **Startup command**: `dotnet MOBAdemo.dll`
- **Build on deploy**: disabled (`SCM_DO_BUILD_DURING_DEPLOYMENT=false`) so App Service serves the prebuilt .NET output
  and precompressed `wwwroot` assets directly without running Oryx, Node or `dotnet build` on the `F1` instance
- **Transport hardening**: HTTPS-only enabled, minimum TLS 1.2, HTTP/2 enabled and FTPS disabled

### Passwordless GitHub Actions deployment (`FR-054`)

`.github/workflows/deploy-mobademo.yml` (`Deploy MOBAdemo`) is started manually via `workflow_dispatch` on GitHub. It
audits `package-lock.json`, runs frontend lint and unit tests, executes `MOBAdemo/build.ps1 -Configuration Release`,
enforces the 200 KB Brotli bundle budget, publishes `MOBAdemo/Host/MOBAdemo.csproj`, deploys the zip package with
`az webapp deploy --type zip --clean true --track-status false`, and verifies `/` and `/health` over HTTPS.

Authentication uses passwordless OpenID Connect (OIDC) federation between GitHub Actions (`id-token: write`) and
Microsoft Entra ID (`azure/login`), with `Website Contributor` scoped strictly to the `mobaflow-demo` web app resource.
No publish profile, client secret or local path is stored in the repository; only the non-secret OIDC identifiers
(`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`) are configured in the `mobademo` GitHub Actions
environment and repository variables.

### Slotless Free-tier replacement (`FR-055`)

The App Service Free tier (`F1`) has no deployment slots. Each zip deployment (`--clean true`) replaces the live files
in place and restarts `mobaflow-demo` automatically without manual intervention. Workflow concurrency sets
`cancel-in-progress: false` so an in-flight upload is never interrupted mid-transfer. Visitors with an already open tab
keep their client-side view state, and reloading revalidates `index.html` (`Cache-Control: no-cache`) to pick up the new
content-hashed assets (`FR-059`).

### Free-tier quotas and HTTP 403 exhaustion (`FR-075`)

`F1` enforces platform quotas of **165 MB outgoing bandwidth per day** and **60 CPU minutes per day** (plus 3 CPU
minutes per 5-minute window), resetting at **00:00 UTC**. Per-visitor request rate limiting (`FR-056`, `FR-078`) bounds
single-client traffic, while distributed traffic spikes can still consume the daily allowance.

When a Free-tier quota is exhausted, Azure stops the web app until the next UTC reset and answers every request at the
platform front end with **HTTP 403** (so the MOBAdemo host itself is not reached). Operators can inspect current daily
bandwidth/CPU consumption and confirm quota exhaustion in the Azure Portal on the `mobaflow-demo` web app under
**Settings -> Quotas** (the web app's quota view).
