import { existsSync } from 'node:fs';
import { createServer } from 'node:net';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig, devices } from '@playwright/test';

const appDir = dirname(fileURLToPath(import.meta.url));
const hostProject = resolve(appDir, '../Host/MOBAdemo.csproj');
const releaseDll = resolve(appDir, '../Host/bin/Release/net10.0/MOBAdemo.dll');
const debugDll = resolve(appDir, '../Host/bin/Debug/net10.0/MOBAdemo.dll');

async function resolvePort(): Promise<number> {
  const configured = process.env.MOBADEMO_E2E_PORT;
  if (configured) {
    const parsed = Number(configured);
    if (Number.isInteger(parsed) && parsed > 0 && parsed < 65536) {
      return parsed;
    }
    throw new Error(`Invalid MOBADEMO_E2E_PORT value: ${configured}`);
  }

  const port = await new Promise<number>((resolvePortPromise, reject) => {
    const server = createServer();
    server.unref();
    server.on('error', reject);
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      if (!address || typeof address === 'string') {
        server.close(() => reject(new Error('Failed to allocate a free localhost port.')));
        return;
      }
      server.close((error) => (error ? reject(error) : resolvePortPromise(address.port)));
    });
  });

  process.env.MOBADEMO_E2E_PORT = String(port);
  return port;
}

function buildHostCommand(baseURL: string): string {
  if (existsSync(releaseDll)) {
    return `dotnet "${releaseDll}" --urls "${baseURL}" --environment Production`;
  }
  if (existsSync(debugDll)) {
    return `dotnet run --project "${hostProject}" --no-build --configuration Debug -- --urls "${baseURL}" --environment Production`;
  }
  return `dotnet run --project "${hostProject}" -- --urls "${baseURL}" --environment Production`;
}

const port = await resolvePort();
const baseURL = `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: './e2e',
  outputDir: './test-results',
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],
  use: {
    baseURL,
    headless: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: buildHostCommand(baseURL),
    url: `${baseURL}/health`,
    reuseExistingServer: false,
    timeout: 60_000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
