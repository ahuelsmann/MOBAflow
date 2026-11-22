# MOBAdemo frontend

React, TypeScript and Vite frontend of the MOBAdemo browser showcase (issue #430, spec
`specs/020-web-demo/`). This is the scaffold of slice 2: toolchain, theme tokens, page flags and the
start-up states. Pages, navigation and example data follow in later slices.

## Requirements

- Node.js 24 (`.nvmrc` says `24`; `package.json` allows `>=24 <25`) and npm 11.
- Moving to a newer Node major is its own slice.

## Commands

Run them in this folder.

| Command | What it does |
| --- | --- |
| `npm ci` | Installs the exact versions of `package-lock.json` |
| `npm run dev` | Starts the Vite dev server |
| `npm test` | Runs the Vitest unit tests (includes a build of the app into a temporary folder) |
| `npm run lint` | Runs ESLint, including the rule against whole component and icon packages |
| `npm run build` | Type-checks and builds the app into `dist/` |

## Rules worth knowing

- **No inline script or style.** The host serves the app under `script-src 'self'; style-src 'self'`.
  Vite's module preload polyfill is therefore off, and a test checks the built `index.html`.
- **No whole component or icon packages** (spec FR-077). ESLint rejects whole-package imports of
  `@fluentui/react-components`, `@fluentui/react-icons`, `lodash`, `lodash-es`, `@mui/material`,
  `@mui/icons-material` and `react-icons`, their index and bundle files, `lodash/fp`, `lodash/core`
  and every `react-icons/*` icon set, in static, re-export and dynamic form (`import()` needs a
  plain string literal). Single modules (`lodash/debounce`) and type-only imports stay allowed;
  a single Fluent icon is imported from its own module below `@fluentui/react-icons/lib/`. Exotic
  bypasses are left to the byte budget check of slice 10.
- **Page flags.** `src/config/enabledPages.ts` is composed from one `enabled.ts` per page under
  `src/pages/`. A page slice flips only its own flag, in the pull request whose browser checks pass.
- **Accent colour.** Accent colour: purple (maintainer decision 2026-11-20); the value is set in slice 20. Until then the accent tokens hold a neutral placeholder (T5).
