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

const chromePaths = {
  train: 'M4 2h8v9H4zM4 7h8M5 12l-2 2m8-2 2 2M6 9h.01m4 0h.01',
  new: 'M8 3v10M3 8h10',
  open: 'M2 5h5l2 2h5l-2 6H2z',
  simulate: 'm5 3 7 5-7 5z',
  reset: 'M4 5a5 5 0 1 1-1 6M4 2v4h4',
  errors: 'M3 3l10 10M13 3 3 13',
  warnings: 'M8 2l6 11H2zM8 6v3m0 2h.01',
  information: 'M8 7v4m0-6h.01M14 8a6 6 0 1 1-12 0 6 6 0 0 1 12 0',
};

function ChromeIcon({ name }: Readonly<{ name: keyof typeof chromePaths }>) {
  return (
    <svg aria-hidden="true" className="demo-chrome-icon" viewBox="0 0 16 16">
      <path d={chromePaths[name]} fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function CommandBar({ lightTheme, onToggleTheme }: Pick<ShellBarsProps, 'lightTheme' | 'onToggleTheme'>) {
  return (
    <header className="demo-command-bar">
      <div className="demo-command-brand">
        <strong><ChromeIcon name="train" />MOBA<span>flow</span></strong>
        <small>Web Demo</small>
      </div>
      <div className="demo-command-workspace">
        <button className="demo-bar-button demo-bar-toggle" type="button" aria-pressed={lightTheme} onClick={onToggleTheme}>
          Light theme
        </button>
        <div className="demo-command-controls" role="group" aria-label="Commands">
          <InertButton className="demo-bar-button" hint={texts.fixedProjectHint}><ChromeIcon name="new" />New</InertButton>
          <InertButton className="demo-bar-button" hint={texts.fixedProjectHint}><ChromeIcon name="open" />Open</InertButton>
          <span className="demo-command-separator" aria-hidden="true" />
          <div className="demo-simulate-command">
            <InertButton className="demo-bar-button"><ChromeIcon name="simulate" />Simulate</InertButton>
            <InertNumberBox className="demo-bar-number" label="Feedback Point (InPort)" value={0} />
          </div>
          <span className="demo-command-separator" aria-hidden="true" />
          <InertButton className="demo-bar-button"><ChromeIcon name="reset" />Reset</InertButton>
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
        <span className="demo-message-summary">{loadFailed ? '2 messages' : '1 message'}</span>
        <div className="demo-message-filters" role="group" aria-label="Message filters">
          <InertButton className="demo-bar-button" pressed><ChromeIcon name="errors" />Errors 0</InertButton>
          <InertButton className="demo-bar-button" pressed><ChromeIcon name="warnings" />Warnings {warnings}</InertButton>
          <InertButton className="demo-bar-button" pressed><ChromeIcon name="information" />Information 1</InertButton>
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
          <InertButton className="demo-bar-button demo-bar-toggle demo-track-power" pressed={status.trackPowerOn}>{status.trackPowerLabel}</InertButton>
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
