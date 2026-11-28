import { ColumnBadge, ColumnPin } from '../../tokens/ColumnBadge';
import { useEffect, useMemo, useState } from 'react';
import { defaultSources, parseExamples } from '../../examples/loader';
import type { SignalBoxElement, SignalBoxElementType } from '../../examples/types';
import { InertButton } from '../../shell/inert/InertButton';
import { SignalAspectGlyph } from './SignalAspectGlyph';
import './signal-box.css';

interface ToolboxItem {
  type: SignalBoxElementType;
  label: string;
}

const TOOLBOX_ITEMS: readonly ToolboxItem[] = [
  { type: 'TrackStraight', label: 'Straight' },
  { type: 'TrackCurve', label: 'Curve' },
  { type: 'Turnout', label: 'Turnout' },
  { type: 'Detector', label: 'Detector' },
  { type: 'Signal', label: 'Ks Signal' },
];

const REDUCED_MOTION_QUERY = '(prefers-reduced-motion: reduce)';
const VIEW_COL_START = 0;
const VIEW_ROW_START = 2;
const VIEW_COLS = 10;
const VIEW_ROWS = 6;

function readReducedMotion(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return false;
  }
  return Boolean(window.matchMedia(REDUCED_MOTION_QUERY).matches);
}

function usePrefersReducedMotion(): boolean {
  const [reduced, setReduced] = useState(readReducedMotion);

  useEffect(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return undefined;
    }
    const mediaQuery = window.matchMedia(REDUCED_MOTION_QUERY);
    const update = () => setReduced(Boolean(mediaQuery.matches));
    update();

    if (typeof mediaQuery.addEventListener === 'function') {
      mediaQuery.addEventListener('change', update);
      return () => mediaQuery.removeEventListener('change', update);
    }
    return undefined;
  }, []);

  return reduced;
}

function ToolboxGlyph({ type }: Readonly<{ type: SignalBoxElementType }>) {
  switch (type) {
    case 'TrackStraight':
      return (
        <svg aria-hidden="true" className="signal-box-tool-icon" viewBox="0 0 48 28">
          <line className="signal-box-track" x1="4" y1="14" x2="44" y2="14" />
        </svg>
      );
    case 'TrackCurve':
      return (
        <svg aria-hidden="true" className="signal-box-tool-icon" viewBox="0 0 48 28">
          <path className="signal-box-track" d="M 6,14 Q 26,14 26,26" />
        </svg>
      );
    case 'Turnout':
      return (
        <svg aria-hidden="true" className="signal-box-tool-icon" viewBox="0 0 48 28">
          <line className="signal-box-track" x1="4" y1="14" x2="44" y2="14" />
          <path className="signal-box-turnout-diverging" d="M 20,14 Q 30,14 38,4" />
        </svg>
      );
    case 'Detector':
      return (
        <svg aria-hidden="true" className="signal-box-tool-icon" viewBox="0 0 48 28">
          <line className="signal-box-track" x1="4" y1="14" x2="44" y2="14" />
          <circle className="signal-box-detector-node" cx="24" cy="14" r="5" />
        </svg>
      );
    case 'Signal':
      return (
        <svg aria-hidden="true" className="signal-box-tool-icon" viewBox="0 0 48 28">
          <rect className="signal-glyph-housing" x="15" y="2" width="18" height="24" rx="3" />
          <circle className="signal-lamp signal-lamp-red" cx="21" cy="10" r="3" />
          <circle className="signal-lamp signal-lamp-green" cx="27" cy="18" r="3" />
        </svg>
      );
  }
}

function describeCanvasElement(
  element: SignalBoxElement,
  selectedSignalId: string,
  preparedAspectName: string,
): string {
  switch (element.type) {
    case 'Turnout':
      return `Turnout ${element.name}, ${element.position ?? 'straight'}`;
    case 'Detector':
      return `Detector ${element.name}`;
    case 'Signal': {
      const selected = element.id === selectedSignalId ? ', selected' : '';
      return `Ks signal ${element.name}${selected}, ${preparedAspectName}`;
    }
    case 'TrackCurve':
      return `Curved track at column ${element.x + 1}, row ${element.y + 1}`;
    case 'TrackStraight':
      return `Straight track at column ${element.x + 1}, row ${element.y + 1}`;
  }
}

function CanvasElementGraphic({
  element,
  cellSize,
  isSelectedSignal,
  preparedAspectId,
  preparedAspectName,
}: Readonly<{
  element: SignalBoxElement;
  cellSize: number;
  isSelectedSignal: boolean;
  preparedAspectId: string;
  preparedAspectName: string;
}>) {
  const half = cellSize / 2;
  const tx = element.x * cellSize;
  const ty = element.y * cellSize;

  return (
    <g data-element-id={element.id} transform={`translate(${tx}, ${ty})`}>
      <g transform={`rotate(${element.rotation} ${half} ${half})`}>
        {element.type === 'TrackStraight' && (
          <line className="signal-box-track" x1="0" y1={half} x2={cellSize} y2={half} />
        )}
        {element.type === 'TrackCurve' && (
          <path className="signal-box-track" d={`M 0,${half} Q ${half},${half} ${half},${cellSize}`} />
        )}
        {element.type === 'Turnout' && (
          <>
            <line
              className={
                element.position === 'straight' ? 'signal-box-turnout-straight' : 'signal-box-track-muted'
              }
              x1="0"
              y1={half}
              x2={cellSize}
              y2={half}
            />
            <path
              className={
                element.position === 'diverging' ? 'signal-box-turnout-diverging' : 'signal-box-track-muted'
              }
              d={`M 0,${half} Q ${half},${half} ${half},${cellSize}`}
            />
            <circle
              className={
                element.position === 'diverging'
                  ? 'signal-box-turnout-hub-diverging'
                  : 'signal-box-turnout-hub-straight'
              }
              cx={half}
              cy={half}
              r="5"
            />
          </>
        )}
        {element.type === 'Detector' && (
          <>
            <line className="signal-box-track" x1="0" y1={half} x2={cellSize} y2={half} />
            <circle className="signal-box-detector-node" cx={half} cy={half} r="7" />
          </>
        )}
        {element.type === 'Signal' && (
          <g>
            {isSelectedSignal && (
              <rect
                className="signal-box-selection-ring"
                x="4"
                y="4"
                width={cellSize - 8}
                height={cellSize - 8}
                rx="6"
              />
            )}
            <rect className="signal-glyph-mast" x="28" y="37" width="4" height="15" rx="1.5" />
            <rect className="signal-glyph-housing" x="17" y="9" width="26" height="30" rx="4" />
            <circle
              className={preparedAspectId === 'Hp0' ? 'signal-lamp signal-lamp-red' : 'signal-lamp signal-lamp-off'}
              cx="34"
              cy="17"
              r="3.5"
            />
            <circle
              className={preparedAspectId === 'Ks1' ? 'signal-lamp signal-lamp-green' : 'signal-lamp signal-lamp-off'}
              cx="25"
              cy="25"
              r="3.5"
            />
            <circle
              className={
                preparedAspectId === 'Ks2'
                  ? 'signal-lamp signal-lamp-yellow signal-lamp-flashing'
                  : 'signal-lamp signal-lamp-off'
              }
              cx="35"
              cy="25"
              r="3.5"
            />
            <circle
              className={preparedAspectId === 'Sh1' ? 'signal-lamp signal-lamp-white' : 'signal-lamp signal-lamp-off'}
              cx="23"
              cy="33"
              r="2.75"
            />
            <circle
              className={preparedAspectId === 'Sh1' ? 'signal-lamp signal-lamp-white' : 'signal-lamp signal-lamp-off'}
              cx="32"
              cy="29"
              r="2.75"
            />
          </g>
        )}
      </g>

      {element.type === 'Turnout' && (
        <g data-label-for={element.id} transform={element.rotation === 180 ? 'translate(0, 62)' : 'translate(0, -24)'}>
          <rect className="signal-box-element-tag-bg" x="2" y="0" width="56" height="20" rx="4" />
          <text className="signal-box-element-label" x={half} y="14">
            {element.name}
          </text>
        </g>
      )}
      {element.type === 'Detector' && (
        <g data-label-for={element.id} transform="translate(-76, 20)">
          <rect className="signal-box-element-tag-bg" x="0" y="0" width="70" height="20" rx="4" />
          <text className="signal-box-element-label" x="35" y="14">
            {element.name}
          </text>
        </g>
      )}
      {element.type === 'Signal' && (
        <g data-label-for={element.id} transform="translate(64, 12)">
          <rect className="signal-box-element-tag-bg" x="0" y="0" width="84" height="36" rx="4" />
          <text className="signal-box-element-label" x="42" y="15">
            {element.name}
          </text>
          <text className="signal-box-element-sublabel" x="42" y="29">
            {preparedAspectName}
          </text>
        </g>
      )}
    </g>
  );
}

export function SignalBoxPage() {
  const signalBox = useMemo(() => parseExamples(defaultSources).signalBox, []);
  const reducedMotion = usePrefersReducedMotion();

  const { width, height, cellSize } = signalBox.grid;
  const viewWidth = width * cellSize;
  const viewHeight = height * cellSize;
  const cropX = VIEW_COL_START * cellSize;
  const cropY = VIEW_ROW_START * cellSize;
  const cropWidth = VIEW_COLS * cellSize;
  const cropHeight = VIEW_ROWS * cellSize;

  const selectedSignalElement = signalBox.elements.find((element) => element.id === signalBox.signal.elementId);
  const preparedAspect = signalBox.signal.aspects.find((aspect) => aspect.id === signalBox.signal.preparedAspect);
  const straightTurnout = signalBox.elements.find((element) => element.type === 'Turnout' && element.position === 'straight');
  const divergingTurnout = signalBox.elements.find((element) => element.type === 'Turnout' && element.position === 'diverging');
  const detectorElement = signalBox.elements.find((element) => element.type === 'Detector');

  if (!selectedSignalElement || !preparedAspect) return null;

  const verticalLines = Array.from({ length: width + 1 }, (_, index) => index * cellSize);
  const horizontalLines = Array.from({ length: height + 1 }, (_, index) => index * cellSize);

  return (
    <div className="signal-box" data-reduced-motion={reducedMotion ? 'reduce' : 'no-preference'}>
      <div className="signal-box-header moba-page-header">
        <h1 className="sr-only">Signal Box</h1>
        <fieldset className="signal-box-toolbar" aria-label="Signal Box toolbar">
          <InertButton className="signal-box-button">Delete</InertButton>
          <InertButton className="signal-box-button">Context</InertButton>
        </fieldset>
      </div>

      <div className="signal-box-workbench">
        <section className="signal-box-pane moba-col moba-col--helper" aria-label="Elements">
          <h2 className="moba-col-title"><ColumnBadge role="helper" />Elements<ColumnPin /></h2>
          <p className="signal-box-subtitle">Drag &amp; drop onto track canvas</p>
          <ul className="signal-box-tools">
            {TOOLBOX_ITEMS.map((item) => (
              <li key={item.type}>
                <InertButton className="signal-box-tool" label={item.label}>
                  <ToolboxGlyph type={item.type} />
                  <span className="signal-box-tool-label">{item.label}</span>
                </InertButton>
              </li>
            ))}
          </ul>
          <div className="signal-box-grid-meta">
            <p className="signal-box-grid-info">
              Grid: {width} × {height} ({cellSize} px)
            </p>
            <InertButton className="signal-box-button">Fit to view</InertButton>
          </div>
        </section>

        <section className="signal-box-pane signal-box-canvas-pane" aria-label="Signal box canvas">
          <div className="signal-box-canvas-header">
            <h2>Track Layout</h2>
            <ul className="signal-box-legend" aria-label="Element legend">
              <li className="signal-box-legend-item">
                <span className="signal-box-swatch signal-box-swatch-straight" aria-hidden="true" />
                {straightTurnout?.name ?? 'Turnout'} Straight
              </li>
              <li className="signal-box-legend-item">
                <span className="signal-box-swatch signal-box-swatch-diverging" aria-hidden="true" />
                {divergingTurnout?.name ?? 'Turnout'} Diverging
              </li>
              <li className="signal-box-legend-item">
                <span className="signal-box-swatch signal-box-swatch-detector" aria-hidden="true" />
                {detectorElement?.name ?? 'Block'} Detector
              </li>
              <li className="signal-box-legend-item">
                <span className="signal-box-swatch signal-box-swatch-selected" aria-hidden="true" />
                Selected ({selectedSignalElement.name})
              </li>
            </ul>
          </div>

          <div className="signal-box-canvas-stage">
            <svg
              aria-hidden="true"
              className="signal-box-svg"
              viewBox={`${cropX} ${cropY} ${cropWidth} ${cropHeight}`}
            >
              {verticalLines.map((x) => (
                <line key={`v-${x}`} className="signal-box-grid-line" x1={x} y1="0" x2={x} y2={viewHeight} />
              ))}
              {horizontalLines.map((y) => (
                <line key={`h-${y}`} className="signal-box-grid-line" x1="0" y1={y} x2={viewWidth} y2={y} />
              ))}
              {signalBox.elements.map((element) => (
                <CanvasElementGraphic
                  key={element.id}
                  element={element}
                  cellSize={cellSize}
                  isSelectedSignal={element.id === selectedSignalElement.id}
                  preparedAspectId={preparedAspect.id}
                  preparedAspectName={preparedAspect.name}
                />
              ))}
            </svg>

            <div className="signal-box-hit-grid">
              {signalBox.elements.map((element) => {
                const isSelected = element.id === selectedSignalElement.id;
                return (
                  <div
                    key={element.id}
                    className="signal-box-cell"
                    data-element-id={element.id}
                    data-grid-x={element.x - VIEW_COL_START}
                    data-grid-y={element.y - VIEW_ROW_START}
                    data-selected={isSelected ? 'true' : undefined}
                  >
                    <InertButton
                      className="signal-box-cell-button"
                      label={describeCanvasElement(element, selectedSignalElement.id, preparedAspect.name)}
                      pressed={isSelected ? true : undefined}
                    >
                      {null}
                    </InertButton>
                  </div>
                );
              })}
            </div>
          </div>
        </section>

        <section className="signal-box-pane signal-box-context moba-col moba-col--properties" aria-label="Context">
          <h2 className="moba-col-title"><ColumnBadge role="properties" />Context<ColumnPin /></h2>
          <div className="signal-box-selected-summary">
            <span className="signal-box-selected-name">{selectedSignalElement.name}</span>
            <span className="signal-box-selected-system">System: {signalBox.signal.system}</span>
          </div>

          <h3>Select Signal Aspect</h3>
          <ul className="signal-box-aspects">
            {signalBox.signal.aspects.map((aspect) => {
              const isPrepared = aspect.id === signalBox.signal.preparedAspect;
              const isFlashing = aspect.id === 'Ks2';
              return (
                <li key={aspect.id}>
                  <InertButton className="signal-box-aspect-tile" pressed={isPrepared}>
                    <SignalAspectGlyph aspectId={aspect.id} />
                    <span className="signal-aspect-code">{aspect.name}</span>
                    <span className="signal-aspect-desc">{aspect.description}</span>
                    {isPrepared && <span className="signal-aspect-badge">Prepared</span>}
                    {isFlashing && reducedMotion && (
                      <span className="signal-aspect-flashing-badge">Flashing</span>
                    )}
                  </InertButton>
                </li>
              );
            })}
          </ul>

          <h3>Multiplex Signal Configuration</h3>
          <section aria-label="Multiplexer configuration">
            <dl className="signal-box-mux-list">
              <div className="signal-box-mux-row">
                <dt>Multiplexer</dt>
                <dd>{signalBox.signal.multiplexer.articleNumber}</dd>
              </div>
              <div className="signal-box-mux-row">
                <dt>Base DCC address</dt>
                <dd>{signalBox.signal.multiplexer.baseAddress}</dd>
              </div>
              {signalBox.signal.multiplexer.outputs.map((output) => (
                <div key={output.label} className="signal-box-mux-row">
                  <dt>{output.label}</dt>
                  <dd>{output.value}</dd>
                </div>
              ))}
            </dl>
          </section>
        </section>
      </div>

      <section className="signal-box-footer moba-page-footer" aria-label="Signal box summary">
        <span className="signal-box-footer-status">
          <svg aria-hidden="true" className="signal-box-footer-icon" viewBox="0 0 16 16">
            <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor" strokeWidth="1.5" />
            <circle cx="8" cy="8" r="2.5" fill="currentColor" />
          </svg>
          <span>
            Selected: {selectedSignalElement.name} ({preparedAspect.name})
          </span>
        </span>
        <span className="signal-box-footer-count">
          {signalBox.elements.length} elements · Signal {selectedSignalElement.name} ({preparedAspect.name})
        </span>
      </section>
    </div>
  );
}
