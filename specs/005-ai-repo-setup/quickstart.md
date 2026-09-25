# Validation quickstart
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145

From the task worktree with Git, PowerShell 7 and Python 3.11+:
```powershell
./scripts/Test-InstructionConsistency.ps1
python scripts/Test-AiRepositorySetup.py
python scripts/Test-AiRepositorySetup.Tests.py
./scripts/Test-SpecKitGovernance.Tests.ps1
./scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -BaseRef github/main
./scripts/Test-LineEndings.ps1
uv tool run --from git+https://github.com/github/spec-kit.git@v1.0.11 specify integration status
```
Execute added commands after implementation. Fixtures use temporary repos, distinct roots,
invalid configuration/metadata, broken links and ambiguous remotes. Invalid fixtures fail;
the real setup and supported remote forms pass. No live issues are created.
A fresh authenticated agent should discover the two project skills and choose scoped checks
for documentation, review and simulated diagnosis. Record actual evidence or access limits.
