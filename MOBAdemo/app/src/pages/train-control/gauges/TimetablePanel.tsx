import type { TimetableFile } from '../../../examples/types';
import './gauges.css';

interface TimetablePanelProps {
  timetable: TimetableFile;
}

type StopState = 'past' | 'current' | 'upcoming';

function StopMarkerIcon({ state }: Readonly<{ state: StopState }>) {
  return (
    <svg className="tc-stop-icon" viewBox="0 0 16 16" aria-hidden="true">
      {state === 'current' && <circle cx="8" cy="8" r="5" fill="currentColor" />}
      {state === 'past' && (
        <>
          <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor" strokeWidth="1.5" />
          <path d="m5.5 8.2 1.7 1.7 3.3-3.6" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
        </>
      )}
      {state === 'upcoming' && <circle cx="8" cy="8" r="5" fill="none" stroke="currentColor" strokeWidth="1.5" />}
    </svg>
  );
}

const STATE_LABELS: Readonly<Record<StopState, string>> = {
  past: 'Departed',
  current: 'Current stop',
  upcoming: 'Upcoming',
};

export function TimetablePanel({ timetable }: Readonly<TimetablePanelProps>) {
  return (
    <section className="train-control-panel tc-timetable" aria-label="Timetable">
      <div className="tc-timetable-header">
        <h2>Timetable</h2>
        <p className="tc-timetable-train">{timetable.trainName}</p>
      </div>
      <ol className="tc-timetable-stops">
        {timetable.stops.map((stop, index) => {
          const state: StopState =
            index === timetable.currentStopIndex
              ? 'current'
              : index < timetable.currentStopIndex
                ? 'past'
                : 'upcoming';

          return (
            <li
              key={stop.station}
              className="tc-stop"
              data-state={state}
              aria-current={state === 'current' ? 'step' : undefined}
            >
              <div className="tc-stop-main">
                <StopMarkerIcon state={state} />
                <strong className="tc-stop-name">{stop.station}</strong>
                <span className="tc-stop-badge">{STATE_LABELS[state]}</span>
              </div>
              <div className="tc-stop-details">
                <span>Platform {stop.platform}</span>
                {stop.arrival && <span>Arr {stop.arrival}</span>}
                {stop.departure && <span>Dep {stop.departure}</span>}
              </div>
            </li>
          );
        })}
      </ol>
    </section>
  );
}
