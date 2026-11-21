import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// The host serves the app under script-src 'self'; style-src 'self' (spec FR-071), so the output must carry no
// inline script or style; the build test checks the entry document. Vite 8 injects the module
// preload polyfill into the entry chunk (not as an inline script), so it is not a CSP risk. It
// stays off because every supported browser has native module preload, which saves bytes.
export default defineConfig({
  plugins: [react()],
  build: {
    modulePreload: { polyfill: false },
    cssCodeSplit: true,
    assetsInlineLimit: 0,
  },
  test: {
    environment: 'jsdom',
    // The first ESLint run is cold and runs next to the build test; 5 s is not always enough.
    testTimeout: 30_000,
    setupFiles: ['./tests/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}', 'tests/**/*.test.{ts,tsx}'],
  },
});
