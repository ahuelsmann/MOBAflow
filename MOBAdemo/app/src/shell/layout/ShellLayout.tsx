import { useState, useSyncExternalStore, type ReactNode } from 'react';
import './layout.css';

function subscribeToResize(notify: () => void) {
  window.addEventListener('resize', notify);
  return () => window.removeEventListener('resize', notify);
}

function getShellMode() {
  if (window.innerWidth < 1024) return 'narrow';
  return window.innerWidth < 1280 ? 'compact' : 'full';
}

export function ShellLayout({ children }: Readonly<{ children: ReactNode }>) {
  const mode = useSyncExternalStore(subscribeToResize, getShellMode);
  const [noticeDismissed, setNoticeDismissed] = useState(false);

  return (
    <div data-shell-mode={mode}>
      {mode === 'narrow' && !noticeDismissed && (
        <div className="demo-narrow-notice">
          <p>Desktop UI demo - best on a screen at least 1024 px wide</p>
          <button type="button" aria-label="Dismiss narrow-window notice" onClick={() => setNoticeDismissed(true)}>
            Dismiss
          </button>
        </div>
      )}
      <div className="demo-shell-viewport">
        <div className="demo-desktop-layout">{children}</div>
      </div>
    </div>
  );
}
