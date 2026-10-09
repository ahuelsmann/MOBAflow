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

## Fresh-client probe checklist (T019)

Run once per supported client: Codex CLI (maintainer) and Claude Code (agent). Use your own normal login;
never copy credentials, tokens or personal configuration into the clone, the worktree or the result.

1. **Fresh clone and worktree**: clone the repository into an empty folder, then create a task worktree:
   `git worktree add ../mobaflow-probe -b probe/<client>-<date> <remote>/main`. Start the client in the
   worktree root and trust the project when asked.
2. **Remote resolution**: run `./scripts/Resolve-GitHubRepository.ps1` once with the remote named `origin`
   and once after `git remote rename origin github`; both must report `ahuelsmann/MOBAflow` and the worktree
   root. Rename the remote back afterwards.
3. **Instruction loading**: ask "Which rules apply before you push or start MOBAflow?". The answer must cite
   AGENTS.md (task branch and pull request, no app launch without approval, draft PR until Sonar is green).
4. **Skill discovery**: ask the client to list the project skills. Codex must show `mobaflow-code-review`,
   `mobaflow-diagnosing-bugs`, `audit-code-quality` and the Spec Kit skills; Claude Code must show the two
   MOBAflow skills.
5. **Documentation task**: ask for a one-line wording fix in `docs/AI-DEVELOPMENT.md`. The client must choose
   the documentation checks from AGENTS.md (no .NET build or tests) and must not launch the app.
6. **Review skill**: run the review skill against the last merged PR. It must stay read-only and report
   findings with file and line, or say that the change is clean.
7. **Diagnosis skill**: run the diagnosis skill on a simulated symptom ("rows from the previous project
   reappear after switching projects"). It must plan a bounded reproduction and a targeted regression test
   without launching the app or touching hardware.
8. **Worktree confinement**: check that every file the client read or changed is inside the probe worktree.
9. **Prompt secrets hook**: confirm in the client that the project hook ran on a prompt. Without Sonar CLI the
   client shows "Secrets scan unavailable"; with Sonar CLI run `sonar hook --help` first and check that the
   `codex-prompt-submit` or `claude-prompt-submit` subcommand exists, then send a harmless prompt and confirm
   that it passes.
10. **Clean up**: discard the probe changes, remove the worktree and delete the probe branch.

Record per client in `validation.md`: date, client and version, operating system, commit, the result of each
step (passed, failed or not possible with the reason) and any deviation. Send the Codex CLI result to the agent
or add it to `validation.md` directly.
