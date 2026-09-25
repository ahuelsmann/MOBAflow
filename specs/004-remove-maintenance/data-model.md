# Data model

Source: https://github.com/ahuelsmann/MOBAflow/issues/147

- Locomotive and Wagon (PassengerWagon/GoodsWagon): remove Maintenance; all master-data fields remain.
- Delete VehicleMaintenanceData, VehicleMaintenanceEntry, VehicleMaintenancePlan, MaintenanceCategory
  and maintenance-only MoneyAmount.
- Keep LocomotiveDecoderProfile, DecoderCvSnapshot/Value, DecoderProtocol and TrainType.Maintenance.
- Library entries lose HasMaintenanceHistory; passports lose LatestMaintenance and MaintenanceState.
  LocomotiveMaintenanceSummary and maintenance status records disappear.
- No new entities, state transitions, persistence stores or configuration are introduced.
