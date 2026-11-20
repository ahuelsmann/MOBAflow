import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// The host serves the app under a content security policy of
// script-src 'self'; style-src 'self' (spec FR-071), so the output must carry no
// inline script or style. The module preload polyfill is an inline script, so it is off.
export default defineConfig({
  plugins: [react()],
  build: {
    modulePreload: { polyfill: false },
    cssCodeSplit: true,
    assetsInlineLimit: 0,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}', 'tests/**/*.test.{ts,tsx}'],
  },
});
