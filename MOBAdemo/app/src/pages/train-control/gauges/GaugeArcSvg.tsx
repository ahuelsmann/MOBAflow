import './gauges.css';

interface ArcPoint {
  x: number;
  y: number;
}

const CX = 130;
const CY = 120;
const RADIUS = 90;

function clampRatio(ratio: number): number {
  return Number.isFinite(ratio) ? Math.max(0, Math.min(1, ratio)) : 0;
}

export function polarArcPoint(ratio: number, radius: number = RADIUS): ArcPoint {
  const clamped = clampRatio(ratio);
  const theta = Math.PI * (1 - clamped);
  return {
    x: Number((CX + radius * Math.cos(theta)).toFixed(2)),
    y: Number((CY - radius * Math.sin(theta)).toFixed(2)),
  };
}

export function buildValueArcPath(ratio: number): string | null {
  const clamped = clampRatio(ratio);
  if (clamped <= 0) return null;
  const start = polarArcPoint(0);
  const end = polarArcPoint(clamped);
  return `M ${start.x} ${start.y} A ${RADIUS} ${RADIUS} 0 0 1 ${end.x} ${end.y}`;
}

interface GaugeArcSvgProps {
  ratio: number;
  ticks: ReadonlyArray<{ ratio: number; label: string }>;
}

/** Shared semi-circular SVG gauge face without inline styles (CSP style-src 'self'). */
export function GaugeArcSvg({ ratio, ticks }: Readonly<GaugeArcSvgProps>) {
  const clamped = clampRatio(ratio);
  const start = polarArcPoint(0);
  const end = polarArcPoint(1);
  const trackPath = `M ${start.x} ${start.y} A ${RADIUS} ${RADIUS} 0 0 1 ${end.x} ${end.y}`;
  const valuePath = buildValueArcPath(clamped);
  const needleAngle = (-90 + clamped * 180).toFixed(1);

  return (
    <svg className="tc-gauge-svg" viewBox="0 0 260 150" aria-hidden="true">
      <path className="tc-gauge-track" d={trackPath} />
      {valuePath && <path className="tc-gauge-fill" d={valuePath} />}
      {ticks.map((tick) => {
        const inner = polarArcPoint(tick.ratio, RADIUS - 12);
        const outer = polarArcPoint(tick.ratio, RADIUS + 4);
        const text = polarArcPoint(tick.ratio, RADIUS + 18);
        return (
          <g key={tick.ratio}>
            <line
              className="tc-gauge-tick"
              x1={inner.x}
              y1={inner.y}
              x2={outer.x}
              y2={outer.y}
            />
            <text className="tc-gauge-scale-label" x={text.x} y={text.y}>
              {tick.label}
            </text>
          </g>
        );
      })}
      <g className="tc-gauge-needle" transform={`rotate(${needleAngle} ${CX} ${CY})`}>
        <path d={`M ${CX - 3.5} ${CY} L ${CX} ${CY - 74} L ${CX + 3.5} ${CY} Z`} />
      </g>
      <circle className="tc-gauge-hub" cx={CX} cy={CY} r={8} />
    </svg>
  );
}
