# Release Studio

Release Studio turns an existing signed MOBAflow version tag into a reviewable draft GitHub Release. It validates the tag, builds and tests the Windows application, packages the self-contained x64 output, creates a checksum, and attaches the artifacts to a draft release.

It does not create tags and it never publishes a release automatically.

## Prerequisites

- The release commit is on the remote repository.
- `CHANGELOG.md` describes the release-worthy changes.
- The plain Semantic Version tag exists without a `v` prefix, for example `0.2.0`.
- The tag is annotated and signed with a key that GitHub recognizes as verified.

Create and push the tag from a trusted maintainer workstation:

```powershell
git switch main
git pull --ff-only
git tag -s 0.2.0 -m "Release 0.2.0"
git push origin 0.2.0
```

## Create a release candidate

1. Open **Actions > Release Studio** in GitHub.
2. Select **Run workflow**.
3. Enter the existing tag.
4. Select whether the draft represents a prerelease.
5. Start the workflow and review every completed step.
6. Download and test the workflow artifact when an additional local check is useful.
7. Open the generated draft under **Releases**.
8. Review the generated notes, attached ZIP, and `SHA256SUMS.txt`.
9. Publish the draft only after the release candidate has passed manual application and hardware checks.

## Release checklist

- [ ] The selected commit is the intended release commit.
- [ ] The signed tag is visible as verified on GitHub.
- [ ] Automated build and tests pass.
- [ ] MOBAflow starts on a supported Windows system.
- [ ] MOBApi starts with MOBAflow and remains restricted to the private network.
- [ ] Z21 connection, emergency stop, locomotive control, and feedback monitoring are manually checked.
- [ ] Existing project files open successfully.
- [ ] Release notes describe user-visible changes and breaking changes.
- [ ] The ZIP checksum matches `SHA256SUMS.txt`.
- [ ] The draft release contains no secrets, logs, local settings, or development files.

## Safe re-runs

Running Release Studio again for the same tag updates the assets of an existing draft. It refuses to modify a published release. Delete an unwanted draft manually only after confirming that no one is reviewing it.

## Relationship to Azure DevOps

The existing Azure DevOps release pipeline remains available while Release Studio is evaluated. Do not remove it until at least one GitHub-native release has been built, manually verified, and published successfully.

## Troubleshooting

### The tag is lightweight

Delete the incorrect remote tag only when it has not been used for a release, then recreate it as a signed annotated tag from a trusted workstation.

### GitHub cannot verify the signature

Confirm that the signing key is associated with the maintainer's GitHub account and that the tag displays as verified in the repository. Release Studio intentionally rejects unknown or unverifiable signatures.

### A published release already exists

Published releases are immutable to this workflow. Create a new patch version when release contents need to change.
