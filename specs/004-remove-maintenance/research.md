# Research decisions

Source: https://github.com/ahuelsmann/MOBAflow/issues/147

- Decision: remove maintenance completely. Rationale: explicit MVP decision in #144/#147.
  Rejected alternative: hiding the UI leaves services and stored data behind.
- Decision: reuse existing filtered vehicle libraries. Rationale: the removed maintenance projection currently
  owns the lists, but MainWindowViewModel already exposes name-based search. Verify collection/name notifications.
  Rejected alternative: a replacement fleet service would add unnecessary ownership.
- Decision: retain decoder/passport features and work-train category. Rationale: separate inventory concerns.
- Decision: preserve schema 4 and normal JSON loading. Rationale: no retained maintenance members and no migration.
- Decision: separate PR after reviewed #148 base, integrate main before publication and #146 before merge.
  Rationale: shared schema and registration files require coordinated integration.

No research unknowns or new technology choices remain.
