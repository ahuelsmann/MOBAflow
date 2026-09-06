---
description: 'Platform-neutral backend I/O, runtime ownership and event-driven state.'
applyTo: 'Backend/**/*.cs'
---

# Backend boundaries

Use [AGENTS.md](../../AGENTS.md) and [the architecture source map](architecture.instructions.md).
`Backend/Backend.csproj` targets `net10.0`; keep WinUI/MAUI references, dispatchers and platform-specific
compilation branches out of this layer.

## I/O and notifications

- Reuse the existing I/O interfaces (`IUdpClientWrapper`, `IFileSystem`, etc.) through constructor injection.
  Keep production I/O adapters and test doubles at the existing boundaries; do not introduce a second transport.
- Await asynchronous I/O and preserve cancellation, logging and resource disposal. Use `ConfigureAwait(false)`
  for library continuations that do not depend on a caller context.
- Publish through the established EventBus or existing domain events. Backend callbacks may run in the background;
  UI marshalling belongs to the UI host's decorator/adapters. Do not introduce a backend dependency on a dispatcher.
- Preserve the Z21 callback's responsiveness and existing ordering/concurrency guarantees when changing handlers.

## Runtime state

- Z21 feedback is the source of observed hardware state. Keep runtime state transitions in the owning service;
  ViewModels project the results and send commands through the runtime/gateway.
- Do not reset telemetry independently in a UI command after sending track-power changes: a delayed hardware
  response can overwrite that reset. Apply any required display filtering consistently when projecting state.
- Distinguish editable UI state from observed hardware state. Preserve the runtime's existing pending-command
  reconciliation, fail-safe behavior and operator acknowledgement; avoid blanket changes to optimistic state.
- Extend existing workflow action handlers and execution context construction. Preserve failure propagation,
  action ordering and the difference between sequential and parallel execution.

Use the relevant runtime/workflow tests and fake I/O under `Test/` for validation. For packet or connection
changes, also read [Z21 guidance](z21-backend.instructions.md).