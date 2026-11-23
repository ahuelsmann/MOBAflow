import locomotivesJson from './locomotives.json';
import signalBoxJson from './signal-box.json';
import textsJson from './texts.json';
import timetableJson from './timetable.json';
import workflowJson from './workflow.json';
import { examplePhotos } from './photos';
import type {
  ExampleData,
  LocomotivesFile,
  RawExamples,
  ResolvedLocomotive,
  SignalBoxFile,
  TextsFile,
  TimetableFile,
  WorkflowFile,
} from './types';

/** A broken example file; the message names the file so the failed-load state of the app can show it. */
export class ExampleDataError extends Error {
  constructor(file: string, problem: string) {
    super(`${file}: ${problem}`);
    this.name = 'ExampleDataError';
  }
}

/** The committed example files, as imported. */
export const defaultSources: RawExamples = {
  locomotives: locomotivesJson,
  timetable: timetableJson,
  workflow: workflowJson,
  signalBox: signalBoxJson,
  texts: textsJson,
};

const FUNCTION_COUNT = 32;
const TIME_PATTERN = /^\d{2}:\d{2}$/;

type Check = (condition: boolean, problem: string) => asserts condition;

function checker(file: string): Check {
  return (condition, problem) => {
    if (!condition) throw new ExampleDataError(file, problem);
  };
}

type Obj = Record<string, unknown>;

function isObject(value: unknown): value is Obj {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isText(value: unknown): value is string {
  return typeof value === 'string';
}

function isNonEmptyText(value: unknown): value is string {
  return typeof value === 'string' && value.trim() !== '';
}

function isNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value);
}

function isNumberIn(value: unknown, min: number, max: number): boolean {
  return isNumber(value) && value >= min && value <= max;
}

function objectOf(check: Check, value: unknown, where: string): Obj {
  check(isObject(value), `${where} must be an object`);
  return value;
}

function arrayOf(check: Check, value: unknown, where: string): unknown[] {
  check(Array.isArray(value), `${where} must be a list`);
  return value;
}

function parseLocomotives(raw: unknown, photos: Readonly<Record<string, string>>): ResolvedLocomotive[] {
  const check: Check = checker('locomotives.json');
  const list = arrayOf(check, objectOf(check, raw, 'the file')['locomotives'], 'locomotives');
  check(list.length > 0, 'locomotives must not be empty');

  const locomotives = list.map((item, index) => {
    const where = `locomotives[${index}]`;
    const locomotive = objectOf(check, item, where) as unknown as Obj;
    check(isNonEmptyText(locomotive['id']), `${where}.id is missing`);
    check(isNonEmptyText(locomotive['name']), `${where}.name is missing`);
    check(isNonEmptyText(locomotive['series']), `${where}.series is missing`);
    check(isNumber(locomotive['address']), `${where}.address must be a number`);
    check(typeof locomotive['selected'] === 'boolean', `${where}.selected must be true or false`);

    const photo = locomotive['photo'];
    check(isText(photo) && photo in photos, `${where}.photo "${String(photo)}" is not an imported photo`);

    const functions = arrayOf(check, locomotive['functions'], `${where}.functions`).map((fn, fnIndex) => {
      const fnWhere = `${where}.functions[${fnIndex}]`;
      const entry = objectOf(check, fn, fnWhere);
      check(isNumber(entry['number']), `${fnWhere}.number must be a number`);
      check(isNonEmptyText(entry['icon']), `${fnWhere}.icon is missing`);
      check(isText(entry['name']), `${fnWhere}.name must be text`);
      check(typeof entry['pressed'] === 'boolean', `${fnWhere}.pressed must be true or false`);
      return entry;
    });
    const numbers = functions.map((fn) => fn['number'] as number).sort((a, b) => a - b);
    check(
      numbers.length === FUNCTION_COUNT && numbers.every((number, position) => number === position),
      `${where}.functions must be exactly F0 to F31 without duplicates`,
    );

    const prepared = objectOf(check, locomotive['prepared'], `${where}.prepared`);
    check(prepared['direction'] === 'forward' || prepared['direction'] === 'reverse', `${where}.prepared.direction is invalid`);
    check([14, 28, 126].includes(prepared['speedSteps'] as number), `${where}.prepared.speedSteps must be 14, 28 or 126`);
    check(isNumberIn(prepared['speed'], 0, 126), `${where}.prepared.speed must be 0 to 126`);
    check(isNumberIn(prepared['speedKmh'], 0, 400), `${where}.prepared.speedKmh must be 0 to 400`);
    for (const flag of ['accelerationRamp', 'brake', 'doorRelease', 'doorBlocking']) {
      check(typeof prepared[flag] === 'boolean', `${where}.prepared.${flag} must be true or false`);
    }
    for (const value of ['currentAmps', 'maxCurrentAmps', 'trackVoltage']) {
      check(isNumberIn(prepared[value], 0, Number.MAX_SAFE_INTEGER), `${where}.prepared.${value} must be a number`);
    }

    return { ...(locomotive as unknown as LocomotivesFile['locomotives'][number]), photoUrl: photos[photo] };
  });

  check(locomotives.filter((locomotive) => locomotive.selected).length === 1, 'exactly one locomotive must be selected');
  const selected = locomotives.find((locomotive) => locomotive.selected);
  check(
    selected !== undefined && selected.functions.every((fn) => isNonEmptyText(fn.name)),
    'the selected locomotive must name all its functions',
  );
  check(new Set(locomotives.map((locomotive) => locomotive.id)).size === locomotives.length, 'locomotive ids must be unique');
  return locomotives;
}

function parseTimetable(raw: unknown): TimetableFile {
  const check: Check = checker('timetable.json');
  const file = objectOf(check, raw, 'the file');
  check(isNonEmptyText(file['trainName']), 'trainName is missing');
  const stops = arrayOf(check, file['stops'], 'stops');
  check(stops.length >= 2, 'stops needs at least two entries');
  stops.forEach((stop, index) => {
    const where = `stops[${index}]`;
    const entry = objectOf(check, stop, where);
    check(isNonEmptyText(entry['station']), `${where}.station is missing`);
    check(isNonEmptyText(entry['platform']), `${where}.platform is missing`);
    for (const key of ['arrival', 'departure']) {
      const time = entry[key];
      check(time === undefined || (isText(time) && TIME_PATTERN.test(time)), `${where}.${key} must be HH:MM`);
    }
    check(entry['arrival'] !== undefined || entry['departure'] !== undefined, `${where} needs a time`);
  });
  const current = file['currentStopIndex'];
  check(isNumber(current) && Number.isInteger(current) && current >= 0 && current < stops.length, 'currentStopIndex is outside stops');
  return file as unknown as TimetableFile;
}

function parseWorkflow(raw: unknown): WorkflowFile {
  const check: Check = checker('workflow.json');
  const file = objectOf(check, raw, 'the file');
  const list = arrayOf(check, file['list'], 'list').map((entry, index) => {
    const item = objectOf(check, entry, `list[${index}]`);
    check(isNonEmptyText(item['id']) && isNonEmptyText(item['name']), `list[${index}] needs id and name`);
    return item;
  });
  check(list.filter((entry) => entry['id'] === file['selectedId']).length === 1, 'selectedId must name exactly one list entry');
  check(isText(file['description']), 'description must be text');

  const nodes = arrayOf(check, file['nodes'], 'nodes').map((node, index) => {
    const where = `nodes[${index}]`;
    const entry = objectOf(check, node, where);
    check(isNonEmptyText(entry['id']), `${where}.id is missing`);
    check(isNonEmptyText(entry['name']), `${where}.name is missing`);
    check(['trigger', 'action', 'terminate'].includes(entry['type'] as string), `${where}.type is invalid`);
    check(isText(entry['summary']), `${where}.summary must be text`);
    check(isNumber(entry['step']), `${where}.step must be a number`);
    check(entry['state'] === 'idle' || entry['state'] === 'completed', `${where}.state is invalid`);
    const position = objectOf(check, entry['position'], `${where}.position`);
    check(isNumber(position['x']) && isNumber(position['y']), `${where}.position needs numeric x and y`);
    return entry;
  });
  const ids = new Set(nodes.map((node) => node['id']));
  check(ids.size === nodes.length, 'node ids must be unique');
  check(nodes.filter((node) => node['type'] === 'trigger').length === 1, 'exactly one trigger node is required');
  for (const [index, edge] of arrayOf(check, file['edges'], 'edges').entries()) {
    const item = objectOf(check, edge, `edges[${index}]`);
    check(ids.has(item['from']) && ids.has(item['to']), `edges[${index}] refers to an unknown node`);
  }
  for (const [index, property] of arrayOf(check, file['properties'], 'properties').entries()) {
    const item = objectOf(check, property, `properties[${index}]`);
    check(isNonEmptyText(item['label']) && isText(item['value']), `properties[${index}] needs label and value`);
  }
  return file as unknown as WorkflowFile;
}

const ELEMENT_TYPES = ['TrackStraight', 'TrackCurve', 'Turnout', 'Detector', 'Signal'];

function parseSignalBox(raw: unknown): SignalBoxFile {
  const check: Check = checker('signal-box.json');
  const file = objectOf(check, raw, 'the file');
  const grid = objectOf(check, file['grid'], 'grid');
  check(isNumber(grid['width']) && isNumber(grid['height']) && isNumber(grid['cellSize']), 'grid needs width, height and cellSize');

  const elements = arrayOf(check, file['elements'], 'elements').map((element, index) => {
    const where = `elements[${index}]`;
    const entry = objectOf(check, element, where);
    check(isNonEmptyText(entry['id']), `${where}.id is missing`);
    check(ELEMENT_TYPES.includes(entry['type'] as string), `${where}.type is invalid`);
    check(isNumber(entry['x']) && isNumber(entry['y']) && isNumber(entry['rotation']), `${where} needs x, y and rotation`);
    check(isText(entry['name']), `${where}.name must be text`);
    if (entry['type'] === 'Turnout') {
      check(entry['position'] === 'straight' || entry['position'] === 'diverging', `${where}.position is invalid`);
    }
    return entry;
  });
  check(new Set(elements.map((element) => element['id'])).size === elements.length, 'element ids must be unique');
  check(elements.some((element) => element['type'] === 'Turnout'), 'at least one turnout is required');

  const signal = objectOf(check, file['signal'], 'signal');
  const signalElement = elements.find((element) => element['id'] === signal['elementId']);
  check(signalElement?.['type'] === 'Signal', 'signal.elementId must name a Signal element');
  check(signal['system'] === 'Ks', 'signal.system must be Ks');
  const aspects: unknown[] = arrayOf(check, signal['aspects'], 'signal.aspects').map((aspect, index) => {
    const entry = objectOf(check, aspect, `signal.aspects[${index}]`);
    check(isNonEmptyText(entry['id']) && isNonEmptyText(entry['name']) && isText(entry['description']), `signal.aspects[${index}] needs id, name and description`);
    return entry['id'];
  });
  check(new Set(aspects).size === aspects.length, 'signal.aspects must be unique');
  check(aspects.includes(signal['preparedAspect']), 'signal.preparedAspect must be one of the signal aspects');
  const multiplexer = objectOf(check, signal['multiplexer'], 'signal.multiplexer');
  check(isNonEmptyText(multiplexer['articleNumber']) && isNumber(multiplexer['baseAddress']), 'signal.multiplexer needs articleNumber and baseAddress');
  arrayOf(check, multiplexer['outputs'], 'signal.multiplexer.outputs');
  return file as unknown as SignalBoxFile;
}

function parseTexts(raw: unknown): TextsFile {
  const check: Check = checker('texts.json');
  const file = objectOf(check, raw, 'the file');
  const statusBar = objectOf(check, file['statusBar'], 'statusBar');
  for (const key of ['solution', 'z21', 'trackPowerLabel', 'restApi', 'speech']) {
    check(isNonEmptyText(statusBar[key]), `statusBar.${key} is missing`);
  }
  check(typeof statusBar['trackPowerOn'] === 'boolean', 'statusBar.trackPowerOn must be true or false');
  const message = objectOf(check, file['firstVisitMessage'], 'firstVisitMessage');
  check(message['severity'] === 'info' && isNonEmptyText(message['text']), 'firstVisitMessage needs severity info and text');
  check(isNonEmptyText(file['inertHint']) && isNonEmptyText(file['fixedProjectHint']), 'the hints are missing');
  return file as unknown as TextsFile;
}

/** Validates the five raw files and resolves the photo keys; throws an {@link ExampleDataError} naming the broken file. */
export function parseExamples(raw: RawExamples, photos: Readonly<Record<string, string>> = examplePhotos): ExampleData {
  return {
    locomotives: parseLocomotives(raw.locomotives, photos),
    timetable: parseTimetable(raw.timetable),
    workflow: parseWorkflow(raw.workflow),
    signalBox: parseSignalBox(raw.signalBox),
    texts: parseTexts(raw.texts),
  };
}

/** Loads the committed example data; a malformed file rejects, which the app shows as its failed-load state. */
export async function loadExamples(sources: RawExamples = defaultSources): Promise<ExampleData> {
  return parseExamples(sources);
}
