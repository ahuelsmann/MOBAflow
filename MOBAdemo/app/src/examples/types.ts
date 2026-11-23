/** Types of the demo-authored example data files (spec FR-010). Not a MOBAflow file format. */

export type Direction = 'forward' | 'reverse';
export type DccSpeedSteps = 14 | 28 | 126;

export interface LocomotiveFunction {
  /** Function number, F0 to F31. */
  number: number;
  /** Symbol name from the MOBAflow function symbol set, without file extension. */
  icon: string;
  /** Display name; example data, so it may be German ("Stirnlicht"). Empty only on non-selected locomotives. */
  name: string;
  /** Prepared pressed state. */
  pressed: boolean;
}

export interface PreparedDriveState {
  direction: Direction;
  speedSteps: DccSpeedSteps;
  /** Slider position, 0 to 126. */
  speed: number;
  /** Speedometer value, 0 to 400 km/h. */
  speedKmh: number;
  accelerationRamp: boolean;
  brake: boolean;
  doorRelease: boolean;
  doorBlocking: boolean;
  currentAmps: number;
  maxCurrentAmps: number;
  trackVoltage: number;
}

export interface ExampleLocomotive {
  id: string;
  name: string;
  /** Series shown by the series search, for example "BR 211". */
  series: string;
  address: number;
  /** Key into the imported image modules (see photos.ts). */
  photo: string;
  selected: boolean;
  functions: LocomotiveFunction[];
  prepared: PreparedDriveState;
}

export interface LocomotivesFile {
  locomotives: ExampleLocomotive[];
}

export interface TimetableStop {
  station: string;
  platform: string;
  /** HH:MM, absent at the first stop. */
  arrival?: string;
  /** HH:MM, absent at the last stop. */
  departure?: string;
}

export interface TimetableFile {
  trainName: string;
  currentStopIndex: number;
  stops: TimetableStop[];
}

export type WorkflowNodeType = 'trigger' | 'action' | 'terminate';
export type WorkflowNodeState = 'idle' | 'completed';

export interface WorkflowNode {
  id: string;
  name: string;
  type: WorkflowNodeType;
  summary: string;
  /** Step number shown on the node. */
  step: number;
  state: WorkflowNodeState;
  position: { x: number; y: number };
}

export interface WorkflowProperty {
  label: string;
  value: string;
}

export interface WorkflowFile {
  list: { id: string; name: string }[];
  selectedId: string;
  description: string;
  nodes: WorkflowNode[];
  edges: { from: string; to: string }[];
  properties: WorkflowProperty[];
}

export type SignalBoxElementType = 'TrackStraight' | 'TrackCurve' | 'Turnout' | 'Detector' | 'Signal';

export interface SignalBoxElement {
  id: string;
  type: SignalBoxElementType;
  x: number;
  y: number;
  rotation: number;
  name: string;
  /** Turnouts only. */
  position?: 'straight' | 'diverging';
}

export interface SignalAspect {
  id: string;
  name: string;
  description: string;
}

export interface SignalBoxFile {
  grid: { width: number; height: number; cellSize: number };
  elements: SignalBoxElement[];
  signal: {
    elementId: string;
    system: 'Ks';
    aspects: SignalAspect[];
    preparedAspect: string;
    multiplexer: { articleNumber: string; baseAddress: number; outputs: { label: string; value: string }[] };
  };
}

export interface TextsFile {
  statusBar: {
    solution: string;
    z21: string;
    trackPowerLabel: string;
    trackPowerOn: boolean;
    restApi: string;
    speech: string;
  };
  firstVisitMessage: { severity: 'info'; text: string };
  inertHint: string;
  fixedProjectHint: string;
}

/** The five raw files, as imported. */
export interface RawExamples {
  locomotives: unknown;
  timetable: unknown;
  workflow: unknown;
  signalBox: unknown;
  texts: unknown;
}

export type ResolvedLocomotive = ExampleLocomotive & { photoUrl: string };

/** The validated data the pages use. */
export interface ExampleData {
  locomotives: ResolvedLocomotive[];
  timetable: TimetableFile;
  workflow: WorkflowFile;
  signalBox: SignalBoxFile;
  texts: TextsFile;
}
