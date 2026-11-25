import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { App } from '../../App';

const load = async () => {};
const notice = 'Desktop UI demo - best on a screen at least 1024 px wide';

function resize(width: number) {
  vi.stubGlobal('innerWidth', width);
  window.dispatchEvent(new Event('resize'));
}

afterEach(() => vi.unstubAllGlobals());

it.each([[1023, 'narrow'], [1024, 'compact'], [1279, 'compact'], [1280, 'full']])(
  'uses the expected layout at %i pixels',
  async (width, mode) => {
    resize(width);
    render(<App load={load} />);
    const main = await screen.findByRole('main');
    expect(main.closest('[data-shell-mode]')).toHaveAttribute('data-shell-mode', mode);
    expect(screen.queryByText(notice) !== null).toBe(mode === 'narrow');
  },
);

it('remembers dismissing the narrow-window notice only until reload', async () => {
  resize(800);
  const app = render(<App load={load} />);
  expect(await screen.findByText(notice)).toBeVisible();
  fireEvent.click(screen.getByRole('button', { name: 'Dismiss narrow-window notice' }));
  expect(screen.queryByText(notice)).not.toBeInTheDocument();
  app.unmount();

  render(<App load={load} />);
  expect(await screen.findByText(notice)).toBeVisible();
});
