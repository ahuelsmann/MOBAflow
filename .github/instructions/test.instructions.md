---
description: 'Behavior-focused NUnit tests and platform-aware validation.'
applyTo: 'Test/**/*.cs,Test/Test.csproj'
---

# Testing

Use the validation policy and commands in [AGENTS.md](../../AGENTS.md).
[Test.csproj](../../Test/Test.csproj) defines portable and Windows targets; Android participation is opt-in.

## Test design

- Use NUnit and Moq as already referenced by the project. Follow nearby fixture naming and Arrange-Act-Assert.
- Cover observable behavior and failure modes. For a bug fix, add a regression that would fail before the fix
  when the behavior is testable; reuse existing coverage rather than duplicating it.
- Prefer `Assert.That` and related assertions around one behavior. Multiple related assertions are fine.
- Async tests return `Task` and await operations. Coordinate asynchronous events with explicit completion and
  bounded timeouts instead of arbitrary sleeps. Check event unsubscription/disposal when changing lifetimes.
- Keep tests isolated. Use owned temporary directories and clean them up for filesystem integration tests;
  use existing I/O interfaces and doubles for unit tests. Do not read or overwrite user project/settings files.
- Use existing setup helpers and real registration extensions for DI tests, so tests cover production registrations.
  Do not reconstruct a second container configuration that can pass while production is broken.
- For UI behavior that requires platform types, extract only the necessary shared logic into an appropriate
  platform-neutral type. Verify platform glue by compile/UI checks; do not create an abstraction solely to mirror a test.

## Z21 and runtime fixtures

- [FakeUdpClientWrapper](../../Test/Mocks/FakeUdpClientWrapper.cs) exposes `SentPayloads` and `RaiseReceived(...)`.
  Reuse it instead of a real command station or an invented UDP simulator process.
- [Z21Packets](../../Test/TestData/Z21Packets.cs) provides packet fixtures.
- [Z21CommandTests](../../Test/Backend/Z21CommandTests.cs) demonstrates length/header/checksum assertions.
- [WorkflowServiceTests](../../Test/Backend/WorkflowServiceTests.cs) and
  [WorkflowExecutionEndToEndTests](../../Test/Integration/WorkflowExecutionEndToEndTests.cs) cover workflow execution.
- [MobileRuntimeCoordinatorTests](../../Test/SharedUI/MobileRuntimeCoordinatorTests.cs) covers local/remote routing.

## Running and interpreting checks

- Inspect actual fixture names/categories before filtering. Confirm that the intended tests ran.
- Use the portable target for shared changes and the Windows target for desktop integration.
  `Test/Analysis/` contains separate analysis helpers, not the normal test entry point.
- Respect explicitly marked integration/hardware tests and their prerequisites. Automated railroad tests use fakes.
- Report failed/skipped platform tests precisely. `Test/Unit/SystemSpeechEngineTest.cs` depends on Windows SAPI/audio;
  an unavailable device does not justify dismissing unrelated failures or deleting assertions.
- Coverage is available through `Test/coverlet.runsettings` and `Test/dotnet-coverage.runsettings`.
  Follow the applicable quality pipeline when coverage is needed; do not install global tools or impose a new
  percentage threshold for an ordinary fix.