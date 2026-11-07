# Data model: unchanged

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304

- LocomotiveTitle/LocoAddress remain existing projections; no duplicate selection state.
- EmergencyStopCommand retains existing availability and asynchronous selected-target behavior.
- The same 32 FunctionButtonViewModel items retain Index/Label/Description/IsOn; no stored field or transition added.
- Function semantic text reflects the same label, meaning and On/Off state as the visual.
- No serialization/schema/configuration/API/protocol/migration/DI/startup changes.
