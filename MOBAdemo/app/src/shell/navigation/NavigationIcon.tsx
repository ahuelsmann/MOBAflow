import type { NavigationEntry } from '../../examples/types';

// Small web equivalents of the navigation model's glyphs. Shared desktop glyphs share a path.
const paths: Readonly<Partial<Record<string, string>>> = {
  '\uE80F': 'M3 11 12 3l9 8M5 10v10h5v-6h4v6h5V10', // Overview
  '\uEC49': 'M4 17a8 8 0 1 1 16 0H4Zm8-2 4-5M7 12l1 1m4-4v1', // Train Control
  '\uE7C1': 'M5 18V6h14v12H5Zm0-8h14M9 14h6M9 22l2-4m4 4-2-4', // Journeys
  '\uE787': 'M4 6h16v14H4V6Zm0 4h16M8 4v4m8-4v4m-8 5h3m-3 3h8', // Timetable
  '\uEF90': 'M4 4h6v5H4V4Zm10 11h6v5h-6v-5ZM7 9v8h7m3-2V7h-7', // Workflows
  '\uE945': 'M12 3v3m0 12v3M3 12h3m12 0h3M8 8l8 8m0-8-8 8', // Event Manager
  '\uE81D': 'm3 6 6-2 6 2 6-2v14l-6 2-6-2-6 2V6Zm6-2v14m6-12v14', // Journey Map
  '\uE8B7': 'M3 7h7l2 2h9v10H3V7Zm0 0V5h8l2 2h6v2', // Solution
  '\uEC06': 'm3 10 9-6 9 6M5 10v10h14V10M9 20v-7h6v7', // Stations
  '\uE7C0': 'M6 6a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v11H6V6Zm0 5h12m-8 3h4m-6 6 2-3m6 3-2-3', // Rolling stock
  '\uE7F9': 'M6 3v18m12-18v18M6 7h12M6 12h12M6 17h12', // Track Plan
  '\uE7F4': 'M3 5h18v12H3V5Zm9 12v3m-4 0h8', // ESP32 Display / Monitor
  '\uE8A9': 'M4 4h16v16H4V4Zm0 5h16M4 15h16M9 4v16m6-16v16', // Matrix Images
  '\uE7C8': 'M20 12a8 8 0 1 1-16 0 8 8 0 0 1 16 0Zm-6 0a2 2 0 1 1-4 0 2 2 0 0 1 4 0Z', // Recorder
  '\uE897': 'M20 12a8 8 0 1 1-16 0 8 8 0 0 1 16 0ZM10 9a2 2 0 1 1 3 2l-1 2m0 3h.01', // Help
  '\uE946': 'M20 12a8 8 0 1 1-16 0 8 8 0 0 1 16 0Zm-8-1v5m0-8h.01', // Info
  '\uE115': 'M4 7h3m4 0h9M4 12h9m4 0h3M4 17h3m4 0h9M7 5v4m10 1v4M7 15v4', // Settings
};

export function NavigationIcon({ entry }: { entry: NavigationEntry }) {
  if (entry.pathIconData) {
    return (
      <svg aria-hidden="true" className="demo-nav-icon" viewBox="0 0 18 20" fill="currentColor">
        <path d={entry.pathIconData} />
      </svg>
    );
  }

  return (
    <svg aria-hidden="true" className="demo-nav-icon" viewBox="0 0 24 24">
      <path
        d={entry.icon ? paths[entry.icon] : undefined}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
