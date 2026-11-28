import { useId, useMemo } from 'react';
import { defaultSources, parseExamples } from '../../../examples/loader';
import texts from '../../../examples/texts.json';
import { InertButton } from '../../../shell/inert/InertButton';
import { CabControlTiles } from '../gauges/CabControlTiles';
import { CurrentGauge } from '../gauges/CurrentGauge';
import { SpeedometerGauge } from '../gauges/SpeedometerGauge';
import { TimetablePanel } from '../gauges/TimetablePanel';
import { SpeedSlider } from '../slider/SpeedSlider';
import './frame.css';

// Desktop light-theme 20px symbols for the shipped flagship, kept inside the standalone app build.
const functionIcons = import.meta.glob<string>('./icons/*.png', { eager: true, query: '?url', import: 'default' });

function SeriesSearch() {
  const hintId = useId();
  return (
    <div className="train-control-search">
      <input
        type="search"
        className="train-control-field"
        aria-label="Search by series"
        aria-disabled="true"
        aria-describedby={hintId}
        placeholder="Search by series"
        value=""
        readOnly
      />
      <span id={hintId} className="train-control-search-hint">{texts.inertHint}</span>
    </div>
  );
}

export function TrainControlFrame() {
  const { locomotives, timetable } = useMemo(() => parseExamples(defaultSources), []);
  const selected = locomotives.find((locomotive) => locomotive.selected);
  if (!selected) return null;

  const drive = selected.prepared;
  const direction = drive.direction === 'forward' ? 'Forward' : 'Reverse';

  return (
    <div className="train-control">
      <div className="train-control-header moba-page-header">
        <h1>Train Control</h1>
        <div className="train-control-toolbar" role="group" aria-label="Train Control toolbar">
          <SeriesSearch />
          <InertButton className="train-control-emergency">EMERGENCY STOP</InertButton>
        </div>
      </div>
      <div className="train-control-panels">
        <section className="train-control-panel" aria-label="Locomotives">
          <h2>Locomotives</h2>
          <ul className="train-control-roster">
            {locomotives.map((locomotive) => (
              <li key={locomotive.id}>
                <InertButton className="train-control-locomotive" pressed={locomotive.selected} label={locomotive.name}>
                  <img src={locomotive.photoUrl} alt="" />
                  <span>{locomotive.name}</span>
                </InertButton>
              </li>
            ))}
          </ul>
          <div className="train-control-drive" role="group" aria-label="Driving settings">
            <InertButton className="train-control-button" pressed={drive.direction === 'forward'}>
              Direction: {direction}
            </InertButton>
            <InertButton className="train-control-button" label={'DCC speed steps: ' + drive.speedSteps}>
              DCC speed steps: {drive.speedSteps} <span aria-hidden="true">▾</span>
            </InertButton>
            <InertButton className="train-control-button" pressed={drive.accelerationRamp}>Acceleration ramp</InertButton>
          </div>
        </section>
        <div className="train-control-center">
          <TimetablePanel timetable={timetable} />
          <CabControlTiles
            brake={drive.brake}
            doorRelease={drive.doorRelease}
            doorBlocking={drive.doorBlocking}
          />
          <section className="train-control-panel tc-gauge-panel" aria-label="Speedometer">
            <h2>Speedometer</h2>
            <SpeedometerGauge speedKmh={drive.speedKmh} />
            <SpeedSlider initialStep={drive.speed} />
          </section>
          <CurrentGauge
            currentAmps={drive.currentAmps}
            maxCurrentAmps={drive.maxCurrentAmps}
            trackVoltage={drive.trackVoltage}
          />
        </div>
        <section className="train-control-panel" aria-label="Functions">
          <h2>Functions</h2>
          <ul className="train-control-functions">
            {selected.functions.map((item) => (
              <li key={item.number}>
                <InertButton className="train-control-function" pressed={item.pressed}>
                  <img className="train-control-function-icon" src={functionIcons['./icons/' + item.icon + '.png']} alt="" />
                  <span className="train-control-function-number">F{item.number}</span>{' '}
                  <span lang="de">{item.name}</span>
                </InertButton>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  );
}
