import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

const eslint = new ESLint({ cwd: join(import.meta.dirname, '..') });
const restrictedRules = new Set([
  'no-restricted-imports',
  '@typescript-eslint/no-restricted-imports',
  'no-restricted-syntax',
  '@typescript-eslint/no-require-imports',
]);

async function restrictedMessages(source: string) {
  const [result] = await eslint.lintText(source, { filePath: 'src/fixture.ts' });
  return result.messages.filter((m) => m.ruleId !== null && restrictedRules.has(m.ruleId));
}

describe('import rule against whole component and icon packages (FR-077)', () => {
  it('reports an error for a fixture importing whole packages', async () => {
    const fixture = readFileSync(
      join(import.meta.dirname, 'fixtures', 'whole-package-import.ts.fixture'),
      'utf8',
    );
    const restricted = await restrictedMessages(fixture);

    expect(restricted).toHaveLength(3);
    expect(restricted.every((m) => m.severity === 2)).toBe(true);
  });

  it.each([
    // static imports
    "import * as I from 'react-icons/fa';\nexport { I };\n",
    "import _ from 'lodash/lodash.js';\nexport { _ };\n",
    "import * as F from '@fluentui/react-icons/lib/index';\nexport { F };\n",
    "import _ from 'lodash-es';\nexport { _ };\n",
    "import { Button } from '@mui/material/index';\nexport { Button };\n",
    "import fp from 'lodash/fp';\nexport { fp };\n",
    "import core from 'lodash/core';\nexport { core };\n",
    "import _ from 'lodash/index.js';\nexport { _ };\n",
    "import _ from 'lodash-es/index';\nexport { _ };\n",
    // re-exports and require
    "export * from 'react-icons/fa';\n",
    "const _ = require('lodash');\nexport { _ };\n",
    // dynamic imports with a literal source follow the same names and patterns
    "export const load = () => import('lodash');\n",
    "export const load = () => import('react-icons/fa');\n",
    "export const load = () => import('@fluentui/react-components');\n",
    "export const load = () => import('lodash/lodash.js');\n",
    "export const load = () => import('@fluentui/react-icons/lib/index');\n",
    // dynamic imports whose source is not a plain string literal
    'export const load = () => import(`lodash`);\n',
    "export const load = () => import('lod' + 'ash');\n",
    "export const load = (x: string) => import('react-icons/' + x);\n",
    'export const load = (name: string) => import(name);\n',
  ])('rejects the whole-package import %#', async (source) => {
    const restricted = await restrictedMessages(source);
    expect(restricted.length).toBeGreaterThan(0);
    expect(restricted.every((m) => m.severity === 2)).toBe(true);
  });

  it.each([
    "import debounce from 'lodash/debounce';\nexport { debounce };\n",
    "import debounce from 'lodash-es/debounce';\nexport { debounce };\n",
    "import indexOf from 'lodash/indexOf';\nexport { indexOf };\n",
    "import indexOf from 'lodash-es/indexOf.js';\nexport { indexOf };\n",
    "export const load = () => import('lodash/debounce');\n",
    "import { useState } from 'react';\nexport { useState };\n",
    "import type { ButtonProps } from '@fluentui/react-components';\nexport type { ButtonProps };\n",
    "import { type ButtonProps } from '@fluentui/react-components';\nexport type { ButtonProps };\n",
    "import { Icon } from '@fluentui/react-icons/lib/icons/chunk-0';\nexport { Icon };\n",
  ])('allows the single-module or type-only import %#', async (source) => {
    expect(await restrictedMessages(source)).toEqual([]);
  });
});
