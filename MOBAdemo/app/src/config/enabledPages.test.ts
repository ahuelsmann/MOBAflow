import { describe, expect, it } from 'vitest';
import { signalBoxEnabled } from '../pages/signal-box/enabled';
import { trackPlanEnabled } from '../pages/track-plan/enabled';
import { trainControlEnabled } from '../pages/train-control/enabled';
import { workflowsEnabled } from '../pages/workflows/enabled';
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

  it('wires each page entry to its own flag module and keeps Train Control enabled', () => {
    expect(enabledPages).toEqual({
      'train-control': trainControlEnabled,
      workflows: workflowsEnabled,
      'track-plan': trackPlanEnabled,
      'signal-box': signalBoxEnabled,
    });
    expect(trainControlEnabled).toBe(true);
  });
});
