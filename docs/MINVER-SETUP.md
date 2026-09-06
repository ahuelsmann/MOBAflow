# MinVer setup

MOBAflow derives assembly/package versions from signed Semantic Version tags by
using MinVer across all .NET projects.

## Repository policy

- Tags use plain `MAJOR.MINOR.PATCH` without a `v` prefix, for example `0.2.0`.
- Release tags are annotated and signed by a listed maintainer.
- `Directory.Build.props` references MinVer and sets the repository defaults.
- `Directory.Packages.props` owns the MinVer package version.
- `version.json` supplies additional MinVer defaults.

The nearest valid tag determines a release build version. Commits after a tag
receive an automatically calculated prerelease version and commit metadata.
Always inspect the actual build output or `minver` result instead of copying an
example version into release notes.

## Create a release tag

From a trusted maintainer workstation:

```powershell
git switch main
git pull --ff-only
git tag -s 0.2.1 -m "Release 0.2.1"
git push origin 0.2.1
```

Verify the signature before using the tag:

```powershell
git fetch origin --tags
git tag -v 0.2.1
```

Do not rewrite a tag that has already been used for a release. Create a new
patch version instead.

## Check the calculated version

```powershell
dotnet tool restore
dotnet minver
dotnet build MOBAflow/MOBAflow.csproj -c Release
```

If MinVer cannot see the expected version, fetch full tag history and retry:

```powershell
git fetch --tags
git describe --tags --always
dotnet minver
```

## Release automation

- `.github/workflows/release-studio.yml` validates an existing signed tag,
  builds/tests the Windows app and creates or updates a draft GitHub Release.
- `.azure-pipelines/release.yml` remains an additional maintainer-controlled
  release path.
- Neither path should silently invent or rewrite a release tag.

See [Release Studio](RELEASE-STUDIO.md) for the artifact and manual verification
checklist.

## Common mistakes

- `v0.2.1` does not match the configured empty tag prefix.
- `0.2` is not a complete Semantic Version.
- A lightweight tag is not sufficient for the signed-tag release workflow.
- A shallow clone without tags can calculate an unexpected fallback version.
- `version.json` and `Directory.Build.props` are both active inputs; keep their
  prerelease policy intentional when changing either file.

Further reading: [MinVer](https://github.com/adamralph/minver) and
[Semantic Versioning](https://semver.org/).
