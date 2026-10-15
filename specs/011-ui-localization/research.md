# Localization research

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140), 2026-10-14.

## Platform resources

**Decision**: Shared English/German `.resx` catalogues with an explicit deterministic WinUI
`.resw` property projection. Shared ViewModels use IUiLocalizer; MAUI uses a host markup adapter.
**Rationale**: x:Uid requires property keys such as `InfoSubtitle.Text`; shared logic uses semantic
keys. Single translation ownership avoids drift. The generator is our design inference.
**Alternatives considered**: Duplicate catalogues (drift), WinUI types in SharedUI (layer violation),
dynamic bindings for all WinUI text (ignores issue's x:Uid approach).

Sources: [WinUI MRTCore localization](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/localize-strings),
[MAUI localization](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/localization?view=net-maui-10.0).

## Startup and restart

**Decision**: Apply Windows App SDK PrimaryLanguageOverride before covered WinUI views/resource
lookup; unpackaged hosts reapply preference each process start. Android sets resource UI culture
before App/Shell/views. Existing per-host settings own persistence. Saving announces full restart.
**Rationale**: Override affects ResourceLoader/x:Uid; unpackaged persistence is app-owned.
System resolves de family to de-DE, other languages to en-US.
**Alternatives considered**: Instant switching (scope/race cost), relying on MRTCore persistence
(unpackaged gap), syncing with MOBApi (unnecessary device coupling).

Sources: [PrimaryLanguageOverride](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.globalization.applicationlanguages.primarylanguageoverride?view=windows-app-sdk-1.8),
[DefaultThreadCurrentUICulture](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.defaultthreadcurrentuiculture?view=net-10.0).

## Fallback and formatting

**Decision**: Neutral language en-US; known missing German entries fall back to English.
Unknown keys fail development validation. Keep CurrentCulture unchanged; format covered UI
templates explicitly using localizer display culture.
**Rationale**: CurrentUICulture selects text; CurrentCulture may affect unrelated parsing/TTS.
Tests prefer explicit instance cultures; global culture mutation requires test isolation.
**Alternatives considered**: Visible keys (bad UX), translating user data (violates #140),
global CurrentCulture switch (unbounded behavior effects).

Source: [ResourceManager.GetString](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-resources-resourcemanager-getstring).

## Repository evidence/risks

ApplicationSettings and both ISettingsService implementations already own app-local preferences;
they copy loaded fields explicitly, so UiLanguage needs both roundtrips covered. Desktop settings
load synchronously; mobile settings currently load later. Preference must precede covered views
without blocking async work. NavigationItemFactory centralizes desktop labels; MainPage.xaml
is CounterPage and hosts mobile settings. Use a new control, not a Counter extraction.

Build output must contain the host PRI (`MOBAflow.pri`) and shared satellite assemblies. Microsoft's MAUI
guide warns culture behavior varies by platform; unit checks do not replace authorized Android
runtime/long-text acceptance. No hardware or paid provider is needed.
