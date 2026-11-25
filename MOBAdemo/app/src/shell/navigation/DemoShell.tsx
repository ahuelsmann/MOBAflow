import { useId, useState } from 'react';
import { enabledPages } from '../../config/enabledPages';
import navigation from '../../examples/navigation.json';
import type { NavigationEntry } from '../../examples/types';
import { NavigationIcon } from './NavigationIcon';
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

interface DemoShellProps {
  lightTheme: boolean;
  onToggleTheme: () => void;
}

export function DemoShell({ lightTheme, onToggleTheme }: Readonly<DemoShellProps>) {
  const [page, setPage] = useState(landingPage);
  const hintId = useId();
  const title = flagsByKey[page] ? navigation.entries.find((entry) => entry.key === page)?.title : null;

  return (
    <div className="demo-nav-shell">
      <aside className="demo-nav-sidebar">
        <button className="demo-theme-toggle" type="button" aria-pressed={lightTheme} onClick={onToggleTheme}>
          Light theme
        </button>
        <p hidden id={hintId}>{disabledHint}</p>
        <nav aria-label="MOBAflow pages">
          {Array.from(groups, ([category, entries]) => (
            <ul key={category} aria-label={category}>
              {entries.map((entry) => {
                const available = entry.demoPage && flagsByKey[entry.key] === true;
                const badgeId = entry.badge ? hintId + '-' + entry.key : undefined;
                const description = [badgeId, available ? undefined : hintId].filter(Boolean).join(' ');
                return (
                  <li key={entry.key}>
                    <button
                      type="button"
                      className="demo-nav-entry"
                      aria-label={entry.title}
                      aria-current={available && entry.key === page ? 'page' : undefined}
                      aria-disabled={!available || undefined}
                      aria-describedby={description || undefined}
                      title={available ? undefined : disabledHint}
                      tabIndex={available ? undefined : -1}
                      onClick={available ? () => setPage(entry.key) : undefined}
                    >
                      <NavigationIcon entry={entry} />
                      {entry.isBold ? <strong>{entry.title}</strong> : <span>{entry.title}</span>}
                      {entry.badge && <span className="demo-nav-badge" id={badgeId}>{entry.badge}</span>}
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
