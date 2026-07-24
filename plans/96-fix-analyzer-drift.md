# Issue 96 - Analyzer baseline drift repair

## Goal

Restore the exact cross-platform, Windows, and Android analyzer ratchets after
the startup save-dialog fix without increasing or suppressing any baseline.

## Scope

- Modernize the newly added `HasUnsavedChanges` observable property so it does
  not add another MVVM Toolkit diagnostic.
- Fix only analyzer findings introduced by the new startup/autosave tests.
- Remove baseline entries only when the corresponding diagnostic no longer
  exists.
- Keep RF-13 and all feature work outside this repair.

## Validation

1. Run the focused startup/autosave tests.
2. Run the exact cross-platform analyzer ratchet.
3. Run the exact Windows and Android analyzer ratchets.
4. Run the Windows test and coverage gates affected by the change.
5. Attempt local Sonar analysis against `github/main`, then require a green
   remote SonarCloud check with zero open or confirmed PR issues.

## Completion

Delete this plan after the validated implementation is committed. The closed
issue, merged pull request, and Git history remain the durable record.
