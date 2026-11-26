import { GaugeArcSvg } from './GaugeArcSvg';

interface SpeedometerGaugeProps {
  speedKmh: number;
  maxKmh?: number;
}

const DEFAULT_MAX_KMH = 400;

export function SpeedometerGauge({ speedKmh, maxKmh = DEFAULT_MAX_KMH }: Readonly<SpeedometerGaugeProps>) {
  const ratio = maxKmh > 0 ? speedKmh / maxKmh : 0;
  const ticks = [0, 0.25, 0.5, 0.75, 1].map((step) => ({
    ratio: step,
    label: String(Math.round(step * maxKmh)),
  }));

  return (
    <div className="tc-gauge">
      <GaugeArcSvg ratio={ratio} ticks={ticks} />
      <p className="tc-gauge-readout">
        <span className="tc-gauge-value">{Math.round(speedKmh)}</span>{' '}
        <span className="tc-gauge-unit">km/h</span>
      </p>
      <p className="tc-gauge-meta">Scale: 0 to {maxKmh} km/h</p>
    </div>
  );
}
