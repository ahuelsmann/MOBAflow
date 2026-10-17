# Presentation contract: Train consist card

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

## Entry and reading

From the existing Trains view, select an existing train and activate **Train card**. Bind the separate card ViewModel's Open command to that selection. Without a selection the action is disabled; revalidate project membership on invocation.

The read-only dialog shows train name, project, full available identity, "Planned consist · Edited Solution data", one creation time with zone offset, and a statement that physical/runtime/decoder state is not certified. Each ordered row emphasizes position and fully wrapped name; type, full reference identity, configured locomotive DCC/unconfigured text and diagnostics follow. Repeated vehicle occurrences remain separate rows; no extra repeat badge is required.

Invalid/missing/ambiguous rows keep their number and raw available identity with a textual explanation at that row. Source identity problems are also shown in the header. An empty consist is explicitly empty; unavailable data is a different state.

Controls: **Print** and **Close**. All have accessible names and keyboard access. Focus enters the reading surface; Tab/Shift+Tab reaches actions without getting trapped, cancelling or ending print preparation keeps the card open and returns focus to Print, and Close returns to the invoking control if present, otherwise the train list. Screen styling uses ThemeResource. Names and diagnostics wrap; enlarged text increases layout height and scrolling instead of being clipped or ellipsized. No edit/remove/reorder controls are present.

## Print preparation

Print requests the current snapshot through the host adapter. The native UI is window-bound and requests A5 portrait. The user chooses a supported printer/media setting. Unsupported A5 is disclosed, not claimed to have printed on A5. The native UI's output is not a new observation.

Pages use paper-appropriate black text on white, at least 11-point body text, and larger text when requested. Each page identifies train/project, scope and original timestamp; page x of y is correct after pagination. Occurrence numbering continues across pages. Normal positions remain together where possible. Overheight positions use readable text fragments labeled with the same position and a continuation indication. Very long headers/footers and unusable paper areas must fail honestly if they cannot leave a valid content area; no blank-page loop.

Screen, native preview and final submitted page collection must agree on every copied identity, timestamp, occurrence and diagnostic. Changes to printer options can change only page layout/count. Cancellation and failure leave the same screen card available; Print can be explicitly retried after completion. No parallel jobs for one card.

## Read-only boundary

No model mutation, Solution persistence, runtime creation/update, owner query, operator command or railroad interaction is permitted from this flow. User-initiated output goes only to the selected Windows print service. No external browser, new HTML/PDF export action, card-history store or global Messages filter update is introduced.

## Acceptance provenance

Planned contract, not tested behavior. Record separately: pure synthetic tests, real Windows UI interaction, generated A5 digital pages, and a physical A5 print. Print preview or a successful spooler callback is never a physical-paper pass.
