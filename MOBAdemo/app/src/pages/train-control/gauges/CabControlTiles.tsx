import texts from '../../../examples/texts.json';
import { InertButton } from '../../../shell/inert/InertButton';
import './gauges.css';

interface CabControlTilesProps {
  brake: boolean;
  doorRelease: boolean;
  doorBlocking: boolean;
}

function BrakeIcon({ active }: Readonly<{ active: boolean }>) {
  return (
    <svg className="tc-cab-icon" viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="12" r="8" fill="none" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7 7v10M17 7v10" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      {active && <path d="M12 8v4m0 3h.01" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />}
    </svg>
  );
}

function DoorReleaseIcon({ open }: Readonly<{ open: boolean }>) {
  return (
    <svg className="tc-cab-icon" viewBox="0 0 24 24" aria-hidden="true">
      <rect x="4" y="4" width="16" height="16" rx="2" fill="none" stroke="currentColor" strokeWidth="1.8" />
      <path
        d={open ? 'M12 4v16M8 12H5m11 0h3' : 'M12 4v16M9 12h6'}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
    </svg>
  );
}

function DoorBlockingIcon({ active }: Readonly<{ active: boolean }>) {
  return (
    <svg className="tc-cab-icon" viewBox="0 0 24 24" aria-hidden="true">
      <rect x="6" y="10" width="12" height="10" rx="2" fill="none" stroke="currentColor" strokeWidth="1.8" />
      <path
        d={active ? 'M8 10V7a4 4 0 0 1 8 0v3' : 'M8 10V7a4 4 0 0 1 7.5-2'}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function CabControlTiles({ brake, doorRelease, doorBlocking }: Readonly<CabControlTilesProps>) {
  const brakeState = brake ? 'Applied' : 'Released';
  const doorReleaseState = doorRelease ? 'Released' : 'Locked';
  const doorBlockingState = doorBlocking ? 'Active' : 'Inactive';

  return (
    <section className="train-control-panel tc-cab-panel" aria-label="Cab controls">
      <h2>Cab controls</h2>
      <div className="tc-cab-grid">
        <InertButton className="tc-cab-tile" pressed={brake}>
          <BrakeIcon active={brake} />
          <span className="tc-cab-label">Brake</span>
          <span className="tc-cab-state">{brakeState}</span>
        </InertButton>
        <InertButton className="tc-cab-tile" pressed={doorRelease}>
          <DoorReleaseIcon open={doorRelease} />
          <span className="tc-cab-label">Door release</span>
          <span className="tc-cab-state">{doorReleaseState}</span>
        </InertButton>
        <div
          className="tc-cab-tile tc-cab-status"
          data-active={doorBlocking}
          title={texts.inertHint}
        >
          <DoorBlockingIcon active={doorBlocking} />
          <span className="tc-cab-label">Door blocking</span>
          <span className="tc-cab-state">{doorBlockingState}</span>
        </div>
      </div>
    </section>
  );
}
