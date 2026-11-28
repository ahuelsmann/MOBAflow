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
      <circle cx="12" cy="12" r="4.5" fill="none" stroke="currentColor" strokeWidth="1.8" />
      <path d="M6 5a9 9 0 0 0 0 14M18 5a9 9 0 0 1 0 14" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      {active && <path d="M12 8v4m0 3h.01" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />}
    </svg>
  );
}

function DoorReleaseIcon({ open }: Readonly<{ open: boolean }>) {
  return (
    <svg className="tc-cab-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M7 4h4v16H7zm6 0h4v16h-4z" fill="none" stroke="currentColor" strokeWidth="1.8" />
      <path
        d={open ? 'M2 9l3 3-3 3m20-6-3 3 3 3' : 'M9 12h6'}
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
      <path d="M3 5h6v14H3zm12 0h6v14h-6z" fill="none" stroke="currentColor" strokeWidth="1.8" />
      <path
        d={active ? 'm10 9 4 6m0-6-4 6' : 'M10 12h4'}
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
      <h2 className="sr-only">Cab controls</h2>
      <div className="tc-cab-grid">
        <InertButton className="tc-cab-tile tc-cab-brake" pressed={brake}>
          <BrakeIcon active={brake} />
          <span className="tc-cab-label sr-only">Brake</span>
          <span className="tc-cab-state sr-only">{brakeState}</span>
        </InertButton>
        <InertButton className="tc-cab-tile" pressed={doorRelease}>
          <DoorReleaseIcon open={doorRelease} />
          <span className="tc-cab-label sr-only">Door release</span>
          <span className="tc-cab-state sr-only">{doorReleaseState}</span>
        </InertButton>
        <div className="tc-cab-tile tc-cab-status" data-active={doorBlocking}>
          <DoorBlockingIcon active={doorBlocking} />
          <span className="tc-cab-label sr-only">Door blocking</span>
          <span className="tc-cab-state sr-only">{doorBlockingState}</span>
        </div>
      </div>
    </section>
  );
}
