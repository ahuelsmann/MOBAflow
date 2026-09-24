# Direct operation and display contract

IInterlockingRuntime exposes activation, synchronization, immutable current state, standalone turnout commands, observation draining and disposal. Every route command disappears.

Direct commands target one mapped turnout, supported position and nonempty correlation. Existing services reject invalid/busy/duplicate commands and disconnected transport sends nothing. Block occupancy cannot inhibit direct switching. Disconnect invalidates observations without automatic hardware effects.

The selected-object workbench offers mapped turnout positions and block/signal information. Route draft, authoring, preview, selection, setting, cancellation, reconciliation, safe-stop and release disappear. Existing IMobaRuntime direct commands, local/remote routing, InPort counts and journey rules remain unchanged.
