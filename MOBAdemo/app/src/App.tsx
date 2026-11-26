import { useEffect, useState } from 'react';
import { ShellBars } from './shell/bars/ShellBars';
import { ShellLayout } from './shell/layout/ShellLayout';
import { DemoShell } from './shell/navigation/DemoShell';
import { useDemoTheme } from './shell/theme/useDemoTheme';

export interface AppProps {
  /** Loads the data files the app needs. Injected so the start-up states can be tested. */
  load: () => Promise<void>;
}

type LoadState = 'loading' | 'ready' | 'error';

export function App({ load }: Readonly<AppProps>) {
  const [state, setState] = useState<LoadState>('loading');
  const { theme, toggleTheme } = useDemoTheme();

  useEffect(() => {
    let cancelled = false;
    load().then(
      () => {
        if (!cancelled) setState('ready');
      },
      () => {
        if (!cancelled) setState('error');
      },
    );
    return () => {
      cancelled = true;
    };
  }, [load]);

  return (
    <ShellLayout>
      <ShellBars lightTheme={theme === 'light'} onToggleTheme={toggleTheme} loadFailed={state === 'error'}>
        {state === 'loading' && <output className="app-message">Loading MOBAdemo...</output>}
        {state === 'error' && (
          <div role="alert" className="app-message app-message-error">
            <p>The demo data could not be loaded. Please reload the page.</p>
          </div>
        )}
        {state === 'ready' && <DemoShell />}
      </ShellBars>
    </ShellLayout>
  );
}
