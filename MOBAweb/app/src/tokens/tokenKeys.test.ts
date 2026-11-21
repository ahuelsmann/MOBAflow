import { describe, expect, it } from 'vitest';
import { parseTokenKeys } from './tokenKeys';

describe('parseTokenKeys', () => {
  it('finds declarations on separate lines', () => {
    expect(parseTokenKeys(':root {\n  --a: 1;\n  --b: 2;\n}')).toEqual(['--a', '--b']);
  });

  it('finds several declarations on one line', () => {
    expect(parseTokenKeys(':root { --a: 1; --b: 2;--c:3 }')).toEqual(['--a', '--b', '--c']);
  });

  it('ignores comments and var() references', () => {
    const css = ':root { /* --hidden: 1; */ --a: 1; color: var(--a); }';
    expect(parseTokenKeys(css)).toEqual(['--a']);
  });
});
