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
3. deletes the output folder and fills it with the result, so no file of an older build stays behind. A failed
   build leaves the old output untouched.

The default output is `MOBAdemo/Host/wwwroot-build/`, which Git ignores. `-OutputPath` chooses another folder
(tests and CI use it); inside the repository only the default folder may be replaced. The committed placeholder
`Host/wwwroot/index.html` is never changed.

## Run or publish the host

The host project uses `wwwroot-build` as its web root whenever that folder contains an `index.html`; otherwise it
uses the placeholder `wwwroot`. A local run and a publish behave the same:

```text
pwsh MOBAdemo/build.ps1
dotnet run --project MOBAdemo/Host/MOBAdemo.csproj
dotnet publish MOBAdemo/Host/MOBAdemo.csproj -c Release -o <folder>
```

Delete `MOBAdemo/Host/wwwroot-build/` to go back to the placeholder.

## Tests

`dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~MOBAdemo"` runs the
host tests and the build tests in `Test/MOBAdemo/Build`, which run the script once into a temporary folder. They are
ignored with a clear reason when `pwsh`, `node` or `npm` is missing. Frontend unit tests: `npm test` in `MOBAdemo/app`.
