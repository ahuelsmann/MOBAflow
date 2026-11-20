import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './tokens/dark.css';
import './tokens/base.css';

// No example data files exist yet (slice 6 adds them); the loader resolves at once.
const loadData = async (): Promise<void> => {};

const container = document.getElementById('root');
if (container === null) {
  throw new Error('The #root element is missing from index.html.');
}

createRoot(container).render(
  <StrictMode>
    <App load={loadData} />
  </StrictMode>,
);
