import js from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import globals from 'globals';
import tseslint from 'typescript-eslint';

// Spec FR-077: the demo bundles only the parts it uses. Importing one of these packages as a
// whole is an error; import the single used part (for example 'lodash/debounce') instead.
const wholePackages = [
  '@fluentui/react-components',
  '@fluentui/react-icons',
  '@mui/material',
  '@mui/icons-material',
  'lodash',
  'react-icons',
];

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
          paths: wholePackages.map((name) => ({
            name,
            message: `Do not import the whole ${name} package (FR-077); import only the part the demo uses.`,
          })),
        },
      ],
    },
  },
);
