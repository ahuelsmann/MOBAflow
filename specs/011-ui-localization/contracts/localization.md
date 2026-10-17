# UI localization contract

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).

## Shared boundary

IUiLocalizer in `SharedUI/Localization/` exposes startup `CultureInfo Culture`, GetText(key) and
Format(key, arguments). No WinUI/MAUI/transport types cross the boundary.
GetText resolves translation then English fallback for known keys. Unknown/empty English source
is a development failure caught by checks, never a user-visible key. Format uses active display
culture and supports reordered positional arguments; never concatenate sentence fragments or
feed formatted text into protocols/persistence.

## Host startup

Resolve System from OS UI culture before the app override; never use an already overridden
culture for System. Reapply preference each process start. Set CurrentUICulture and
DefaultThreadCurrentUICulture for shared resources; WinUI additionally sets PrimaryLanguageOverride.
Keep CurrentCulture and TTS options unchanged.

## Visible controls

Choices use localized System and native English/Deutsch names plus canonical values.
Explicit Save shows current-language restart/error text. No automatic restart. Controls expose
accessible names and keyboard focus; long prose wraps. WinUI x:Uid maps .Text/.Content/.Header
and attached accessible names explicitly. Navigation tags, product names, versions, URLs and
custom page-label suffixes remain invariant. MAUI markup resolves the same semantic catalogue.

ISettingsService.SaveUiLanguageAsync serializes the candidate preference under the host's existing
settings-file write lock. The shared AppSettings preference changes only after that write succeeds;
queued automatic saves therefore retain the previous preference after a failed language save.
UiLanguagePreferenceChanged notifies a committed preference or successful reset, including a reset
to an unchanged System value. LanguageSettingsViewModel refreshes its choice and status through
IUiDispatcher and releases the subscription on disposal. Reset discards an unsaved picker choice;
the running UI culture remains unchanged.

## Validation

Reject duplicate/missing/empty keys, unknown used keys, malformed composite format,
differing placeholder indexes and stale WinUI projection. Honor escaped braces.
Negative tests use isolated fixtures, never edits to tracked production resources.
