import { ColumnBadge } from '../../tokens/ColumnBadge';
import { useEffect, useId, useRef, useState } from 'react';
import texts from '../../examples/texts.json';
import { InertButton } from '../../shell/inert/InertButton';
import './track-plan.css';

const CATALOGUE_GROUPS: ReadonlyArray<readonly [category: string, items: readonly string[]]> = [
  ['Straight', ['G62 (61.88 mm)', 'G107 (107.32 mm)', 'G115 (115.46 mm)', 'G119 (119.54 mm)', 'G231 (230.93 mm)', 'G239 (239.07 mm)']],
  ['Curve', ['R1 (360 mm / 30°)', 'R2 (422 mm / 30°)', 'R3 (484 mm / 30°)', 'R4 (546 mm / 30°)', 'R9 (908 mm / 15°)', 'R175 (360 mm / 7.5°)', 'R275 (422 mm / 7.5°)']],
  ['Switch', ['WR Right Turnout 15°', 'WL Left Turnout 15°', 'WY Y-Switch 30°', 'W3 Three-Way Turnout', 'BWL Left Curved Turnout', 'BWR Right Curved Turnout', 'BWLR3 Left R3→R4', 'BWRR3 Right R3→R4', 'DKW Double Slip Switch']],
  ['Crossing', ['K15 Crossing 15°', 'K30 Crossing 30°']],
];

interface ToolbarItem {
  text: string;
  label?: string;
  pressed?: boolean;
}

const TOOLBAR_ITEMS: readonly ToolbarItem[] = [
  { text: 'Undo' },
  { text: 'Redo' },
  { text: 'Export' },
  { text: 'Validate' },
  { text: 'Fit' },
  { text: '100%' },
  { text: 'Context' },
  { text: 'Snap', pressed: true },
  { text: 'Disconnect' },
  { text: 'Delete' },
  { text: 'Grid', pressed: false },
  { text: 'Validation Overlay', pressed: true },
  { text: 'Track Labels', pressed: true },
  { text: 'Rotate ←', label: 'Rotate left' },
  { text: 'Rotate →', label: 'Rotate right' },
];

const IGNORED_SLIDER_KEYS = new Set([
  'ArrowLeft',
  'ArrowRight',
  'ArrowUp',
  'ArrowDown',
  'Home',
  'End',
  'PageUp',
  'PageDown',
]);

function InertZoomSlider() {
  const hintId = useId();
  const sliderRef = useRef<HTMLInputElement>(null);
  const [hovered, setHovered] = useState(false);
  const [focused, setFocused] = useState(false);
  const [tapped, setTapped] = useState(false);
  const [dismissed, setDismissed] = useState(false);
  const visible = !dismissed && (hovered || focused || tapped);

  useEffect(() => {
    if (!visible) return;
    function onDocumentKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Escape') return;
      setTapped(false);
      setDismissed(true);
    }
    document.addEventListener('keydown', onDocumentKeyDown);
    return () => document.removeEventListener('keydown', onDocumentKeyDown);
  }, [visible]);

  useEffect(() => {
    if (!tapped) return;
    function onOutsidePointerDown(event: PointerEvent) {
      const target = event.target;
      if (target instanceof Node && sliderRef.current?.contains(target)) return;
      setTapped(false);
    }
    document.addEventListener('pointerdown', onOutsidePointerDown);
    return () => document.removeEventListener('pointerdown', onOutsidePointerDown);
  }, [tapped]);

  return (
    <span
      className="track-plan-zoom-slider-wrap"
      onPointerEnter={() => {
        setHovered(true);
        setDismissed(false);
      }}
      onPointerLeave={() => setHovered(false)}
    >
      <input
        ref={sliderRef}
        className="track-plan-zoom-slider"
        type="range"
        min={10}
        max={300}
        step={5}
        value={100}
        aria-label="Zoom"
        aria-valuetext="100%"
        aria-disabled="true"
        aria-describedby={hintId}
        onFocus={() => {
          setFocused(true);
          setDismissed(false);
        }}
        onBlur={() => {
          setFocused(false);
          setTapped(false);
        }}
        onClick={() => {
          setDismissed(tapped);
          setTapped(!tapped);
        }}
        onKeyDown={(event) => {
          if (IGNORED_SLIDER_KEYS.has(event.key)) {
            event.preventDefault();
          }
        }}
        onInput={(event) => {
          event.currentTarget.value = '100';
        }}
        onChange={(event) => {
          event.currentTarget.value = '100';
        }}
      />
      <span id={hintId} className="track-plan-zoom-hint" role="tooltip" hidden={!visible}>
        {texts.inertHint}
      </span>
    </span>
  );
}

export function TrackPlanPage() {
  return (
    <div className="track-plan">
      <div className="track-plan-header moba-page-header">
        <h1>Track Plan</h1>
        <div className="track-plan-toolbar" role="toolbar" aria-label="Track Plan toolbar">
          {TOOLBAR_ITEMS.map((tool) => (
            <InertButton key={tool.text} className="track-plan-tool" pressed={tool.pressed} label={tool.label}>
              {tool.text}
            </InertButton>
          ))}
        </div>
      </div>

      <div className="track-plan-workspace">
        <section className="track-plan-panel track-plan-catalogue moba-col moba-col--collection" aria-label="Piko A-Track catalogue">
          <h2 className="moba-col-title"><ColumnBadge role="collection" />{"Piko A-Track"}</h2>
          <div className="track-plan-catalogue-groups">
            {CATALOGUE_GROUPS.map(([category, items]) => (
              <div key={category} className="track-plan-catalogue-group">
                <h3>{category}</h3>
                <ul className="track-plan-catalogue-list">
                  {items.map((label) => (
                    <li key={label}>
                      <InertButton className="track-plan-catalogue-item">{label}</InertButton>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </section>

        <section className="track-plan-panel track-plan-canvas-pane moba-col moba-col--helper" aria-label="Track Plan canvas">
          <h2 className="moba-col-title"><ColumnBadge role="helper" />Layout</h2>
          <div className="track-plan-canvas-frame">
            <img
              className="track-plan-canvas-image"
              src="/assets/track-plan.svg"
              alt="Example model railway layout on Track Plan canvas"
            />
            <fieldset className="track-plan-zoom-bar" aria-label="Zoom controls">
              <InertButton className="track-plan-zoom-button" label="Zoom out">−</InertButton>
              <InertZoomSlider />
              <InertButton className="track-plan-zoom-button" label="Zoom in">+</InertButton>
              <span className="track-plan-zoom-value">100%</span>
            </fieldset>
          </div>
        </section>

        <aside className="track-plan-panel track-plan-context moba-col moba-col--properties" aria-label="Context properties">
          <h2 className="moba-col-title"><ColumnBadge role="properties" />{"Context"}</h2>

          <div className="track-plan-context-section">
            <h3>Layout</h3>
            <dl className="track-plan-props">
              <dt>Plan</dt>
              <dd>MOBAflow example layout</dd>
              <dt>Gauge</dt>
              <dd>H0 (1:87)</dd>
              <dt>Track System</dt>
              <dd>Piko A-Track</dd>
              <dt>Selection</dt>
              <dd>No selection</dd>
            </dl>
          </div>

          <div className="track-plan-context-section">
            <h3>Sections</h3>
            <p className="track-plan-muted">No section assigned</p>
            <div className="track-plan-context-actions">
              <InertButton className="track-plan-tool">Create Section</InertButton>
              <InertButton className="track-plan-tool">Toggle Isolator</InertButton>
            </div>
          </div>

          <div className="track-plan-context-section">
            <h3>Statistics</h3>
            <dl className="track-plan-props track-plan-stats">
              <dt>Tracks</dt>
              <dd>12</dd>
              <dt>Connections</dt>
              <dd>11</dd>
              <dt>Open Ends</dt>
              <dd>2</dd>
              <dt>Zoom</dt>
              <dd>100%</dd>
            </dl>
          </div>

          <div className="track-plan-context-section">
            <h3>Validation Overlay</h3>
            <ul className="track-plan-legend">
              <li>
                <span className="track-plan-legend-marker" aria-hidden="true">○</span>
                <span>Open ends</span>
              </li>
              <li>
                <span className="track-plan-legend-marker" aria-hidden="true">◎</span>
                <span>Disconnected, overlapping ports</span>
              </li>
              <li>
                <span className="track-plan-legend-marker" aria-hidden="true">□</span>
                <span>Disconnected groups</span>
              </li>
            </ul>
          </div>
        </aside>
      </div>

      <section className="track-plan-footer moba-page-footer" aria-label="Track Plan status">
        <span className="track-plan-status">
          <span className="track-plan-status-badge" aria-hidden="true">✓</span>
          <span>Ready — Example layout loaded</span>
        </span>
        <span className="track-plan-coordinates">X: 0 mm, Y: 0 mm</span>
      </section>
    </div>
  );
}
