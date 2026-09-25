# Solution and presentation contracts

Source: https://github.com/ahuelsmann/MOBAflow/issues/147

Solution schema remains version 4. Vehicle objects no longer expose or emit maintenance.
Remove vehicleMaintenance, vehicleMaintenanceEntry, vehicleMaintenancePlan and maintenanceCategory schema
definitions and references. Existing master-data and decoder/CV contracts remain intact.

MOBApi has no maintenance endpoint. Its generic solution transport remains generic; new data is serialized
from the reduced domain model. Mobile save/load uses the same model. Do not introduce a migration or a
special raw-JSON rewrite path. Unknown properties have no retained representation in the loaded model.

Library/passport contracts retain identity, inventory and decoder details, without maintenance flags,
summaries or state. The HTML passport omits maintenance rows.
