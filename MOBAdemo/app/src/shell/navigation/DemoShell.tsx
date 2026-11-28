import { useId, useState } from 'react';
import navigation from '../../examples/navigation.json';
import type { NavigationEntry } from '../../examples/types';
import { SignalBoxPage } from '../../pages/signal-box/SignalBoxPage';
import { TrackPlanPage } from '../../pages/track-plan/TrackPlanPage';
import { TrainControlFrame } from '../../pages/train-control/frame/TrainControlFrame';
import { WorkflowsPage } from '../../pages/workflows/WorkflowsPage';
import { NavigationIcon } from './NavigationIcon';
import './navigation.css';

const landingPage = 'traincontrol';
const disabledHint = 'Available in the MOBAflow desktop app (not included in this web demo)';

const demoEntriesByKey = new Map<string, NavigationEntry>(
  navigation.entries.filter((entry) => entry.demoPage).map((entry) => [entry.key, entry]),
);

const groups = new Map<string, NavigationEntry[]>();
for (const entry of navigation.entries) {
  const existing = groups.get(entry.category);
  if (existing) existing.push(entry);
  else groups.set(entry.category, [entry]);
}

function renderPageContent(page: string, title: string | null | undefined) {
  if (page === landingPage) {
    return <TrainControlFrame />;
  }
  if (page === 'trackplaneditor') {
    return <TrackPlanPage />;
  }
  if (page === 'signalbox') {
    return <SignalBoxPage />;
  }
  if (page === 'workflows') {
    return <WorkflowsPage />;
  }
  return <h1>{title ?? 'MOBAdemo'}</h1>;
}

export function DemoShell() {
  const [page, setPage] = useState(landingPage);
  const hintId = useId();
  const title = demoEntriesByKey.get(page)?.title ?? null;

  return (
    <div className="demo-nav-shell">
      <aside className="demo-nav-sidebar">
        <div className="demo-nav-chrome" aria-hidden="true">
          <span>←</span>
          <span>☰</span>
        </div>
        <p hidden id={hintId}>{disabledHint}</p>
        <nav aria-label="MOBAflow pages">
          {Array.from(groups, ([category, entries]) => (
            <ul key={category} aria-label={category}>
              {entries.map((entry) => {
                const available = entry.demoPage;
                const badgeId = entry.badge ? hintId + '-' + entry.key : undefined;
                const description = [badgeId, available ? undefined : hintId].filter(Boolean).join(' ');
                return (
                  <li key={entry.key}>
                    <button
                      type="button"
                      className="demo-nav-entry"
                      aria-current={available && entry.key === page ? 'page' : undefined}
                      aria-disabled={!available || undefined}
                      aria-describedby={description || undefined}
                      title={available ? entry.title : entry.title + '\n' + disabledHint}
                      tabIndex={available ? undefined : -1}
                      onClick={available ? () => setPage(entry.key) : undefined}
                    >
                      <NavigationIcon entry={entry} />
                      {entry.isBold ? <strong className="demo-nav-label">{entry.title}</strong> : <span className="demo-nav-label">{entry.title}</span>}
                      {entry.badge && (
                        <>
                          {' '}
                          <span className="demo-nav-badge" id={badgeId}>{entry.badge}</span>
                        </>
                      )}
                    </button>
                  </li>
                );
              })}
            </ul>
          ))}
        </nav>
      </aside>
      <main className="app-shell">
        {renderPageContent(page, title)}
      </main>
    </div>
  );
}
