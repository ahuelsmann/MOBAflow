import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

function readTokenKeys(fileName: string): string[] {
  const css = readFileSync(join(import.meta.dirname, fileName), 'utf8');
  const keys = [...css.matchAll(/^\s*(--[a-z0-9-]+)\s*:/gim)].map((match) => match[1]);
  return keys.sort();
}

describe('theme token files (FR-023)', () => {
  const light = readTokenKeys('light.css');
  const dark = readTokenKeys('dark.css');

  it('defines tokens in both themes', () => {
    expect(light.length).toBeGreaterThan(0);
    expect(dark.length).toBeGreaterThan(0);
  });

  it('defines the same token keys in Light and Dark', () => {
    expect(dark).toEqual(light);
  });

  it('defines the accent tokens in both themes', () => {
    const accentKeys = [
      '--accent-fill-default',
      '--accent-fill-secondary',
      '--accent-fill-tertiary',
      '--accent-text-default',
    ];
    for (const key of accentKeys) {
      expect(light, `light.css lacks ${key}`).toContain(key);
      expect(dark, `dark.css lacks ${key}`).toContain(key);
    }
  });

  it('declares each token once per theme', () => {
    expect(new Set(light).size).toBe(light.length);
    expect(new Set(dark).size).toBe(dark.length);
  });
});
