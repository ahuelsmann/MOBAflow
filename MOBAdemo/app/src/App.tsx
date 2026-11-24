import { useEffect, useState } from 'react';
import { DemoShell } from './shell/navigation/DemoShell';

export interface AppProps {
  /** Loads the data files the app needs. Injected so the start-up states can be tested. */
  load: () => Promise<void>;
}

type LoadState = { kind: 'loading' } | { kind: 'ready' } | { kind: 'error'; message: string };

export function App({ load }: Readonly<AppProps>) {
  const [state, setState] = useState<LoadState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;
    load().then(
      () => {
        if (!cancelled) setState({ kind: 'ready' });
      },
      (error: unknown) => {
        if (!cancelled) {
          setState({ kind: 'error', message: error instanceof Error ? error.message : String(error) });
        }
      },
    );
    return () => {
      cancelled = true;
    };
  }, [load]);

  if (state.kind === 'loading') {
    return (
      <output className="app-message">
        Loading MOBAdemo...
      </output>
    );
  }

  if (state.kind === 'error') {
    return (
      <div role="alert" className="app-message app-message-error">
        <p>The demo data could not be loaded. Please reload the page.</p>
        <p>{state.message}</p>
      </div>
    );
  }

  return <DemoShell />;
}
