# Testing & Simulation

## Goals
- Make business logic testable without any UI framework
- Provide deterministic simulation for hardware (Z21)
- Cover unit, integration, and end-to-end scenarios

---

## Unit Tests
- Test only `Backend` and `SharedUI` logic in unit tests (no UI).
- Use dependency injection to provide mocks for external dependencies.
- Mock `Backend.Z21` for feedback simulation or use `Z21.SimulateFeedback(inPort)` if available.
- Prefer xUnit or NUnit. Keep tests small and isolated.

### Example (pseudo)
```csharp
// Arrange
var z21Mock = new Mock<Backend.Z21>();
var journeys = new List<Journey> { ... };
var manager = new JourneyManager(z21Mock.Object, journeys);

// Act
z21Mock.Raise(z => z.Received += null, new FeedbackResult { InPort = 1 });

// Assert
Assert.Equal(1, journeys[0].CurrentCounter);
```

---

## Integration Tests
- Use real `Backend` code but mock external network (inject a test `UdpClient` wrapper or use `SimulateFeedback`).
- Run on CI with a headless environment.
- Use `TestServer` for WebApp integration tests (Blazor).

---

## Simulation & Manual Testing
- Use `Z21.SimulateFeedback(inPort)` to reproduce feedback without hardware.
- Provide a developer-only debug panel in WinUI/MAUI to send simulated events.
- Use Wireshark or `tcpdump` for low-level debugging.

---

## CI Recommendations
- Run unit tests in PRs and merge only on green.
- Optionally run integration tests in nightly pipeline (requires more infra).
- Do not store secrets in pipelines; use CI secret storage.

---

## Test Data and Fixtures
- Provide small JSON fixtures in `Test/Fixtures/` for reproducible tests.
- Reset static/global state between tests.

---

## Useful Tips
- Keep tests fast (<100ms ideally per unit test)
- Use `IAsyncEnumerable` and cancellation tokens for streaming tests
- Capture logs and save failing case inputs to `artifacts/` for debugging
