---
description: Template for adding a new service with proper DI registration across all platforms
---

# Add New Service with DI

I need to add a new service to MOBAflow. Please help me:

1. **Create the service interface** in `Backend/Interface/` or `SharedUI/Service/`
2. **Implement the service** in the appropriate project
3. **Register in DI** for all platforms:
   - WinUI: `WinUI/App.xaml.cs`
   - MAUI: `MAUI/MauiProgram.cs`
   - WebApp: `WebApp/Program.cs`
4. **Add to DI tests**:
   - `Test/WinUI/WinUiDiTests.cs`
   - `Test/WebApp/WebAppDiTests.cs`

## Service Details

Service Name: [YOUR_SERVICE_NAME]
Service Type: 
- [ ] Platform-independent (Backend/SharedUI)
- [ ] Platform-specific (WinUI/MAUI/WebApp)

Lifetime:
- [ ] Singleton (application-wide state)
- [ ] Scoped (per-request, Blazor only)
- [ ] Transient (stateless)

Dependencies: [LIST_DEPENDENCIES]

## Requirements

- ✅ Follow namespace conventions
- ✅ Backend stays platform-independent
- ✅ Use constructor injection
- ✅ Add XML documentation
- ✅ Update DI tests

#file:docs/DI-INSTRUCTIONS.md
