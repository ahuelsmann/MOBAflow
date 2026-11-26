import { fireEvent, render, screen, within } from '@testing-library/react';
import { expect, it } from 'vitest';
import { App } from '../../../App';
import { defaultSources, parseExamples } from '../../../examples/loader';
import { trainControlEnabled } from '../enabled';

const load = async () => {};
const examples = parseExamples(defaultSources);
const flagship = examples.locomotives.find((locomotive) => locomotive.selected)!;
const hint = 'Available in the MOBAflow desktop app';

it('switches Train Control on as the landing page with the flagship BR 211 selected (FR-032)', async () => {
  expect(trainControlEnabled).toBe(true);
  render(<App load={load} />);
  const main = await screen.findByRole('main');
  expect(screen.getByRole('button', { name: 'Train Control' })).toHaveAttribute('aria-current', 'page');
  expect(within(main).getByRole('heading', { name: 'Train Control' })).toBeVisible();
  expect(within(main).getByRole('button', { name: 'BR 211' })).toHaveAttribute('aria-pressed', 'true');
  expect(within(main).getByText('82')).toBeVisible();
  expect(within(main).getByText('0.42 A')).toBeVisible();
  expect(within(main).getByText('Regional service RB 21')).toBeVisible();
});

it('shows the prepared locomotive roster and all 32 named functions of the flagship', async () => {
  render(<App load={load} />);
  const page = await screen.findByRole('main');
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

it('keeps the series search, stop, driving settings, and cab tiles inert (FR-035, FR-036)', async () => {
  render(<App load={load} />);
  const page = within(await screen.findByRole('main'));
  const search = page.getByRole('searchbox', { name: 'Search by series' });
  expect(search).toHaveAttribute('readonly');
  expect(search).toHaveAttribute('aria-disabled', 'true');
  expect(search).toHaveAccessibleDescription(hint);

  const stop = page.getByRole('button', { name: 'EMERGENCY STOP' });
  const direction = page.getByRole('button', { name: 'Direction: Forward' });
  const steps = page.getByRole('button', { name: 'DCC speed steps: 126' });
  const ramp = page.getByRole('button', { name: 'Acceleration ramp' });
  const brake = page.getByRole('button', { name: /Brake/ });
  const doorRelease = page.getByRole('button', { name: /Door release/ });
  for (const button of [stop, direction, steps, ramp, brake, doorRelease]) {
    expect(button).toHaveAttribute('aria-disabled', 'true');
    expect(button).toHaveAccessibleDescription(hint);
    fireEvent.click(button);
  }

  const alternate = page.getByRole('button', { name: 'BR 110' });
  fireEvent.click(alternate);
  expect(page.getByRole('button', { name: 'BR 211' })).toHaveAttribute('aria-pressed', 'true');
  expect(alternate).toHaveAttribute('aria-pressed', 'false');
  expect(ramp).toHaveAttribute('aria-pressed', 'true');
  expect(brake).toHaveAttribute('aria-pressed', 'false');
  expect(doorRelease).toHaveAttribute('aria-pressed', 'false');
  expect(direction).toHaveAccessibleName('Direction: Forward');
  expect(steps).toHaveAccessibleName('DCC speed steps: 126');
});
