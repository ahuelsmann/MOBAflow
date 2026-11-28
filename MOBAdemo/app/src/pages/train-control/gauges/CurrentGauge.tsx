import { GaugeArcSvg } from './GaugeArcSvg';

interface CurrentGaugeProps {
  currentAmps: number;
  maxCurrentAmps: number;
  trackVoltage: number;
}

export function CurrentGauge({ currentAmps, maxCurrentAmps, trackVoltage }: Readonly<CurrentGaugeProps>) {
  const ratio = maxCurrentAmps > 0 ? currentAmps / maxCurrentAmps : 0;
  const ticks = [0, 1 / 3, 2 / 3, 1].map((step) => {
    const value = step * maxCurrentAmps;
    return {
      ratio: step,
      label: step === 0 ? '0' : value.toFixed(1),
    };
  });

  return (
    <section className="train-control-panel tc-gauge-panel" aria-label="Track current">
      <h2 className="sr-only">Track current</h2>
      <div className="tc-gauge">
        <GaugeArcSvg ratio={ratio} ticks={ticks} />
        <p className="tc-gauge-readout">
          <span className="tc-gauge-value">{currentAmps.toFixed(2)} A</span>{' '}
          <span className="tc-gauge-unit">/ {maxCurrentAmps.toFixed(1)} A</span>
        </p>
        <p className="tc-gauge-meta">Track voltage: {trackVoltage.toFixed(1)} V</p>
      </div>
    </section>
  );
}
