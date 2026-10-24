# Research: MOBAsmart fixed STOP

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304

## Bounded layout

Decision: Auto/* Grid; only identity/address/STOP fixed; all existing speed/direction/status and function content stays in one bounded ScrollView.
Rationale: ControlPage currently scrolls everything; AppTabHostPage already bounds content in a star row. Fixing the whole speed-gauge card would consume enlarged-text space. Nested scrolling or duplicate STOP surfaces are unnecessary.
Sources: [MAUI ScrollView](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/scrollview?view=net-maui-10.0), [MAUI Grid](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/layouts/grid?view=net-maui-10.0).

## Scaling and command scope

Decision: bounded wrapping name/address, separate selected-loco scope, existing STOP with auto height/minimum 44. Keep font autoscaling enabled. Do not transfer browser pixel dimensions.
Rationale: DangerButton inherits PrimaryButton HeightRequest=48 and wide padding; override locally rather than changing global styles. StatusMessage is transient and cannot replace a permanent DCC field.
Sources: [MAUI Button](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/button?view=net-maui-10.0), [HeightRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.controls.visualelement.heightrequest?view=net-maui-10.0).

## Preserved state and accessible functions

Decision: preserve EmergencyStopCommand and IRuntimeCommandGateway/MobileRuntimeCoordinator. Refresh existing function semantics for description and on/off changes, not just F# label changes; wrap visual meaning.
Rationale: existing function descriptions can otherwise remain stale. No new resource system; English base conventions remain until #140 merges.
Source: [MAUI accessibility](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/accessibility?view=net-maui-10.0).

Baseline limitation: at nonzero speed, setting zero can schedule a debounced drive in addition to the direct STOP send. #304 requires one activation of the existing command, not exactly-once transport. Tests must not conceal this with zero-speed setup or claim total transport counts from an early observation.

All technical unknowns are resolved by current code and official documentation, including one read-only research agent required by speckit-plan. Android layout/touch/accessibility acceptance remains a device gate.
