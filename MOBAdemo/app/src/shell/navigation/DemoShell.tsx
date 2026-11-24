import { useId, useState } from 'react';
import { enabledPages } from '../../config/enabledPages';
import navigation from '../../examples/navigation.json';
import type { NavigationEntry } from '../../examples/types';
import { useDemoTheme } from '../theme/useDemoTheme';
import './navigation.css';

const landingPage = 'traincontrol';
const disabledHint = 'Available in the MOBAflow desktop app (not included in this web demo)';

const flagsByKey: Readonly<Partial<Record<string, boolean>>> = {
  traincontrol: enabledPages['train-control'],
  workflows: enabledPages.workflows,
  trackplaneditor: enabledPages['track-plan'],
  signalbox: enabledPages['signal-box'],
};

const groups = new Map<string, NavigationEntry[]>();
for (const entry of navigation.entries) {
  const existing = groups.get(entry.category);
  if (existing) existing.push(entry);
  else groups.set(entry.category, [entry]);
}

function NavigationIcon({ entry }: { entry: NavigationEntry }) {
  if (entry.pathIconData) {
    return (
      <svg aria-hidden="true" className="demo-nav-icon" viewBox="0 0 18 20" fill="currentColor">
        <path d={entry.pathIconData} />
      </svg>
    );
  }

  return <span aria-hidden="true" className="demo-nav-icon">{entry.icon}</span>;
}

export function DemoShell() {
  const [page, setPage] = useState(landingPage);
  const { theme, toggleTheme } = useDemoTheme();
  const hintId = useId();
  const title = flagsByKey[page] ? navigation.entries.find((entry) => entry.key === page)?.title : null;

  return (
    <div className="demo-nav-shell">
      <aside className="demo-nav-sidebar">
        <button className="demo-theme-toggle" type="button" aria-pressed={theme === 'light'} onClick={toggleTheme}>
          Light theme
        </button>
        <p hidden id={hintId}>{disabledHint}</p>
        <nav aria-label="MOBAflow pages">
          {Array.from(groups, ([category, entries]) => (
            <ul key={category} aria-label={category}>
              {entries.map((entry) => {
                const available = entry.demoPage && flagsByKey[entry.key] === true;
                return (
                  <li key={entry.key}>
                    <button
                      type="button"
                      className="demo-nav-entry"
                      aria-label={entry.title}
                      aria-current={available && entry.key === page ? 'page' : undefined}
                      aria-disabled={!available || undefined}
                      aria-describedby={available ? undefined : hintId}
                      title={available ? undefined : disabledHint}
                      tabIndex={available ? undefined : -1}
                      onClick={available ? () => setPage(entry.key) : undefined}
                    >
                      <NavigationIcon entry={entry} />
                      {entry.isBold ? <strong>{entry.title}</strong> : <span>{entry.title}</span>}
                      {entry.badge && <span className="demo-nav-badge">{entry.badge}</span>}
                    </button>
                  </li>
                );
              })}
            </ul>
          ))}
        </nav>
      </aside>
      <main className="app-shell">
        <h1>{title ?? 'MOBAdemo'}</h1>
      </main>
    </div>
  );
}
