import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { defaultSources, parseExamples } from '../../../examples/loader';
import { CabControlTiles } from './CabControlTiles';
import { CurrentGauge } from './CurrentGauge';
import { SpeedometerGauge } from './SpeedometerGauge';
import { TimetablePanel } from './TimetablePanel';

const examples = parseExamples(defaultSources);
const flagship = examples.locomotives.find((locomotive) => locomotive.selected)!;
const hint = 'Available in the MOBAflow desktop app';

describe('SpeedometerGauge (FR-036, FR-043)', () => {
  it('renders the 0 to 400 km/h scale and exposes the prepared speed as accessible text without inline styles', () => {
    const { container } = render(<SpeedometerGauge speedKmh={flagship.prepared.speedKmh} />);
    expect(screen.getByText('82')).toBeVisible();
    expect(screen.getByText('km/h')).toBeVisible();
    expect(screen.getByText('Scale: 0 to 400 km/h')).toBeVisible();
    for (const tick of ['0', '100', '200', '300', '400']) {
      expect(container.querySelector('svg')).toHaveTextContent(tick);
    }
    const needle = container.querySelector('.tc-gauge-needle');
    expect(needle).toHaveAttribute('transform', 'rotate(-53.1 130 120)');
    expect(container.innerHTML).not.toMatch(/\sstyle=/i);
  });
});

describe('CurrentGauge (FR-036, FR-043)', () => {
  it('exposes prepared current, limit, and track voltage as accessible text without inline styles', () => {
    const { container } = render(
      <CurrentGauge
        currentAmps={flagship.prepared.currentAmps}
        maxCurrentAmps={flagship.prepared.maxCurrentAmps}
        trackVoltage={flagship.prepared.trackVoltage}
      />,
    );
    const region = screen.getByRole('region', { name: 'Track current' });
    expect(within(region).getByText('0.42 A')).toBeVisible();
    expect(within(region).getByText('/ 3.0 A')).toBeVisible();
    expect(within(region).getByText('Track voltage: 18.1 V')).toBeVisible();
    expect(container.innerHTML).not.toMatch(/\sstyle=/i);
  });
});

describe('CabControlTiles (FR-036, FR-044, FR-045)', () => {
  it('exposes Brake and Door release pressed states and Door blocking as a non-toggle status with icon and text', () => {
    const { container } = render(
      <CabControlTiles
        brake={flagship.prepared.brake}
        doorRelease={flagship.prepared.doorRelease}
        doorBlocking={flagship.prepared.doorBlocking}
      />,
    );
    const region = screen.getByRole('region', { name: 'Cab controls' });
    const buttons = within(region).getAllByRole('button');
    expect(buttons).toHaveLength(2);

    const brake = within(region).getByRole('button', { name: /Brake/ });
    const doorRelease = within(region).getByRole('button', { name: /Door release/ });
    expect(brake).toHaveAttribute('aria-pressed', 'false');
    expect(brake).toHaveAttribute('aria-disabled', 'true');
    expect(brake).toHaveAccessibleDescription(hint);
    expect(brake).toHaveTextContent('Released');

    expect(doorRelease).toHaveAttribute('aria-pressed', 'false');
    expect(doorRelease).toHaveAttribute('aria-disabled', 'true');
    expect(doorRelease).toHaveAccessibleDescription(hint);
    expect(doorRelease).toHaveTextContent('Locked');

    fireEvent.click(brake);
    fireEvent.click(doorRelease);
    expect(brake).toHaveAttribute('aria-pressed', 'false');
    expect(doorRelease).toHaveAttribute('aria-pressed', 'false');

    const status = container.querySelector('.tc-cab-status');
    expect(status).not.toBeNull();
    expect(status).toHaveAttribute('data-active', 'true');
    expect(status).toHaveTextContent('Door blocking');
    expect(status).toHaveTextContent('Active');
    expect(status?.querySelector('svg')).not.toBeNull();
    expect(status?.closest('button')).toBeNull();
  });

  it('reflects active Brake/Door release and inactive Door blocking states', () => {
    const { container } = render(<CabControlTiles brake doorRelease doorBlocking={false} />);
    expect(screen.getByRole('button', { name: /Brake/ })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: /Brake/ })).toHaveTextContent('Applied');
    expect(screen.getByRole('button', { name: /Door release/ })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: /Door release/ })).toHaveTextContent('Released');
    const status = container.querySelector('.tc-cab-status');
    expect(status).toHaveAttribute('data-active', 'false');
    expect(status).toHaveTextContent('Inactive');
  });
});

describe('TimetablePanel (FR-036, FR-045)', () => {
  it('renders the train name, stops, and non-color current-stop indicator', () => {
    render(<TimetablePanel timetable={examples.timetable} />);
    const region = screen.getByRole('region', { name: 'Timetable' });
    expect(within(region).getByText('Regional service RB 21')).toBeVisible();

    const items = within(region).getAllByRole('listitem');
    expect(items).toHaveLength(examples.timetable.stops.length);

    const currentItem = items[examples.timetable.currentStopIndex]!;
    expect(currentItem).toHaveAttribute('aria-current', 'step');
    expect(currentItem).toHaveAttribute('data-state', 'current');
    expect(currentItem).toHaveTextContent('Riverside');
    expect(currentItem).toHaveTextContent('Platform 2');
    expect(currentItem).toHaveTextContent('Arr 10:14');
    expect(currentItem).toHaveTextContent('Dep 10:15');
    expect(currentItem).toHaveTextContent('Current stop');
    expect(currentItem.querySelector('svg')).not.toBeNull();

    expect(items[0]).toHaveAttribute('data-state', 'past');
    expect(items[0]).toHaveTextContent('Departed');
    expect(items[2]).toHaveAttribute('data-state', 'upcoming');
    expect(items[2]).toHaveTextContent('Upcoming');
  });
});

describe('reduced motion (FR-046)', () => {
  it('disables animations and transitions under prefers-reduced-motion: reduce in gauges.css', () => {
    const css = readFileSync(join(import.meta.dirname, 'gauges.css'), 'utf8');
    const reducedBlock = /@media\s*\(\s*prefers-reduced-motion:\s*reduce\s*\)\s*\{([\s\S]*?\})\s*\}/.exec(css);
    expect(reducedBlock).not.toBeNull();
    expect(reducedBlock![1]).toContain('animation: none;');
    expect(reducedBlock![1]).toContain('transition: none;');
    expect(reducedBlock![1]).toContain('.tc-gauge-needle');
    expect(reducedBlock![1]).toContain('.tc-cab-tile');
    expect(reducedBlock![1]).toContain('.train-control button');
  });
});
