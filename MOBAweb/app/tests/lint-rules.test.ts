import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

const eslint = new ESLint({ cwd: join(import.meta.dirname, '..') });

async function lint(source: string) {
  const [result] = await eslint.lintText(source, { filePath: 'src/fixture.ts' });
  return result.messages;
}

describe('import rule against whole component and icon packages (FR-077)', () => {
  it('reports an error for a fixture importing whole packages', async () => {
    const fixture = readFileSync(
      join(import.meta.dirname, 'fixtures', 'whole-package-import.ts.fixture'),
      'utf8',
    );
    const messages = await lint(fixture);
    const restricted = messages.filter((m) => m.ruleId === 'no-restricted-imports');

    expect(restricted).toHaveLength(3);
    expect(restricted.every((m) => m.severity === 2)).toBe(true);
  });

  it('allows an import of one used part of a package', async () => {
    const messages = await lint("import debounce from 'lodash/debounce';\nexport { debounce };\n");
    expect(messages.filter((m) => m.ruleId === 'no-restricted-imports')).toEqual([]);
  });
});
