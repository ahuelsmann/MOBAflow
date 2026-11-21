import { signalBoxEnabled } from '../pages/signal-box/enabled';
import { trackPlanEnabled } from '../pages/track-plan/enabled';
import { trainControlEnabled } from '../pages/train-control/enabled';
import { workflowsEnabled } from '../pages/workflows/enabled';

/**
 * Compile-time list of the demo pages that are switched on. Each page slice flips only its own
 * constant, in the pull request whose browser checks pass; slice 19 removes this list.
 */
export const enabledPages = {
  'train-control': trainControlEnabled,
  workflows: workflowsEnabled,
  'track-plan': trackPlanEnabled,
  'signal-box': signalBoxEnabled,
} as const;
