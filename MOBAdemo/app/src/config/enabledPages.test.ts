import { describe, expect, it } from 'vitest';
import { enabledPages } from './enabledPages';

describe('enabledPages', () => {
  it('lists the four demo pages', () => {
    expect(Object.keys(enabledPages).sort()).toEqual([
      'signal-box',
      'track-plan',
      'train-control',
      'workflows',
    ]);
  });

  it('has every page switched off until its last slice enables it', () => {
    for (const [page, enabled] of Object.entries(enabledPages)) {
      expect(enabled, `${page} must be off`).toBe(false);
    }
  });
});
