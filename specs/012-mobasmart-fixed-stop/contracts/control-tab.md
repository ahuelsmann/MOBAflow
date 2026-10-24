# Control tab contract

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304

| Surface | Invariant |
| --- | --- |
| Fixed identity/address | LocomotiveTitle and LocoAddress, bounded wrapping width |
| Fixed STOP | One existing EmergencyStopCommand binding, visible selected-loco scope |
| Availability | Existing CanExecuteLocoCommand, no fallback command |
| Scrolling controls | Existing speed/direction/status/All off and F0-F31 |
| Functions | Functions and ControlRoot.ToggleFunctionCommand forwarding retained |
| Accessible meaning | Full F# plus description and On/Off, refreshed with changes |
| Routing | Required IRuntimeCommandGateway and existing MobileRuntimeCoordinator |

No new API. One command activation is not a new guarantee of one network telegram; existing speed debounce remains. No browser simulation UI enters the product.
