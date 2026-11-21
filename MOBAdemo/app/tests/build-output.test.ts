import { mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from 'vite';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';

describe('built entry document', () => {
  let outDir: string;
  let html: string;

  beforeAll(async () => {
    outDir = mkdtempSync(join(tmpdir(), 'mobademo-build-'));
    await build({ logLevel: 'silent', build: { outDir, emptyOutDir: true } });
    html = readFileSync(join(outDir, 'index.html'), 'utf8');
  }, 60_000);

  afterAll(() => {
    rmSync(outDir, { recursive: true, force: true });
  });

  it('carries the no-JavaScript notice', () => {
    expect(html).toMatch(
      /<noscript>[\s\S]*MOBAdemo needs JavaScript\. Please enable it to view the demo\.[\s\S]*<\/noscript>/,
    );
  });

  it('holds under script-src self and style-src self: no inline script or style', () => {
    const inlineScripts = [...html.matchAll(/<script\b([^>]*)>/gi)].filter(
      (match) => !/\bsrc=/.test(match[1]),
    );
    expect(inlineScripts).toEqual([]);
    expect(html).not.toMatch(/<style\b/i);
    expect(html).not.toMatch(/\sstyle=/i);
    expect(html).not.toMatch(/\son[a-z]+=/i);
  });
});
