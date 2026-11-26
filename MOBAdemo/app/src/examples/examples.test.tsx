import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { App } from '../App';
import { ExampleDataError, defaultSources, loadExamples, parseExamples } from './loader';
import { examplePhotos } from './photos';
import type { RawExamples } from './types';

const PHOTO_LIMIT_BYTES = 60 * 1024;
const FIRST_VISIT_PHOTO_LIMIT = 3;

function freshExamples() {
  return parseExamples(structuredClone(defaultSources) as RawExamples);
}

type Path = (string | number)[];

/** Copies the committed sources and sets (or, with undefined, removes) the value at a path. */
function changed(path: Path, value: unknown): RawExamples {
  const copy = structuredClone(defaultSources) as unknown as Record<string, unknown>;
  let target = copy;
  for (const key of path.slice(0, -1)) {
    target = target[key] as Record<string, unknown>;
  }
  const last = String(path[path.length - 1]);
  if (value === undefined) {
    delete target[last];
  } else {
    target[last] = value;
  }
  return copy as unknown as RawExamples;
}

describe('example locomotives (FR-036)', () => {
  it('has exactly one selected locomotive', () => {
    const selected = freshExamples().locomotives.filter((locomotive) => locomotive.selected);
    expect(selected).toHaveLength(1);
  });

  it('gives every locomotive exactly the functions F0 to F31 without duplicates', () => {
    const expected = Array.from({ length: 32 }, (_, number) => number);
    for (const locomotive of freshExamples().locomotives) {
      const numbers = locomotive.functions.map((fn) => fn.number).sort((a, b) => a - b);
      expect(numbers, locomotive.name).toEqual(expected);
    }
  });

  it('names every function of the selected locomotive and gives each an icon', () => {
    const selected = freshExamples().locomotives.find((locomotive) => locomotive.selected);
    expect(selected).toBeDefined();
    for (const fn of selected!.functions) {
      expect(fn.name.trim(), `F${fn.number}`).not.toBe('');
      expect(fn.icon.trim(), `F${fn.number}`).not.toBe('');
    }
  });

  it('prepares at least one pressed function on the selected locomotive', () => {
    const selected = freshExamples().locomotives.find((locomotive) => locomotive.selected);
    expect(selected!.functions.some((fn) => fn.pressed)).toBe(true);
  });

  it('references a photo for every locomotive that resolves through the imported modules', () => {
    for (const locomotive of freshExamples().locomotives) {
      expect(locomotive.photoUrl, locomotive.name).toBe(examplePhotos[locomotive.photo]);
      expect(locomotive.photoUrl).toBeTruthy();
    }
  });

  it('rejects two selected locomotives', () => {
    const raw = changed(['locomotives', 'locomotives', 1, 'selected'], true);
    expect(() => parseExamples(raw)).toThrow(/locomotives\.json/);
  });

  it('rejects a duplicated function number', () => {
    const raw = changed(['locomotives', 'locomotives', 0, 'functions', 5, 'number'], 4);
    expect(() => parseExamples(raw)).toThrow(/locomotives\.json/);
  });

  it.each([0, 10240, 1.5])('rejects the DCC address %s', (address) => {
    const raw = changed(['locomotives', 'locomotives', 0, 'address'], address);
    expect(() => parseExamples(raw)).toThrow(/locomotives\.json/);
  });

  it('rejects a photo key inherited from Object.prototype', () => {
    const raw = changed(['locomotives', 'locomotives', 0, 'photo'], 'toString');
    expect(() => parseExamples(raw)).toThrow(/locomotives\.json/);
  });

  it('rejects a photo whose URL is not a non-empty string', () => {
    expect(() => parseExamples(structuredClone(defaultSources) as RawExamples, Object.fromEntries(Object.keys(examplePhotos).map((key) => [key, ''])))).toThrow(/locomotives\.json/);
  });

  it('rejects a selected locomotive with an unnamed function', () => {
    const selectedIndex = freshExamples().locomotives.findIndex((locomotive) => locomotive.selected);
    const raw = changed(['locomotives', 'locomotives', selectedIndex, 'functions', 3, 'name'], '');
    expect(() => parseExamples(raw)).toThrow(/locomotives\.json/);
  });

  it('rejects a photo that is not an imported module', () => {
    const raw = changed(['locomotives', 'locomotives', 0, 'photo'], 'missing-photo');
    expect(() => parseExamples(raw)).toThrow(/locomotives\.json/);
  });
});

describe('example timetable (FR-036)', () => {
  it('has stops with times and a current stop inside the list', () => {
    const { timetable } = freshExamples();
    expect(timetable.stops.length).toBeGreaterThanOrEqual(3);
    expect(timetable.currentStopIndex).toBeGreaterThanOrEqual(0);
    expect(timetable.currentStopIndex).toBeLessThan(timetable.stops.length);
    for (const stop of timetable.stops) {
      expect(stop.departure ?? stop.arrival).toMatch(/^\d{2}:\d{2}$/);
    }
  });

  it('rejects a current stop outside the list', () => {
    const raw = changed(['timetable', 'currentStopIndex'], 99);
    expect(() => parseExamples(raw)).toThrow(/timetable\.json/);
  });
});

describe('example workflow (FR-037)', () => {
  it('gives every workflow node a position', () => {
    for (const node of freshExamples().workflow.nodes) {
      expect(Number.isFinite(node.position.x), node.id).toBe(true);
      expect(Number.isFinite(node.position.y), node.id).toBe(true);
    }
  });

  it('connects the trigger node, directly or transitively, to every other node and selects the example workflow', () => {
    const { workflow } = freshExamples();
    const nodeIds = new Set(workflow.nodes.map((node) => node.id));
    expect(workflow.nodes.filter((node) => node.type === 'trigger')).toHaveLength(1);
    const trigger = workflow.nodes.find((node) => node.type === 'trigger')!;
    const reached = new Set([trigger.id]);
    for (let grew = true; grew; ) {
      grew = false;
      for (const edge of workflow.edges) {
        if (reached.has(edge.from) && !reached.has(edge.to)) {
          reached.add(edge.to);
          grew = true;
        }
      }
    }
    expect(reached).toEqual(nodeIds);
    for (const edge of workflow.edges) {
      expect(nodeIds.has(edge.from)).toBe(true);
      expect(nodeIds.has(edge.to)).toBe(true);
    }
    expect(workflow.list.filter((entry) => entry.id === workflow.selectedId)).toHaveLength(1);
  });

  it('rejects a workflow without edges', () => {
    const raw = changed(['workflow', 'edges'], []);
    expect(() => parseExamples(raw)).toThrow(/workflow\.json/);
  });

  it('rejects an edge to an unknown node', () => {
    const raw = changed(['workflow', 'edges', 0, 'to'], 'no-such-node');
    expect(() => parseExamples(raw)).toThrow(/workflow\.json/);
  });

  it('rejects a node the trigger does not reach', () => {
    const raw = changed(['workflow', 'edges', 4, 'from'], 'n5');
    expect(() => parseExamples(raw)).toThrow(/workflow\.json/);
  });

  it('rejects a node without a position', () => {
    const raw = changed(['workflow', 'nodes', 1, 'position'], undefined);
    expect(() => parseExamples(raw)).toThrow(/workflow\.json/);
  });
});

describe('example signal box (FR-039)', () => {
  it('has turnouts and a Ks signal whose prepared aspect is one of its aspects', () => {
    const { signalBox } = freshExamples();
    expect(signalBox.elements.some((element) => element.type === 'Turnout')).toBe(true);
    const signalElement = signalBox.elements.find((element) => element.id === signalBox.signal.elementId);
    expect(signalElement?.type).toBe('Signal');
    const aspects = signalBox.signal.aspects.map((aspect) => aspect.id);
    expect(aspects).toContain(signalBox.signal.preparedAspect);
  });

  it('rejects a multiplexer output that is not an object', () => {
    const raw = changed(['signalBox', 'signal', 'multiplexer', 'outputs'], [null]);
    expect(() => parseExamples(raw)).toThrow(/signal-box\.json/);
  });

  it('rejects a multiplexer output with a numeric label', () => {
    const raw = changed(['signalBox', 'signal', 'multiplexer', 'outputs'], [{ label: 5, value: 'x' }]);
    expect(() => parseExamples(raw)).toThrow(/signal-box\.json/);
  });

  it('rejects a prepared aspect that the signal does not have', () => {
    const raw = changed(['signalBox', 'signal', 'preparedAspect'], 'NotAnAspect');
    expect(() => parseExamples(raw)).toThrow(/signal-box\.json/);
  });
});

describe('example locomotive images (FR-010, FR-058)', () => {
  // Vitest runs from the app folder (the folder of package.json).
  const photoDirectory = join(process.cwd(), 'src', 'assets', 'photos');
  const files = readdirSync(photoDirectory);

  it('keeps every image file at 60 KB or less', () => {
    expect(files.length).toBeGreaterThan(0);
    for (const file of files) {
      expect(statSync(join(photoDirectory, file)).size, file).toBeLessThanOrEqual(PHOTO_LIMIT_BYTES);
    }
  });

  it('references at most three images on the first visit', () => {
    const referenced = new Set(freshExamples().locomotives.map((locomotive) => locomotive.photoUrl));
    expect(referenced.size).toBeGreaterThan(0);
    expect(referenced.size).toBeLessThanOrEqual(FIRST_VISIT_PHOTO_LIMIT);
    expect(files.length).toBeLessThanOrEqual(FIRST_VISIT_PHOTO_LIMIT);
  });

  it('uses only the self-drawn SVG placeholders, no raster image', () => {
    expect([...files].sort()).toEqual(['placeholder-diesel.svg', 'placeholder-electric.svg']);
    for (const locomotive of freshExamples().locomotives) {
      expect(locomotive.photoUrl, locomotive.name).toMatch(/placeholder-(diesel|electric)[^/]*\.svg$/);
    }
  });

  it('carries no style attribute, style element or script in the SVGs (style-src self)', () => {
    for (const file of files) {
      const svg = readFileSync(join(photoDirectory, file), 'utf-8');
      expect(svg, file).not.toMatch(/\sstyle\s*=|<style|<script|<image|data:/i);
    }
  });

});

describe('example texts (FR-029, FR-030, FR-031)', () => {
  const hostDetail: Record<string, RegExp> = {
    'Windows path': /[A-Za-z]:\\|\\\\/,
    'Unix path': /(^|\s)\/(home|users|var|etc|usr|mnt|tmp)\//i,
    'IPv4 address': /\b\d{1,3}(\.\d{1,3}){3}\b/,
    port: /(localhost|\bhost|\bip|:)\s?:?\d{3,5}\b/i,
    localhost: /localhost/i,
    'machine name': /\b(DESKTOP|LAPTOP|WORKSTATION)-[A-Z0-9]+\b/i,
    'user directory': /\\Users\\|\/Users\/|\/home\//i,
  };

  function allStrings(value: unknown): string[] {
    if (typeof value === 'string') return [value];
    if (Array.isArray(value)) return value.flatMap(allStrings);
    if (value !== null && typeof value === 'object') return Object.values(value).flatMap(allStrings);
    return [];
  }

  it('shows the four status bar segments and the first-visit message from the spec', () => {
    const { texts } = freshExamples();
    expect(texts.statusBar.solution).toBe('Solution: MOBAflow demo layout');
    expect(texts.statusBar.z21).toBe('Z21: Not connected (web demo)');
    expect(texts.statusBar.restApi).toBe('REST API: Not available in the web demo');
    expect(texts.statusBar.speech).toBe('Speech: Not available in the web demo');
    expect(texts.firstVisitMessage.severity).toBe('info');
    expect(texts.firstVisitMessage.text).toMatch(/^Visual web demo:/);
    expect(texts.inertHint).toBe('Available in the MOBAflow desktop app');
  });

  it.each(Object.entries(hostDetail))('contains no %s in any example file', (_name, pattern) => {
    for (const [file, content] of Object.entries(defaultSources)) {
      for (const text of allStrings(content)) {
        expect(text, `${file}: ${text}`).not.toMatch(pattern);
      }
    }
  });

  it('would catch a host detail (the check itself works)', () => {
    expect('C:\\Users\\someone\\solution.json').toMatch(hostDetail['Windows path']);
    expect('REST API: 192.168.0.10:5001').toMatch(hostDetail['IPv4 address']);
    expect('listening on localhost:5001').toMatch(hostDetail.port);
  });

  it('rejects a status bar segment that is missing', () => {
    const raw = changed(['texts', 'statusBar', 'z21'], undefined);
    expect(() => parseExamples(raw)).toThrow(/texts\.json/);
  });
});

describe('example data loader (FR-052 failed-load state)', () => {
  it('resolves with the parsed data', async () => {
    const data = await loadExamples();
    expect(data.locomotives.length).toBeGreaterThan(0);
  });

  it('rejects a malformed file with the file name in the error', async () => {
    const raw = changed(['signalBox'], { grid: 'not a grid' });
    await expect(loadExamples(raw)).rejects.toBeInstanceOf(ExampleDataError);
    await expect(loadExamples(raw)).rejects.toThrow(/signal-box\.json/);
  });

  it('shows the visible error state of the app when a file is malformed', async () => {
    const raw = changed(['workflow'], 'not an object');
    render(<App load={async () => void (await loadExamples(raw))} />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('The demo data could not be loaded');
    expect(alert).not.toHaveTextContent('workflow.json');
    expect(screen.getByRole('button', { name: 'Warnings 1' })).toBeVisible();
  });
});
