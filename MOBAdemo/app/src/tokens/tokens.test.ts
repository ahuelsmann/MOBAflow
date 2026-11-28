import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parseTokenKeys } from './tokenKeys';

function readTokenFile(fileName: string): string {
  return readFileSync(join(import.meta.dirname, fileName), 'utf8');
}

function readTokenKeys(fileName: string): string[] {
  return parseTokenKeys(readTokenFile(fileName));
}

function readHexToken(css: string, name: string): string {
  const match = new RegExp(`${name}:\\s*#([0-9a-f]{6});`).exec(css);
  if (!match?.[1]) {
    throw new Error(`Missing hex color token ${name}`);
  }
  return `#${match[1]}`;
}

function relativeLuminance(hex: string): number {
  const raw = hex.slice(1);
  const channels = [0, 2, 4]
    .map((offset) => Number.parseInt(raw.slice(offset, offset + 2), 16) / 255)
    .map((channel) => (channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4));
  return 0.2126 * channels[0]! + 0.7152 * channels[1]! + 0.0722 * channels[2]!;
}

function contrastRatio(foregroundHex: string, backgroundHex: string): number {
  const l1 = relativeLuminance(foregroundHex);
  const l2 = relativeLuminance(backgroundHex);
  return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
}

describe('theme token files (FR-023)', () => {
  const lightCss = readTokenFile('light.css');
  const darkCss = readTokenFile('dark.css');
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

  it('sets the MOBAflow purple accent palette in Dark and Light (T5)', () => {
    expect(readHexToken(darkCss, '--accent-fill-default')).toBe('#8764b8');
    expect(readHexToken(darkCss, '--accent-fill-secondary')).toBe('#7a58aa');
    expect(readHexToken(darkCss, '--accent-fill-tertiary')).toBe('#6c4d99');
    expect(readHexToken(darkCss, '--accent-text-default')).toBe('#c8b1eb');

    expect(readHexToken(lightCss, '--accent-fill-default')).toBe('#744da9');
    expect(readHexToken(lightCss, '--accent-fill-secondary')).toBe('#815bb4');
    expect(readHexToken(lightCss, '--accent-fill-tertiary')).toBe('#8f6cc0');
    expect(readHexToken(lightCss, '--accent-text-default')).toBe('#5b398c');
  });

  it.each([
    ['dark', darkCss],
    ['light', lightCss],
  ])('keeps accent text >= 4.5:1 and accent fill >= 3:1 on surfaces in %s', (_theme, css) => {
    const bgDefault = readHexToken(css, '--background-default');
    const bgCard = readHexToken(css, '--background-card');
    const accentText = readHexToken(css, '--accent-text-default');
    const accentFill = readHexToken(css, '--accent-fill-default');
    const accentEmphasis = readHexToken(css, _theme === 'dark' ? '--accent-fill-tertiary' : '--accent-fill-default');
    const onAccent = readHexToken(css, '--text-on-accent');
    const dangerFill = readHexToken(css, '--danger-fill');

    expect(contrastRatio(onAccent, accentEmphasis)).toBeGreaterThanOrEqual(4.5);
    expect(contrastRatio(onAccent, dangerFill)).toBeGreaterThanOrEqual(4.5);

    expect(contrastRatio(accentText, bgDefault)).toBeGreaterThanOrEqual(4.5);
    expect(contrastRatio(accentText, bgCard)).toBeGreaterThanOrEqual(4.5);
    expect(contrastRatio(accentFill, bgDefault)).toBeGreaterThanOrEqual(3);
    expect(contrastRatio(accentFill, bgCard)).toBeGreaterThanOrEqual(3);
  });
});
