import { describe, expect, it } from 'vitest';
import navigationJson from './navigation.json';
import type { NavigationFile } from './types';

const navigation: NavigationFile = navigationJson;
const entries = navigation.entries;

describe('navigation.json shape (the drift test in Test/MOBAdemo/Navigation compares it with the app)', () => {
  it('has unique keys', () => {
    expect(new Set(entries.map((entry) => entry.key)).size).toBe(entries.length);
  });

  it('marks exactly the four demo pages as available', () => {
    expect(
      entries
        .filter((entry) => entry.demoPage)
        .map((entry) => entry.key)
        .sort(),
    ).toEqual(['signalbox', 'trackplaneditor', 'traincontrol', 'workflows']);
  });

  it('has bold entries only for Train Control and Journeys', () => {
    expect(
      entries
        .filter((entry) => entry.isBold)
        .map((entry) => entry.key)
        .sort(),
    ).toEqual(['journeys', 'traincontrol']);
  });

  it('gives only Signal Box a path icon, and every other entry a font icon', () => {
    expect(entries.filter((entry) => entry.pathIconData !== null).map((entry) => entry.key)).toEqual([
      'signalbox',
    ]);
    for (const entry of entries) {
      expect(entry.icon === null, entry.key).toBe(entry.pathIconData !== null);
    }
  });

  it('lists the entries in the groups of the specification', () => {
    expect(entries.map((entry) => entry.title)).toEqual([
      'Overview',
      'Train Control',
      'Journeys',
      'Timetable',
      'Workflows',
      'Event Manager',
      'Journey Map',
      'Solution',
      'Stations',
      'Locomotives',
      'Passenger Wagons',
      'Goods Wagons',
      'Trains',
      'Track Plan',
      'Signal Box',
      'ESP32 Display',
      'Matrix Images',
      'Monitor',
      'Recorder',
      'Help',
      'Info',
      'Settings',
    ]);
  });
});
