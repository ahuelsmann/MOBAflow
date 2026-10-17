# Localization data model

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).

## UiLanguage preference

Path: `AppSettings.Application.UiLanguage` in existing per-host settings JSON.

- Type: string. Default: `system`.
- Persisted canonical values: `system`, `en-US`, `de-DE`.
- Constraint: null, empty, malformed and unsupported values normalize to `system`.
- Local installation/device scope; never Solution/Project or speech language.
- No schema-version bump, migration or compatibility path.

## Active language

Resolve once at startup from preference and OS UI culture: result en-US/de-DE; German system
family maps to de-DE, other families to en-US. Display culture follows that result for UI templates.
Active language remains independent of mutable saved preference until full restart.

## Text catalogue/projection

- Stable ASCII semantic key, unique ignoring case.
- Nonempty English source and German translation for shipped slice.
- Translator comment describes surface and placeholder meaning.
- Complete templates with matching placeholder-index sets and valid composite format.
- Explicit semantic key -> one or more unique `Uid.Property` mappings.
- Generated .resw is derived data; never an independently editable translation source.

## Language settings state

ViewModel exposes choices, selection, save command and localized message/error state.

1. Startup selects canonical saved preference; localizer keeps startup language.
2. Selection edits UI state only; never Solution/speech values.
3. Save awaits settings service, modifying only UiLanguage.
4. Success shows current-language restart explanation.
5. Failure restores previous in-memory preference and reports localized error with retry.

No automatic restart/shutdown, synchronization or hardware commands.
