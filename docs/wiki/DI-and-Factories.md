# DI and Factories

- IZ21 and IJourneyManagerFactory are registered as singletons in platform hosts.
- Journey view models are created via IJourneyViewModelFactory to keep adapters per platform.
- UI should not instantiate services via `new`; resolve through DI.

Examples

- WinUI: register IoService, IZ21, IJourneyManagerFactory, IJourneyViewModelFactory; set TreeViewBuilder.JourneyVmFactory at startup.
- MAUI: register CounterViewModel and MAUI JourneyViewModel; resolve pages via DI.
- WebApp: register IZ21, IJourneyManagerFactory, WebJourneyViewModelFactory.
