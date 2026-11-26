import { useId, useState } from 'react';
import texts from '../../examples/texts.json';
import { InertButton } from '../../shell/inert/InertButton';
import './track-plan.css';

interface CatalogueItem {
  code: string;
  label: string;
}

interface CatalogueGroup {
  category: string;
  items: readonly CatalogueItem[];
}

const CATALOGUE_GROUPS: readonly CatalogueGroup[] = [
  {
    category: 'Straight',
    items: [
      { code: 'G62', label: 'G62 (61.88 mm)' },
      { code: 'G107', label: 'G107 (107.32 mm)' },
      { code: 'G115', label: 'G115 (115.46 mm)' },
      { code: 'G119', label: 'G119 (119.54 mm)' },
      { code: 'G231', label: 'G231 (230.93 mm)' },
      { code: 'G239', label: 'G239 (239.07 mm)' },
    ],
  },
  {
    category: 'Curve',
    items: [
      { code: 'R1', label: 'R1 (360 mm / 30°)' },
      { code: 'R2', label: 'R2 (422 mm / 30°)' },
      { code: 'R3', label: 'R3 (484 mm / 30°)' },
      { code: 'R4', label: 'R4 (546 mm / 30°)' },
      { code: 'R9', label: 'R9 (908 mm / 15°)' },
      { code: 'R175', label: 'R1/75 (360 mm / 7.5°)' },
      { code: 'R275', label: 'R2/75 (422 mm / 7.5°)' },
    ],
  },
  {
    category: 'Switch',
    items: [
      { code: 'WR', label: 'WR Right Turnout 15°' },
      { code: 'WL', label: 'WL Left Turnout 15°' },
      { code: 'WY', label: 'WY Y-Switch 30°' },
      { code: 'W3', label: 'W3 Three-Way Turnout' },
      { code: 'BWL', label: 'BWL Left Curved Turnout' },
      { code: 'BWR', label: 'BWR Right Curved Turnout' },
      { code: 'BWLR3', label: 'BWLR3 Left R3→R4' },
      { code: 'BWRR3', label: 'BWRR3 Right R3→R4' },
      { code: 'DKW', label: 'DKW Double Slip Switch' },
    ],
  },
  {
    category: 'Crossing',
    items: [
      { code: 'K15', label: 'K15 Crossing 15°' },
      { code: 'K30', label: 'K30 Crossing 30°' },
    ],
  },
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
  const sliderId = useId();
  const hintId = useId();
  const [hovered, setHovered] = useState(false);
  const [focused, setFocused] = useState(false);
  const [tapped, setTapped] = useState(false);
  const [dismissed, setDismissed] = useState(false);
  const visible = !dismissed && (hovered || focused || tapped);

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
        id={sliderId}
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
          if (event.key === 'Escape') {
            setTapped(false);
            setDismissed(true);
            return;
          }
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
      <div className="track-plan-header">
        <h1>Track Plan</h1>
        <div className="track-plan-toolbar" role="toolbar" aria-label="Track Plan toolbar">
          <InertButton className="track-plan-tool">Undo</InertButton>
          <InertButton className="track-plan-tool">Redo</InertButton>
          <InertButton className="track-plan-tool">Export</InertButton>
          <InertButton className="track-plan-tool">Validate</InertButton>
          <InertButton className="track-plan-tool">Fit</InertButton>
          <InertButton className="track-plan-tool">100%</InertButton>
          <InertButton className="track-plan-tool">Context</InertButton>
          <InertButton className="track-plan-tool" pressed={true}>Snap</InertButton>
          <InertButton className="track-plan-tool">Disconnect</InertButton>
          <InertButton className="track-plan-tool">Delete</InertButton>
          <InertButton className="track-plan-tool" pressed={false}>Grid</InertButton>
          <InertButton className="track-plan-tool" pressed={true}>Validation Overlay</InertButton>
          <InertButton className="track-plan-tool" pressed={true}>Track Labels</InertButton>
          <InertButton className="track-plan-tool" label="Rotate left">Rotate ←</InertButton>
          <InertButton className="track-plan-tool" label="Rotate right">Rotate →</InertButton>
        </div>
      </div>

      <div className="track-plan-workspace">
        <section className="track-plan-panel track-plan-catalogue" aria-label="Piko A-Track catalogue">
          <h2>Piko A-Track</h2>
          <div className="track-plan-catalogue-groups">
            {CATALOGUE_GROUPS.map((group) => (
              <div key={group.category} className="track-plan-catalogue-group">
                <h3>{group.category}</h3>
                <ul className="track-plan-catalogue-list">
                  {group.items.map((item) => (
                    <li key={item.code}>
                      <InertButton className="track-plan-catalogue-item">{item.label}</InertButton>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </section>

        <section className="track-plan-panel track-plan-canvas-pane" aria-label="Track Plan canvas">
          <div className="track-plan-canvas-frame">
            <img
              className="track-plan-canvas-image"
              src="/assets/track-plan.svg"
              alt="Example model railway layout on Track Plan canvas"
            />
            <div className="track-plan-zoom-bar" role="group" aria-label="Zoom controls">
              <InertButton className="track-plan-zoom-button" label="Zoom out">−</InertButton>
              <InertZoomSlider />
              <InertButton className="track-plan-zoom-button" label="Zoom in">+</InertButton>
              <span className="track-plan-zoom-value">100%</span>
            </div>
          </div>
        </section>

        <aside className="track-plan-panel track-plan-context" aria-label="Context properties">
          <h2>Context</h2>

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
                <span className="track-plan-legend-marker track-plan-legend-open" aria-hidden="true">○</span>
                <span>Open ends</span>
              </li>
              <li>
                <span className="track-plan-legend-marker track-plan-legend-overlap" aria-hidden="true">◎</span>
                <span>Disconnected, overlapping ports</span>
              </li>
              <li>
                <span className="track-plan-legend-marker track-plan-legend-group" aria-hidden="true">□</span>
                <span>Disconnected groups</span>
              </li>
            </ul>
          </div>
        </aside>
      </div>

      <div className="track-plan-footer" role="region" aria-label="Track Plan status">
        <span className="track-plan-status">
          <span className="track-plan-status-badge" aria-hidden="true">✓</span>
          <span>Ready — Example layout loaded</span>
        </span>
        <span className="track-plan-coordinates">X: 0 mm, Y: 0 mm</span>
      </div>
    </div>
  );
}
