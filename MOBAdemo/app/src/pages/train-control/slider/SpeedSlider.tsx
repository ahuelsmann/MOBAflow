import { useId, useState } from 'react';
import './speed-slider.css';

export function SpeedSlider({ initialStep }: Readonly<{ initialStep: number }>) {
  const inputId = useId();
  const [step, setStep] = useState(initialStep);
  const valueText = 'Step ' + step;

  return (
    <div className="train-speed-control" role="group" aria-label="Speed control">
      <label htmlFor={inputId}>Speed</label>
      <span className="train-speed-value" aria-hidden="true">{valueText}</span>
      <input
        id={inputId}
        type="range"
        min={0}
        max={126}
        step={1}
        value={step}
        aria-valuetext={valueText}
        onChange={(event) => setStep(event.currentTarget.valueAsNumber)}
      />
    </div>
  );
}
