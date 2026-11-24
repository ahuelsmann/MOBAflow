# Feature Specification: Validate Settings inputs

**Feature Directory**: `021-settings-input-validation`
**GitHub Issue**: #470
**Spec Kit**: Required

## Governance and Traceability

The [maintainer decision and Triad resolution](https://github.com/ahuelsmann/MOBAflow/issues/470#issuecomment-7105502485) governs the feature. The [Triad assessment](https://github.com/ahuelsmann/MOBAflow/issues/470#issuecomment-7094711767) defines the three-slice order and the UI pattern.

## User scenarios

### Edit a setting without losing the last valid value

An operator enters `Dddd` in Display IP Address or `X` in Photo storage folder. On leaving the field, the draft stays visible and the English inline error explains what to correct. The app continues using the last valid value and does not save the rejected draft. On entering a valid value, the error clears and normal auto-save applies the change. A subsequent restart uses the accepted value.

### Clear a number or disable polling

An operator temporarily clears a number while editing it. An empty, non-finite, or otherwise invalid number never becomes zero or reaches a numeric cast. The last valid value and its effects are preserved. For Z21 system-state polling, the operator can enter zero explicitly to disable polling or choose a value from 1 to 30 seconds to enable it.

### Choose a path or reset Settings

A successful Browse selection replaces the draft with the chosen path and clears its error after validation. Cancel leaves the current edit as it was. Reset to defaults clears drafts and errors and resynchronizes the controls with the defaults. Empty Display IP Address means unconfigured. Empty Photo storage folder means use the existing default location.

## Requirements

- **FR-001**: Separate editable draft state from the effective settings. A rejected edit must leave the in-memory settings, persisted values and change-driven effects unchanged. Accepting a changed valid value uses the existing auto-save path. Reaccepting the same value clears any error without writing again.
- **FR-002**: Keep text-field validation on focus loss. Show a concise English error below each invalid text or numeric control, separate from the page InfoBar. Use the Event Manager pattern: `CaptionTextBlockStyle`, `SystemFillColorCriticalBrush`, `TextWrapping="Wrap"`, `AutomationProperties.LiveSetting="Polite"` and error-bound `Visibility` that collapses the message when valid. Give the field and its Browse action a distinct accessible name and useful help text where needed. Place each Piper path error beneath the entire TextBox/Browse row so the button remains aligned. Keep focus and the draft in the same control.
- **FR-003**: Allow an empty Display IP Address. Otherwise accept an IPv4 or IPv6 literal only, using the existing `DisplayEndpoint.TryCreate` contract. Reject unspecified addresses. Validate Display UDP Port as a whole number from 1 to 65535 even when the address is blank. Validation makes no network call. Reject invalid field candidates without losing the other field's accepted value.
- **FR-004**: Validate REST API Port as a whole number from 1024 to 65535, matching the current Settings control. Editing the port does not probe availability or start, stop or bind a server.
- **FR-005**: Allow an empty Photo storage folder. Any nonempty candidate must be fully qualified and point to an existing directory. An unavailable directory candidate remains a draft. Validation does not create directories or enumerate their contents.
- **FR-006**: At the editable input boundary, reject cleared values, NaN and infinities for every Settings NumberBox before conversion, clamping, mutation, save or notifications that apply the setting. Reject values that would overflow a destination type before converting.
- **FR-007**: Align the Z21 system-state polling control with its contract. Accept only 0 (disabled) or whole seconds from 1 to 30. Validate the existing retry, health-check and feedback-point rules without changing the runtime behavior of accepted values.
- **FR-008**: Validate the speech and Piper fields against the existing numeric, path and choice contracts. Preserve documented empty/default behavior, language-dependent voice selection, optional config and diagnostic flags. An empty System Speech default voice delegates to the platform default. Preserve the provider's fallback if a named voice is unavailable. Validate blank or invalid test text on the explicit test action and preserve the existing speech failure behavior. Changing a Piper voice revalidates and resynchronizes the derived path drafts and their errors. Do not start speech or a subprocess on validation.
- **FR-009**: Inventory all editable Settings controls. Controls that restrict input to a known valid choice keep that restriction and existing effects. Cover Browse and Reset to defaults in every applicable slice. Preserve the persisted schema and existing values on load. Preserve #471 live Feature Toggles, including permanently available Overview, disabled-page fallback, and the existing Reset-to-defaults refresh. Preserve `IsDarkMode` and its MainWindow theme-change wiring, including synchronization on reset.

Use actionable English wording for the relevant field, for example `Enter a valid IPv4 or IPv6 address.`, `Enter a whole number from 1 to 65535.`, `Enter a fully qualified folder path.`, or `Folder does not exist.`. A blank field with a documented default remains valid.

## Acceptance criteria

- **AC-01**: Rejected text and numeric edits produce field-local feedback and no setting change, save call, or new applied-setting event. A valid correction updates the effective value and persists it exactly once if it changed.
- **AC-02**: Tests cover IPv4, IPv6, blank, hostname, invalid and unspecified display addresses; accepted and rejected ports; and blank, relative, missing and existing photo directories.
- **AC-03**: Tests account for every NumberBox, including a cleared control, NaN, both infinities and integer overflow where a cast is used. An invalid feedback-point edit leaves counters intact.
- **AC-04**: Polling tests cover 0, 1 and 30 seconds and reject negative, fractional and out-of-range values. Reset restores the normal defaults and does not revive stale drafts.
- **AC-05**: Windows practical acceptance covers text and numeric errors, recovery, Browse, Reset, keyboard navigation and Narrator announcements. Verify that Feature Toggles refresh navigation and Overview fallback as before and the theme follows the effective setting across reset. Record this separately from static checks and CI.

## Out of scope

New settings schema or migration, hostnames and network probes, transport/runtime changes, live hardware actions, a new save workflow, and a redesign of Settings. The spec-only PR contains planning artifacts. Implementation follows in three sequential code PRs.
