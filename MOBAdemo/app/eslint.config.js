import js from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import globals from 'globals';
import tseslint from 'typescript-eslint';

// Spec FR-077: the demo bundles only the parts it uses. Importing one of these packages as a
// whole (the package name, an index or bundle file, a whole-library wrapper or an icon set) is
// an error; import the single used module instead (for example 'lodash/debounce'). Type-only
// imports add no bytes and stay allowed. Static, re-export and dynamic imports follow the same
// names and patterns; a dynamic import() must have a plain string literal source. Exotic
// bypasses beyond this are caught by the byte budget of slice 10.
const wholePackages = [
  '@fluentui/react-components',
  '@fluentui/react-icons',
  '@fluentui/react-icons/lib', // the directory import; single modules below lib stay allowed
  '@mui/material',
  '@mui/icons-material',
  'lodash',
  'lodash-es',
  'react-icons',
];

// Gitignore-style patterns. Only the entry files of lodash are listed, so 'lodash/indexOf' passes.
const wholePackagePatterns = [
  'react-icons/*', // every react-icons subpath is a whole icon set
  'lodash/lodash*',
  'lodash/index',
  'lodash/index.js',
  'lodash/fp',
  'lodash/core',
  'lodash-es/lodash*',
  'lodash-es/index',
  'lodash-es/index.js',
  '@fluentui/react-components/index*',
  '@fluentui/react-icons/index*',
  '@fluentui/react-icons/lib/index*',
  '@mui/material/index*',
  '@mui/icons-material/index*',
];

const message = (what) =>
  `Do not import ${what} as a whole (FR-077); import only the single module the demo uses.`;

const escape = (text) => text.replace(/[.+?^${}()|[\]\\/-]/g, String.raw`\$&`);
const globToRegex = (glob) => escape(glob).replaceAll('*', '[^/]*');
const restrictedSourceRegex = `^(${[
  ...wholePackages.map(escape),
  ...wholePackagePatterns.map(globToRegex),
].join('|')})$`;

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'coverage', 'playwright-report', 'test-results'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  reactHooks.configs.flat.recommended,
  {
    languageOptions: { globals: { ...globals.browser, ...globals.node } },
    rules: {
      'no-restricted-imports': 'off',
      '@typescript-eslint/no-restricted-imports': [
        'error',
        {
          paths: wholePackages.map((name) => ({
            name,
            message: message(`the ${name} package`),
            allowTypeImports: true,
          })),
          patterns: [
            {
              group: wholePackagePatterns,
              message: message('a package index, bundle or icon set'),
              allowTypeImports: true,
            },
          ],
        },
      ],
      // Dynamic import() is not covered by the import rule.
      'no-restricted-syntax': [
        'error',
        {
          selector: "ImportExpression:not([source.type='Literal'])",
          message: 'A dynamic import() needs a plain string literal source (FR-077).',
        },
        {
          selector: `ImportExpression[source.type='Literal'][source.value=/${restrictedSourceRegex}/]`,
          message: message('a package via dynamic import()'),
        },
      ],
    },
  },
);
