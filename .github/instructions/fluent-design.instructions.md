---
description: 'MOBAflow-specific WinUI resources, layout and visual validation.'
applyTo: 'MOBAflow/**/*.xaml'
---

# WinUI design
Use [AGENTS.md](../../AGENTS.md) for architecture, validation and launch authorization.
Reuse [ControlStyles.xaml](../../MOBAflow/Resources/ControlStyles.xaml) and existing page patterns.
Available styles include DangerButtonStyle, BacklightToggleButtonStyle, PreviewBadgeStyle,
PreviewBadgeTextStyle and NeonGlowButtonStyle; verify a key exists before referencing it.

- Use ThemeResource for theme-dependent brushes in normal, selected, hover and disabled states.
- Reuse native text styles, existing spacing and English UI labels; avoid local copies of shared styles.
- Use star sizing for resizable content; persist proportions as *ColumnStarValue.
  Use Auto for bounded controls and pixels only where intentionally fixed.
- Wrap or trim long text deliberately. Keep one scrolling owner; avoid nesting a self-scrolling ListView
  inside another ScrollViewer. Check narrow layouts and long content.
- Compile affected views. Inspect changed states in Light and Dark only with explicit app-start approval;
  otherwise report visual acceptance as open. Do not hide XAML errors by excluding active pages.
