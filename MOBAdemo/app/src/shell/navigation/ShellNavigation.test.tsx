import { readFileSync } from 'node:fs';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from '../../App';
import navigation from '../../examples/navigation.json';

vi.mock('../../config/enabledPages', () => ({
  enabledPages: { 'train-control': true, workflows: true, 'track-plan': false, 'signal-box': false },
}));

const load = async () => {};
const disabledHint = 'Available in the MOBAflow desktop app (not included in this web demo)';

beforeEach(() => localStorage.clear());

describe('shell navigation', () => {
  it('renders the ordered groups, titles, bold entries, badges and custom path from the model', async () => {
    render(<App load={load} />);
    const pane = await screen.findByRole('navigation');
    const rows = within(pane).getAllByRole('button');
    expect(rows).toHaveLength(navigation.entries.length);
    navigation.entries.forEach((entry, index) => {
      const row = rows[index]!;
      const expectedName = entry.badge ? `${entry.title} ${entry.badge}` : entry.title;
      expect(row).toHaveAccessibleName(expectedName);
      expect(row.closest('ul')).toHaveAccessibleName(entry.category);
      expect(row.querySelector('strong')?.textContent ?? null).toBe(entry.isBold ? entry.title : null);
      if (entry.badge) {
        expect(within(row).getByText(entry.badge)).toBeVisible();
        expect(row).toHaveAccessibleDescription(expect.stringContaining(entry.badge));
      }
      expect(row.querySelector('svg path')?.getAttribute('d')).toBeTruthy();
      if (entry.pathIconData) expect(row.querySelector('path')).toHaveAttribute('d', entry.pathIconData);
    });
  });

  it('explains unavailable entries and leaves the selected page intact', async () => {
    render(<App load={load} />);
    const overview = await screen.findByRole('button', { name: 'Overview' });
    expect(overview).toHaveAttribute('aria-disabled', 'true');
    expect(overview).toHaveAccessibleDescription(disabledHint);
    expect(overview).toHaveAttribute('title', 'Overview\n' + disabledHint);
    fireEvent.click(overview);
    expect(screen.getByRole('button', { name: 'Train Control' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('button', { name: 'Track Plan' })).toHaveAttribute('aria-disabled', 'true');
  });

  it('keeps the current page when changing theme and reloads to Train Control', async () => {
    const view = render(<App load={load} />);
    fireEvent.click(await screen.findByRole('button', { name: 'Workflows' }));
    expect(screen.getByRole('heading', { name: 'Workflows' })).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: 'Light theme' }));
    expect(screen.getByRole('button', { name: 'Workflows' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('heading', { name: 'Workflows' })).toBeVisible();
    expect(document.documentElement).toHaveAttribute('data-theme', 'light');
    expect(Object.entries(localStorage)).toEqual([['mobademo-theme', 'light']]);
    view.unmount();
    render(<App load={load} />);
    expect(await screen.findByRole('heading', { name: 'Train Control' })).toBeVisible();
    expect(document.documentElement).toHaveAttribute('data-theme', 'light');
  });

  it('applies stored Light to loading and error states', async () => {
    localStorage.setItem('mobademo-theme', 'light');
    render(<App load={() => Promise.reject(new Error('Unavailable'))} />);
    expect(screen.getByRole('status')).toBeVisible();
    expect(document.documentElement).toHaveAttribute('data-theme', 'light');
    expect(await screen.findByRole('alert')).toBeVisible();
    expect(document.documentElement).toHaveAttribute('data-theme', 'light');
  });

  it('defaults to Dark without consulting browser preference or writing storage', async () => {
    const matchMedia = vi.fn(() => ({ matches: true }));
    vi.stubGlobal('matchMedia', matchMedia);
    try {
      render(<App load={load} />);
      expect(await screen.findByRole('button', { name: 'Light theme' })).toHaveAttribute('aria-pressed', 'false');
      expect(document.documentElement).toHaveAttribute('data-theme', 'dark');
      expect(localStorage.length).toBe(0);
      expect(matchMedia).not.toHaveBeenCalled();
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it.each(['dark', 'light'])('keeps disabled navigation contrast at or above 3:1 in %s', (theme) => {
    const css = readFileSync(import.meta.dirname + '/../../tokens/' + theme + '.css', 'utf8');
    function luminance(token: string) {
      const hex = new RegExp('--' + token + ': #([0-9a-f]{6});').exec(css)?.[1];
      if (!hex) throw new Error('Missing color token ' + token);
      const rgb = [0, 2, 4].map((index) => Number.parseInt(hex.slice(index, index + 2), 16) / 255)
        .map((value) => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
      return 0.2126 * rgb[0]! + 0.7152 * rgb[1]! + 0.0722 * rgb[2]!;
    }
    const foreground = luminance('navigation-disabled-text');
    const background = luminance('background-card');
    expect((Math.max(foreground, background) + 0.05) / (Math.min(foreground, background) + 0.05))
      .toBeGreaterThanOrEqual(3);
  });
});
