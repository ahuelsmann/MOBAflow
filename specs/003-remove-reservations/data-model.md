# Retained model

InterlockingDefinition retains turnouts, signals, blocks, connections and bindings. Remove Routes, RouteDefinition, RouteTurnoutRequirement and RouteSignalRequirement. Keep mappings, stable identities, explicit occupied/clear feedback and representation links.

Immutable runtime snapshots retain revision and turnout/block/signal dictionaries. Remove route dictionary, RouteRuntimeState, RouteLifecycle and lock/reservation owners. Standalone turnout results use a turnout-specific status.

Turnout lifecycle remains Unknown/Requested/Pending/Confirmed/Failed. Block occupancy remains Unknown/Free/Occupied/Fault. Neither represents ownership. Existing direct signal configuration/commands remain. No serializer migration or schema-version increment.
