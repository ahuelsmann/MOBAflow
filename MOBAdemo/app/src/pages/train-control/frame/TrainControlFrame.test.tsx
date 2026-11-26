import { fireEvent, render, screen, within } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { DemoShell } from '../../../shell/navigation/DemoShell';
import { defaultSources, parseExamples } from '../../../examples/loader';
import { trainControlEnabled } from '../enabled';

vi.mock('../../../config/enabledPages', () => ({
  enabledPages: { 'train-control': true, workflows: false, 'track-plan': false, 'signal-box': false },
}));

const examples = parseExamples(defaultSources);
const flagship = examples.locomotives.find((locomotive) => locomotive.selected)!;
const hint = 'Available in the MOBAflow desktop app';

it('keeps the shipped Train Control entry disabled until the page is complete', () => {
  expect(trainControlEnabled).toBe(false);
});

it('shows the prepared locomotive roster and all 32 named functions of the flagship', () => {
  render(<DemoShell />);
  const page = screen.getByRole('main');
  expect(within(page).getByRole('heading', { name: 'Train Control' })).toBeVisible();
  const roster = within(page).getByRole('region', { name: 'Locomotives' });
  for (const locomotive of examples.locomotives) {
    const card = within(roster).getByRole('button', { name: locomotive.name });
    expect(card).toHaveAttribute('aria-disabled', 'true');
    expect(card).toHaveAttribute('aria-pressed', String(locomotive.selected));
    expect(card.querySelector('img')?.getAttribute('src')).toBe(locomotive.photoUrl);
  }

  const functions = within(page).getByRole('region', { name: 'Functions' });
  expect(within(functions).getAllByRole('button')).toHaveLength(32);
  for (const item of flagship.functions) {
    const button = within(functions).getByRole('button', { name: 'F' + item.number + ' ' + item.name });
    expect(button).toHaveAttribute('aria-pressed', String(item.pressed));
    expect(button).toHaveAttribute('aria-disabled', 'true');
    expect(button.querySelector('img')?.getAttribute('src')).toBeTruthy();
    expect(button).toHaveAccessibleDescription(hint);
  }
});

it('keeps the series search, stop and prepared driving controls inert', () => {
  render(<DemoShell />);
  const page = within(screen.getByRole('main'));
  const search = page.getByRole('searchbox', { name: 'Search by series' });
  expect(search).toHaveAttribute('readonly');
  expect(search).toHaveAttribute('aria-disabled', 'true');
  expect(search).toHaveAccessibleDescription(hint);

  const stop = page.getByRole('button', { name: 'EMERGENCY STOP' });
  const direction = page.getByRole('button', { name: 'Direction: Forward' });
  const steps = page.getByRole('button', { name: 'DCC speed steps: 126' });
  const ramp = page.getByRole('button', { name: 'Acceleration ramp' });
  for (const button of [stop, direction, steps, ramp]) {
    expect(button).toHaveAttribute('aria-disabled', 'true');
    expect(button).toHaveAccessibleDescription(hint);
    fireEvent.click(button);
  }

  const alternate = page.getByRole('button', { name: 'BR 110' });
  fireEvent.click(alternate);
  expect(page.getByRole('button', { name: 'BR 211' })).toHaveAttribute('aria-pressed', 'true');
  expect(alternate).toHaveAttribute('aria-pressed', 'false');
  expect(ramp).toHaveAttribute('aria-pressed', 'true');
  expect(direction).toHaveAccessibleName('Direction: Forward');
  expect(steps).toHaveAccessibleName('DCC speed steps: 126');
});
