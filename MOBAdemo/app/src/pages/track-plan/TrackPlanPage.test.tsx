import { fireEvent, render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { App } from '../../App';
import { trackPlanEnabled } from './enabled';

const load = async () => {};
const inertHint = 'Available in the MOBAflow desktop app';

beforeEach(() => localStorage.clear());

describe('TrackPlanPage (FR-035, FR-038, US2 scenarios 1 & 4)', () => {
  it('enables Track Plan in slice 15', () => {
    expect(trackPlanEnabled).toBe(true);
  });

  it('renders toolbar, Piko A-Track catalogue, SVG canvas, zoom slider, context pane and footer', async () => {
    render(<App load={load} />);
    const nav = await screen.findByRole('navigation', { name: 'MOBAflow pages' });
    const trackPlanNav = within(nav).getByRole('button', { name: 'Track Plan' });
    expect(trackPlanNav).not.toHaveAttribute('aria-disabled');

    fireEvent.click(trackPlanNav);
    expect(trackPlanNav).toHaveAttribute('aria-current', 'page');

    const main = within(screen.getByRole('main'));
    expect(main.getByRole('heading', { level: 1, name: 'Track Plan' })).toBeVisible();

    const toolbar = main.getByRole('toolbar', { name: 'Track Plan toolbar' });
    for (const label of ['Undo', 'Redo', 'Export', 'Validate', 'Fit', '100%', 'Context', 'Snap', 'Disconnect', 'Delete', 'Grid', 'Validation Overlay', 'Track Labels', 'Rotate left', 'Rotate right']) {
      const button = within(toolbar).getByRole('button', { name: label });
      expect(button).toHaveAttribute('aria-disabled', 'true');
      expect(button).toHaveAccessibleDescription(inertHint);
    }

    const catalogue = main.getByRole('region', { name: 'Piko A-Track catalogue' });
    expect(within(catalogue).getByRole('heading', { level: 2, name: 'Piko A-Track' })).toBeVisible();
    for (const category of ['Straight', 'Curve', 'Switch', 'Crossing']) {
      expect(within(catalogue).getByRole('heading', { level: 3, name: category })).toBeVisible();
    }
    for (const itemLabel of ['G231 (230.93 mm)', 'G239 (239.07 mm)', 'G115 (115.46 mm)', 'R1 (360 mm / 30°)', 'R2 (422 mm / 30°)', 'R3 (484 mm / 30°)', 'WL Left Turnout 15°', 'WR Right Turnout 15°', 'DKW Double Slip Switch', 'K15 Crossing 15°']) {
      const item = within(catalogue).getByRole('button', { name: itemLabel });
      expect(item).toHaveAttribute('aria-disabled', 'true');
      expect(item).toHaveAccessibleDescription(inertHint);
    }

    const canvas = main.getByRole('region', { name: 'Track Plan canvas' });
    const svgImage = within(canvas).getByRole('img', { name: 'Example model railway layout on Track Plan canvas' });
    expect(svgImage).toHaveAttribute('src', '/assets/track-plan.svg');

    const zoomGroup = within(canvas).getByRole('group', { name: 'Zoom controls' });
    const zoomSlider = within(zoomGroup).getByRole('slider', { name: 'Zoom' });
    expect(zoomSlider).toHaveValue('100');
    expect(zoomSlider).toHaveAttribute('aria-valuetext', '100%');
    expect(zoomSlider).toHaveAttribute('aria-disabled', 'true');
    expect(zoomSlider).toHaveAccessibleDescription(inertHint);
    expect(within(zoomGroup).getByText('100%')).toBeVisible();

    const context = main.getByRole('complementary', { name: 'Context properties' });
    expect(within(context).getByRole('heading', { level: 2, name: 'Context' })).toBeVisible();
    expect(within(context).getByText('MOBAflow example layout')).toBeVisible();
    expect(within(context).getByText('H0 (1:87)')).toBeVisible();
    expect(within(context).getByText('Piko A-Track')).toBeVisible();
    expect(within(context).getByText('No selection')).toBeVisible();
    expect(within(context).getByText('12')).toBeVisible();
    expect(within(context).getByText('11')).toBeVisible();
    expect(within(context).getByText('Open ends')).toBeVisible();
    expect(within(context).getByRole('button', { name: 'Create Section' })).toHaveAttribute('aria-disabled', 'true');
    expect(within(context).getByRole('button', { name: 'Toggle Isolator' })).toHaveAttribute('aria-disabled', 'true');

    const footer = main.getByRole('region', { name: 'Track Plan status' });
    expect(within(footer).getByText('Ready — Example layout loaded')).toBeVisible();
    expect(within(footer).getByText('X: 0 mm, Y: 0 mm')).toBeVisible();
  });

  it('keeps every control and the zoom slider completely inert on interaction (FR-035, US2 scenario 4)', async () => {
    render(<App load={load} />);
    const nav = await screen.findByRole('navigation', { name: 'MOBAflow pages' });
    const trackPlanNav = within(nav).getByRole('button', { name: 'Track Plan' });
    fireEvent.click(trackPlanNav);

    const main = within(screen.getByRole('main'));
    const zoomGroup = main.getByRole('group', { name: 'Zoom controls' });
    const zoomSlider = within(zoomGroup).getByRole('slider', { name: 'Zoom' }) as HTMLInputElement;

    fireEvent.focus(zoomSlider);
    expect(within(zoomGroup).getByRole('tooltip')).toHaveTextContent(inertHint);
    fireEvent.keyDown(zoomSlider, { key: 'ArrowRight' });
    fireEvent.input(zoomSlider, { target: { value: '150' } });
    fireEvent.change(zoomSlider, { target: { value: '200' } });
    expect(zoomSlider.value).toBe('100');
    expect(zoomSlider).toHaveAttribute('aria-valuetext', '100%');
    expect(within(zoomGroup).getByText('100%')).toBeVisible();

    fireEvent.keyDown(zoomSlider, { key: 'Escape' });
    expect(within(zoomGroup).queryByRole('tooltip')).not.toBeInTheDocument();
    fireEvent.blur(zoomSlider);

    const toolbar = main.getByRole('toolbar', { name: 'Track Plan toolbar' });
    const snap = within(toolbar).getByRole('button', { name: 'Snap' });
    const grid = within(toolbar).getByRole('button', { name: 'Grid' });
    fireEvent.click(snap);
    fireEvent.click(grid);
    expect(snap).toHaveAttribute('aria-pressed', 'true');
    expect(grid).toHaveAttribute('aria-pressed', 'false');

    const catalogue = main.getByRole('region', { name: 'Piko A-Track catalogue' });
    fireEvent.click(within(catalogue).getByRole('button', { name: 'G231 (230.93 mm)' }));
    expect(main.getByRole('complementary', { name: 'Context properties' })).toHaveTextContent('No selection');
    expect(trackPlanNav).toHaveAttribute('aria-current', 'page');
  });

  it('keeps Track Plan open when switching between Dark and Light themes (FR-021)', async () => {
    render(<App load={load} />);
    const nav = await screen.findByRole('navigation', { name: 'MOBAflow pages' });
    const trackPlanNav = within(nav).getByRole('button', { name: 'Track Plan' });
    fireEvent.click(trackPlanNav);

    const themeButton = screen.getByRole('button', { name: 'Light theme' });
    fireEvent.click(themeButton);
    expect(document.documentElement).toHaveAttribute('data-theme', 'light');
    expect(trackPlanNav).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('heading', { level: 1, name: 'Track Plan' })).toBeVisible();

    fireEvent.click(themeButton);
    expect(document.documentElement).toHaveAttribute('data-theme', 'dark');
    expect(trackPlanNav).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('heading', { level: 1, name: 'Track Plan' })).toBeVisible();
  });
});
