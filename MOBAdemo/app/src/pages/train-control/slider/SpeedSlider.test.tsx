import { fireEvent, render, screen, within } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { App } from '../../../App';
import { defaultSources, parseExamples } from '../../../examples/loader';
import { trainControlEnabled } from '../enabled';

vi.mock('../../../config/enabledPages', () => ({
  enabledPages: { 'train-control': true, workflows: false, 'track-plan': false, 'signal-box': false },
}));

const load = async () => {};
const preparedStep = parseExamples(defaultSources).locomotives.find((locomotive) => locomotive.selected)!.prepared.speed;

beforeEach(() => localStorage.clear());

function pageWithoutSlider(page: HTMLElement) {
  const copy = page.cloneNode(true) as HTMLElement;
  copy.querySelector('[aria-label="Speed control"]')?.remove();
  return copy.innerHTML;
}

it('enables Train Control in the shipped configuration', () => {
  expect(trainControlEnabled).toBe(true);
});

it('changes only the speed control and exposes the full range with Step X text', async () => {
  render(<App load={load} />);
  const page = await screen.findByRole('main');
  const slider = within(page).getByRole('slider', { name: 'Speed' });
  const control = within(page).getByRole('group', { name: 'Speed control' });
  expect(slider).toHaveAttribute('min', '0');
  expect(slider).toHaveAttribute('max', '126');
  expect(slider).toHaveAttribute('step', '1');
  expect(slider).toHaveValue(String(preparedStep));
  expect(slider).toHaveAttribute('aria-valuetext', 'Step ' + preparedStep);
  expect(within(control).getByText('Step ' + preparedStep)).toBeVisible();

  const unchanged = pageWithoutSlider(page);
  for (const step of [0, 84, 126]) {
    fireEvent.change(slider, { target: { value: String(step) } });
    expect(slider).toHaveValue(String(step));
    expect(slider).toHaveAttribute('aria-valuetext', 'Step ' + step);
    expect(within(control).getByText('Step ' + step)).toBeVisible();
    expect(pageWithoutSlider(page)).toBe(unchanged);
  }
  expect(localStorage.length).toBe(0);
});

it('keeps the selected page and speed during both theme switches', async () => {
  render(<App load={load} />);
  const slider = await screen.findByRole('slider', { name: 'Speed' });
  const toggle = screen.getByRole('button', { name: 'Light theme' });
  const navigation = screen.getByRole('navigation');
  const entry = within(navigation).getByRole('button', { name: 'Train Control' });
  fireEvent.change(slider, { target: { value: '84' } });

  for (const theme of ['light', 'dark']) {
    fireEvent.click(toggle);
    expect(document.documentElement).toHaveAttribute('data-theme', theme);
    expect(entry).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('heading', { name: 'Train Control' })).toBeVisible();
    expect(screen.getByRole('slider', { name: 'Speed' })).toBe(slider);
    expect(slider).toHaveValue('84');
    expect(slider).toHaveAttribute('aria-valuetext', 'Step 84');
  }
  expect(Object.entries(localStorage)).toEqual([['mobademo-theme', 'dark']]);
});
