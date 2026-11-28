import type { ReactNode } from 'react';
import texts from '../../examples/texts.json';
import { InertButton, InertNumberBox } from '../inert/InertButton';
import './bars.css';

interface ShellBarsProps {
  children: ReactNode;
  lightTheme: boolean;
  onToggleTheme: () => void;
  loadFailed: boolean;
}

function CommandBar({ lightTheme, onToggleTheme }: Pick<ShellBarsProps, 'lightTheme' | 'onToggleTheme'>) {
  return (
    <header className="demo-command-bar">
      <div className="demo-command-brand">
        <strong>MOBA<span>flow</span></strong>
        <small>Web Demo</small>
      </div>
      <div className="demo-command-workspace">
        <button className="demo-bar-button demo-bar-toggle" type="button" aria-pressed={lightTheme} onClick={onToggleTheme}>
          Light theme
        </button>
        <div className="demo-command-controls" role="group" aria-label="Commands">
          <InertButton className="demo-bar-button" hint={texts.fixedProjectHint}>New</InertButton>
          <InertButton className="demo-bar-button" hint={texts.fixedProjectHint}>Open</InertButton>
          <span className="demo-command-separator" aria-hidden="true" />
          <div className="demo-simulate-command">
            <InertButton className="demo-bar-button">Simulate</InertButton>
            <InertNumberBox className="demo-bar-number" label="Feedback Point (InPort)" value={0} />
          </div>
          <span className="demo-command-separator" aria-hidden="true" />
          <InertButton className="demo-bar-button">Reset</InertButton>
        </div>
      </div>
    </header>
  );
}

function MessagesBar({ loadFailed }: { loadFailed: boolean }) {
  const warnings = loadFailed ? 1 : 0;
  return (
    <section className="demo-messages-bar" aria-label="Messages">
      <div className="demo-messages-controls">
        <InertButton className="demo-bar-button" expanded>
          <span aria-hidden="true">▾ </span>Messages
        </InertButton>
        <span>{loadFailed ? '2 messages' : '1 message'}</span>
        <div className="demo-message-filters" role="group" aria-label="Message filters">
          <InertButton className="demo-bar-button" pressed>Errors 0</InertButton>
          <InertButton className="demo-bar-button" pressed>Warnings {warnings}</InertButton>
          <InertButton className="demo-bar-button" pressed>Information 1</InertButton>
          <InertButton className="demo-bar-button">Refresh</InertButton>
        </div>
      </div>
      <ul className="demo-message-list">
        <li><strong>Info</strong> {texts.firstVisitMessage.text}</li>
        {loadFailed && <li><strong>Warning</strong> Prepared example data is unavailable.</li>}
      </ul>
    </section>
  );
}

function StatusBar() {
  const status = texts.statusBar;
  return (
    <footer className="demo-status-bar" aria-label="Demo status">
      <ul>
        <li>{status.solution}</li>
        <li>
          {status.z21}
          <InertButton className="demo-bar-button demo-bar-toggle" pressed={status.trackPowerOn}>{status.trackPowerLabel}</InertButton>
        </li>
        <li>{status.restApi}</li>
        <li>
          {status.speech}
          <InertButton className="demo-bar-button" label="Speech provider">— ▾</InertButton>
        </li>
      </ul>
    </footer>
  );
}

export function ShellBars({ children, lightTheme, onToggleTheme, loadFailed }: Readonly<ShellBarsProps>) {
  return (
    <div className="demo-shell-frame">
      <CommandBar lightTheme={lightTheme} onToggleTheme={onToggleTheme} />
      <div className="demo-shell-body">{children}</div>
      <MessagesBar loadFailed={loadFailed} />
      <StatusBar />
    </div>
  );
}
