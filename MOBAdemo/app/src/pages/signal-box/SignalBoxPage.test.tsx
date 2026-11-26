import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { defaultSources, parseExamples } from '../../examples/loader';
import { signalBoxEnabled } from './enabled';
import { SignalBoxPage } from './SignalBoxPage';

const examples = parseExamples(defaultSources);
const { signalBox, texts } = examples;
const hint = texts.inertHint;

function stripHints(root: HTMLElement): string {
  const clone = root.cloneNode(true) as HTMLElement;
  clone.querySelectorAll('.demo-inert-hint').forEach((node) => node.remove());
  return clone.innerHTML;
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('SignalBoxPage (FR-039, FR-045, FR-046)', () => {
  it('enables Signal Box in enabled.ts for Slice 16', () => {
    expect(signalBoxEnabled).toBe(true);
  });

  it('renders the inert Elements toolbox and grid metadata', () => {
    render(<SignalBoxPage />);
    const toolbox = screen.getByRole('region', { name: 'Elements' });

    for (const label of ['Straight', 'Curve', 'Turnout', 'Detector', 'Ks Signal', 'Fit to view']) {
      const button = within(toolbox).getByRole('button', { name: label });
      expect(button).toHaveAttribute('aria-disabled', 'true');
      expect(button).toHaveAccessibleDescription(hint);
    }
    expect(within(toolbox).getByText('Grid: 20 × 12 (60 px)')).toBeVisible();
  });

  it('renders the 20x12 SVG schematic with turnouts, detector, selected Ks signal and inert canvas controls', () => {
    const { container } = render(<SignalBoxPage />);
    const canvas = screen.getByRole('region', { name: 'Signal box canvas' });
    const svg = canvas.querySelector('svg.signal-box-svg');
    expect(svg).not.toBeNull();
    expect(svg).toHaveAttribute('viewBox', '0 0 1200 720');

    for (const element of signalBox.elements) {
      expect(svg!.querySelector(`[data-element-id="${element.id}"]`)).not.toBeNull();
    }

    expect(svg!.textContent).toContain('W1');
    expect(svg!.textContent).toContain('W2');
    expect(svg!.textContent).toContain('Block 1');
    expect(svg!.textContent).toContain('G2HBFA1');
    expect(svg!.textContent).toContain('Ks 1');

    const w1 = within(canvas).getByRole('button', { name: 'Turnout W1, straight' });
    const w2 = within(canvas).getByRole('button', { name: 'Turnout W2, diverging' });
    const detector = within(canvas).getByRole('button', { name: 'Detector Block 1' });
    const signal = within(canvas).getByRole('button', { name: 'Ks signal G2HBFA1, selected, Ks 1' });

    for (const control of [w1, w2, detector, signal]) {
      expect(control).toHaveAttribute('aria-disabled', 'true');
      expect(control).toHaveAccessibleDescription(hint);
    }
    expect(signal).toHaveAttribute('aria-pressed', 'true');
    expect(container.querySelector('[data-element-id="e12"][data-selected="true"]')).not.toBeNull();
  });

  it('renders aspect tiles with code, SVG light arrangement, prepared Ks 1 state, and read-only multiplexer config', () => {
    render(<SignalBoxPage />);
    const context = screen.getByRole('region', { name: 'Context' });

    expect(within(context).getByText('G2HBFA1')).toBeVisible();
    expect(within(context).getByText('System: Ks')).toBeVisible();

    for (const aspect of signalBox.signal.aspects) {
      const tile = within(context).getByRole('button', { name: new RegExp(aspect.name) });
      expect(tile).toHaveAttribute('aria-disabled', 'true');
      expect(tile).toHaveAccessibleDescription(hint);
      expect(tile).toHaveAttribute('aria-pressed', String(aspect.id === signalBox.signal.preparedAspect));
      expect(tile.querySelector('svg.signal-aspect-glyph')).not.toBeNull();
      expect(within(tile).getByText(aspect.name)).toBeVisible();
      expect(within(tile).getByText(aspect.description)).toBeVisible();
    }

    const ks1Tile = within(context).getByRole('button', { name: /Ks 1/ });
    expect(within(ks1Tile).getByText('Prepared')).toBeVisible();

    const mux = within(context).getByLabelText('Multiplexer configuration');
    expect(within(mux).getByText('5229')).toBeVisible();
    expect(within(mux).getByText('201')).toBeVisible();
    expect(within(mux).getByText('Main signal')).toBeVisible();
    expect(within(mux).getByText('4046')).toBeVisible();
    expect(within(mux).getByText('Distant signal')).toBeVisible();
    expect(within(mux).getByText('4040')).toBeVisible();
  });

  it('renders the element count footer without host details', () => {
    render(<SignalBoxPage />);
    const summary = screen.getByRole('region', { name: 'Signal box summary' });
    expect(summary).toHaveTextContent('12 elements · Signal G2HBFA1 (Ks 1)');
  });

  it('leaves the page unchanged when clicking inert toolbox, canvas, toolbar and aspect controls (SC-004)', () => {
    const { container } = render(<SignalBoxPage />);
    const before = stripHints(container);

    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThanOrEqual(20);
    for (const button of buttons) {
      fireEvent.click(button);
    }

    expect(stripHints(container)).toBe(before);
    const context = screen.getByRole('region', { name: 'Context' });
    expect(within(context).getByRole('button', { name: /Ks 1/ })).toHaveAttribute('aria-pressed', 'true');
  });

  it('shows steady Flashing text under prefers-reduced-motion: reduce and disables CSS animation (FR-046)', () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      })),
    );

    const { container } = render(<SignalBoxPage />);
    expect(container.querySelector('.signal-box')).toHaveAttribute('data-reduced-motion', 'reduce');
    const ks2Tile = screen.getByRole('button', { name: /Ks 2/ });
    expect(within(ks2Tile).getByText('Flashing')).toBeVisible();

    const css = readFileSync(join(import.meta.dirname, 'signal-box.css'), 'utf8');
    expect(css).toMatch(/@media\s*\(prefers-reduced-motion:\s*reduce\)\s*\{[\s\S]*animation:\s*none;/);
  });

  it('uses zero JSX style= attributes across Signal Box components (FR-071)', () => {
    for (const file of ['SignalBoxPage.tsx', 'SignalAspectGlyph.tsx']) {
      const source = readFileSync(join(import.meta.dirname, file), 'utf8');
      expect(source, file).not.toMatch(/\bstyle\s*=/);
    }
  });
});
