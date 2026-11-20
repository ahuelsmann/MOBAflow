import js from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import globals from 'globals';
import tseslint from 'typescript-eslint';

// Spec FR-077: the demo bundles only the parts it uses. Importing one of these packages as a
// whole (the package name, an index or barrel file, or an icon set) is an error; import the
// single used module instead (for example 'lodash/debounce').
const wholePackages = [
  '@fluentui/react-components',
  '@fluentui/react-icons',
  '@mui/material',
  '@mui/icons-material',
  'lodash',
  'lodash-es',
  'react-icons',
];

const message = (what) =>
  `Do not import ${what} as a whole (FR-077); import only the single module the demo uses.`;

const wholePackageNames = wholePackages
  .map((name) => name.replace(/[/@-]/g, (c) => `\\${c}`))
  .join('|');

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'coverage'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  reactHooks.configs.flat.recommended,
  {
    languageOptions: { globals: { ...globals.browser, ...globals.node } },
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: wholePackages.map((name) => ({ name, message: message(`the ${name} package`) })),
          patterns: [
            {
              // Icon sets: every react-icons subpath is a whole set.
              group: ['react-icons/*'],
              message: message('a react-icons icon set'),
            },
            {
              // Index and bundle files that re-export the whole package.
              group: [
                'lodash/lodash*',
                'lodash/index*',
                'lodash-es/lodash*',
                'lodash-es/index*',
                '@fluentui/react-components/index*',
                '@fluentui/react-icons/index*',
                '@fluentui/react-icons/lib',
                '@fluentui/react-icons/lib/index*',
                '@mui/material/index*',
                '@mui/icons-material/index*',
              ],
              message: message('a package index'),
            },
          ],
        },
      ],
      // Dynamic import() is not covered by no-restricted-imports.
      'no-restricted-syntax': [
        'error',
        {
          selector: `ImportExpression[source.value=/^(${wholePackageNames}|react-icons\\/.*)$/]`,
          message: message('a package via dynamic import()'),
        },
      ],
    },
  },
);
