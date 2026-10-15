# UI localization

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).
The first slice covers only the surfaces in [the coverage table](../specs/011-ui-localization/coverage.md).
Other page bodies and existing Settings sections retain English until later slices.

## Choose a language

Desktop Settings and the mobile Counter page have a separate UI-language area. Choose
System language, English or Deutsch, then Save language. A full app restart applies it;
the running interface stays in its startup language. Each device stores its own choice.
System selects German for German OS UI languages and English for all others.

The preference is `Application.UiLanguage` (`system`, `en-US`, `de-DE`) in the existing
application settings file. Missing/invalid preferences use System. It is independent of
solution files, undo history, speech provider/language/voice and user-authored text.
The UI does not restart itself or send a runtime/hardware command when language changes.

## Add or change text

1. Add a stable ASCII semantic key to `SharedUI/Localization/UiStrings.resx` and its German
   value to `UiStrings.de-DE.resx`. English is the complete source/fallback catalogue.
   Supply a translator comment describing the surface and each argument's meaning.
2. Translate complete sentences. Keep positional placeholder indexes identical, allowing
   reordered arguments. Escape literal braces as `{{`/`}}`. Use `IUiLocalizer.Format` so UI
   numbers/dates follow the active display culture; keep technical/persisted values invariant.
3. Shared ViewModels receive `IUiLocalizer` by constructor injection. MAUI XAML uses the
   host `{l:Localize Key}` extension (`xmlns:l="clr-namespace:Moba.MAUI.Localization"`).
4. For WinUI static text, give the element a stable `x:Uid` and map the semantic key to its
   `Uid.Text`, `Uid.Content`, `Uid.Header` or other property in `WinUiResourceMap.json`.
   Attached accessibility names use the fully qualified property syntax shown in that map.
   Existing Info/Help inline English values are design-time defaults; runtime text comes from
   x:Uid. Update defaults when changing their source meaning; never maintain German in XAML.
5. Run the generator and checks below. Commit both authored catalogues, map and generated
   `.resw` files. Never edit the generated resources independently.

```powershell
scripts/Update-LocalizationResources.ps1
scripts/Test-LocalizationResources.ps1
scripts/Test-LocalizationResources.Tests.ps1
scripts/Test-LineEndings.ps1 -Path SharedUI/Localization/UiStrings.resx,SharedUI/Localization/UiStrings.de-DE.resx,SharedUI/Localization/WinUiResourceMap.json,MOBAflow/Strings/en-US/Resources.resw,MOBAflow/Strings/de-DE/Resources.resw -Fix
```

The Quality workflow runs catalogue and tool-fixture checks. It rejects duplicate, missing or
empty keys, malformed/different placeholders, unknown referenced keys and stale projections.
The checker scans explicitly listed Slice 1 sources plus registered navigation tags; when
adding a new surface, extend that source list and coverage row rather than claiming whole-app coverage.

## Add a language

Agree the new target/scope in #140, add a matching shared satellite `.resx`, translate all keys
with context, then extend supported preference normalization, startup policy, choice list,
generator/checker language lists and tests. Add the matching generated WinUI language folder.
No page or domain logic should require translation-specific branching. Adding a resource file
alone does not make a language selectable. English remains the fallback.

## Validate the hosts

Use [the validation guide](../specs/011-ui-localization/quickstart.md) for builds/tests and the
manual matrix. Generated WinUI `MOBAflow.pri` and shared satellite assemblies must be shipped
with the host output. WinUI applies its MRTCore override before App XAML is initialized;
Android applies resource UI culture before App/Shell/views. Only CurrentUICulture changes;
CurrentCulture is retained and translated UI templates use explicit display culture.

Desktop/Android compile success does not prove font scaling, keyboard focus, accessibility,
themes or device restart behavior. Record those checks separately after authorized app launch.
No paid translation provider, network request or hardware access is required.
