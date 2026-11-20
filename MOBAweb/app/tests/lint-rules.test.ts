import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

const eslint = new ESLint({ cwd: join(import.meta.dirname, '..') });
const restrictedRules = ['no-restricted-imports', 'no-restricted-syntax'];

async function restrictedMessages(source: string) {
  const [result] = await eslint.lintText(source, { filePath: 'src/fixture.ts' });
  return result.messages.filter((m) => m.ruleId !== null && restrictedRules.includes(m.ruleId));
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
    "import * as I from 'react-icons/fa';\nexport { I };\n",
    "import _ from 'lodash/lodash.js';\nexport { _ };\n",
    "import * as F from '@fluentui/react-icons/lib/index';\nexport { F };\n",
    "import _ from 'lodash-es';\nexport { _ };\n",
    "export const load = () => import('lodash');\n",
    "export const load = () => import('react-icons/fa');\n",
    "export const load = () => import('@fluentui/react-components');\n",
    "import { Button } from '@mui/material/index';\nexport { Button };\n",
  ])('rejects the whole-package import %#', async (source) => {
    const restricted = await restrictedMessages(source);
    expect(restricted.length).toBeGreaterThan(0);
    expect(restricted.every((m) => m.severity === 2)).toBe(true);
  });

  it.each([
    "import debounce from 'lodash/debounce';\nexport { debounce };\n",
    "import debounce from 'lodash-es/debounce';\nexport { debounce };\n",
    "export const load = () => import('lodash/debounce');\n",
    "import { useState } from 'react';\nexport { useState };\n",
  ])('allows the single-module import %#', async (source) => {
    expect(await restrictedMessages(source)).toEqual([]);
  });
});
