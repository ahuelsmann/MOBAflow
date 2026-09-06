---
description: 'Mandatory Git worktree isolation for independent tasks that modify repository files or Git state.'
applyTo: '**'
---

# Git Worktree Isolation

Use one physical Git worktree and one task branch for each independent repository write task. A conversation, agent, or branch name alone does not isolate `HEAD`, the index, or uncommitted files.

## Mandatory rule

Before the first repository file write or Git mutation:

1. Identify one primary GitHub issue or explicitly named task.
2. Run `git worktree list --porcelain` and `git status --short --branch`.
3. Confirm that the current directory is the task's dedicated worktree.
4. Confirm that the branch is task-specific and starts with `codex/` unless the user requests another convention.
5. Confirm the intended base branch and that no unrelated or unowned changes are present.
6. Run every subsequent file, build, test, and Git command from that worktree.

Stop before writing when any check fails. Create or request a dedicated worktree instead of switching a shared checkout.

## Bootstrap exception

Use the shared or default checkout only for the read-only checks and the `git worktree add -b` command required to create the dedicated worktree. Do not make task file changes there. End this exception as soon as the dedicated worktree exists.

When the Codex app or another approved task workflow provisions a worktree, use that mechanism and verify the resulting path, branch, and base before writing.

## Scope

Require a dedicated worktree for tasks that create, edit, move, or delete repository files or that mutate branches, the index, commits, or pull-request source changes.

Do not require a dedicated worktree for:

- read-only repository analysis and Git inspection;
- GitHub-only issue, pull-request, label, comment, or project metadata changes;
- agents explicitly collaborating on the same primary task when file ownership and sequencing are clear.

Never combine independent primary issues in one worktree.

## Existing changes

When a checkout contains unrelated or unowned changes:

- do not switch branches, stash, reset, clean, stage, move, or delete those changes;
- create the task worktree from the agreed base;
- transfer only changes that clearly belong to the task through an explicit, reviewable patch;
- stop and coordinate when file or hunk ownership is unclear.

## Creating a task worktree

Confirm that the target path and branch do not already exist, then create the worktree from the intended base without switching the shared checkout:

```powershell
git worktree add 'C:\Repo\ahuelsmann\MOBAflow-issue-116' `
  -b codex/issue-116-worktree-isolation github/main
```

Verify the result with `git worktree list --porcelain`. Use a stable path that identifies the task.

## Cleanup

Inspect `git status --short --branch` and resolve the exact absolute target path before removal.

- Remove a clean worktree after its branch is integrated or no longer needed.
- Discard a dirty worktree only after the user explicitly authorizes losing those exact changes.
- Never remove a shared checkout or a worktree containing unknown or unowned changes.
- Remove obsolete local branches only after confirming their worktrees are gone and their commits are integrated or intentionally discarded.

## Pre-write checklist

- [ ] Primary issue or task identified
- [ ] Dedicated worktree path confirmed
- [ ] Task branch and base confirmed
- [ ] No unrelated or unowned changes present
- [ ] Every command uses the dedicated worktree
