import { hostname, userInfo } from 'node:os';
import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Response } from '@playwright/test';

const FIRST_VISIT_BUDGET_BYTES = 1_500_000;
const SCRIPT_STYLE_BUDGET_BYTES = 200_000;
const REPEAT_VISIT_BUDGET_BYTES = 100_000;
const NARROW_NOTICE = 'Desktop UI demo - best on a screen at least 1024 px wide';
const NO_JS_NOTICE = 'MOBAdemo needs JavaScript. Please enable it to view the demo.';

interface LifecycleSnapshot {
  loadingSeenBeforeShell: boolean;
  shellRenderedAfterLoading: boolean;
}

interface TrackedTransfer {
  url: string;
  mimeType: string;
  encodedDataLength: number;
}

function escapeRegExp(value: string): string {
  return value.replaceAll(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`);
}

function buildHostDetailPatterns(): RegExp[] {
  const patterns: RegExp[] = [
    /[A-Za-z]:[\\/](?:Users|Repo|Windows|Program Files)/i,
    /(?:^|[^\w./])\/(?:home|Users|root)\/[\w.-]+/,
    /\b(?:10\.\d{1,3}|192\.168|172\.(?:1[6-9]|2\d|3[01])|169\.254)\.\d{1,3}\.\d{1,3}\b/,
    /(?:localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1\]|\.local|\.internal|\.lan):\d{2,5}|:(?:5000|5001|5173|8080)\b/i,
  ];

  const identityNames = new Set<string>();
  const machine = hostname();
  if (machine.length >= 3) identityNames.add(machine);
  try {
    const user = userInfo().username;
    if (user.length >= 3) identityNames.add(user);
  } catch {
    // userInfo() may fail in restricted environments.
  }

  for (const name of identityNames) {
    const escaped = escapeRegExp(name);
    patterns.push(
      new RegExp(String.raw`(?:Users|home)[\\/]${escaped}(?![\w.-])`, 'i'),
      new RegExp(String.raw`\\\\${escaped}[\\/]`, 'i'),
      new RegExp(String.raw`://${escaped}(?![\w.-])`, 'i'),
      new RegExp(String.raw`(?<![\w.-])${escaped}\.(?:local|lan|internal)\b`, 'i'),
    );
  }

  return patterns;
}

const HOST_DETAIL_PATTERNS = buildHostDetailPatterns();

function isTextContentType(contentType: string | undefined): boolean {
  if (!contentType) return false;
  const mediaType = contentType.split(';', 1)[0].trim().toLowerCase();
  return (
    mediaType.startsWith('text/') ||
    mediaType === 'application/javascript' ||
    mediaType === 'application/json' ||
    mediaType === 'image/svg+xml'
  );
}

function assertNoHostLeak(label: string, text: string): void {
  for (const pattern of HOST_DETAIL_PATTERNS) {
    const match = pattern.exec(text);
    expect(match, `${label} exposed host detail "${match?.[0] ?? ''}" matching ${pattern}`).toBeNull();
  }
}

async function inspectHttpResponse(response: Response): Promise<void> {
  const url = response.url();
  const headers = response.headers();

  expect(headers['server'], `${url} must not send a Server header (FR-072)`).toBeUndefined();
  expect(headers['x-powered-by'], `${url} must not send X-Powered-By`).toBeUndefined();
  expect(headers['x-content-type-options'], `${url} must send nosniff`).toBe('nosniff');
  expect(headers['content-security-policy'], `${url} must send CSP`).toContain("style-src 'self'");

  for (const [name, value] of Object.entries(headers)) {
    assertNoHostLeak(`Header ${name} on ${url}`, value);
  }

  if (isTextContentType(headers['content-type']) && response.status() !== 204 && response.status() !== 304) {
    const body = await response.text();
    assertNoHostLeak(`Body of ${url}`, body);
  }
}

test.describe('MOBAdemo shell browser checks (slice 10)', () => {
  test.use({ extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.1' } });

  test('cold load shows loading state before rendering the shell (FR-052)', async ({ page }) => {
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') consoleErrors.push(msg.text());
    });
    page.on('pageerror', (err) => pageErrors.push(err.message));

    await page.addInitScript(() => {
      const state = {
        loadingSeenBeforeShell: false,
        shellRenderedAfterLoading: false,
      };
      (window as unknown as { __mobademoLifecycle?: typeof state }).__mobademoLifecycle = state;

      const record = () => {
        const loading = document.querySelector('output.app-message');
        const main = document.querySelector('main.app-shell');
        if (loading && loading.textContent?.includes('Loading MOBAdemo') && main === null) {
          state.loadingSeenBeforeShell = true;
        }
        if (state.loadingSeenBeforeShell && main !== null) {
          state.shellRenderedAfterLoading = true;
        }
      };

      const observer = new MutationObserver(record);
      observer.observe(document, { childList: true, subtree: true, characterData: true });
    });

    await page.goto('/');
    const main = page.getByRole('main');
    await expect(main).toBeVisible();
    await expect(page.getByRole('banner')).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'MOBAflow pages' })).toBeVisible();
    await expect(page.getByRole('region', { name: 'Messages' })).toBeVisible();
    await expect(page.getByRole('contentinfo', { name: 'Demo status' })).toBeVisible();

    await expect(page.getByRole('status')).toHaveCount(0);
    await expect(page.getByRole('alert')).toHaveCount(0);
    await expect(page.locator('body')).not.toContainText('MOBAdemo is being built');

    const lifecycle = await page.evaluate(
      () => (window as unknown as { __mobademoLifecycle?: LifecycleSnapshot }).__mobademoLifecycle,
    );
    expect(lifecycle).toEqual({
      loadingSeenBeforeShell: true,
      shellRenderedAfterLoading: true,
    });
    expect(consoleErrors).toEqual([]);
    expect(pageErrors).toEqual([]);
  });

  test('renders explicit no-JavaScript notice when JavaScript is disabled', async ({ browser }) => {
    const context = await browser.newContext({
      javaScriptEnabled: false,
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.2' },
    });
    try {
      const page = await context.newPage();
      await page.goto('/');
      const noscript = page.locator('noscript');
      expect(await noscript.textContent()).toBe(NO_JS_NOTICE);
      const box = await noscript.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.width).toBeGreaterThan(0);
      expect(box!.height).toBeGreaterThan(0);
      await expect(page.getByRole('main')).toHaveCount(0);
    } finally {
      await context.close();
    }
  });

  test('stays within first-visit and repeat-visit compressed transfer budgets (FR-057, FR-076, SC-006)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.3' },
    });
    try {
      const page = await context.newPage();
      const cdp = await context.newCDPSession(page);
      await cdp.send('Network.enable');

      const responsesByRequest = new Map<string, { url: string; mimeType: string; contentEncoding?: string; status: number }>();
      let currentTransfers: TrackedTransfer[] = [];

      cdp.on('Network.responseReceived', (event) => {
        const headerEntry = Object.entries(event.response.headers).find(
          ([key]) => key.toLowerCase() === 'content-encoding',
        );
        responsesByRequest.set(event.requestId, {
          url: event.response.url,
          mimeType: event.response.mimeType,
          contentEncoding: headerEntry?.[1],
          status: event.response.status,
        });
      });

      cdp.on('Network.loadingFinished', (event) => {
        const meta = responsesByRequest.get(event.requestId);
        if (!meta) return;
        currentTransfers.push({
          url: meta.url,
          mimeType: meta.mimeType,
          encodedDataLength: event.encodedDataLength,
        });
      });

      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('main')).toBeVisible();

      const firstVisitTransfers = currentTransfers;
      expect(firstVisitTransfers.length).toBeGreaterThanOrEqual(2);

      const firstVisitTotal = firstVisitTransfers.reduce((sum, item) => sum + item.encodedDataLength, 0);
      const scriptAndStyleBytes = firstVisitTransfers
        .filter((item) => /\.(?:mjs|js|css)(?:[?#]|$)/i.test(item.url))
        .reduce((sum, item) => sum + item.encodedDataLength, 0);

      expect(scriptAndStyleBytes).toBeGreaterThan(0);
      expect(scriptAndStyleBytes).toBeLessThanOrEqual(SCRIPT_STYLE_BUDGET_BYTES);
      expect(firstVisitTotal).toBeLessThanOrEqual(FIRST_VISIT_BUDGET_BYTES);

      const encodedAssets = [...responsesByRequest.values()].filter((r) =>
        /\.(?:mjs|js|css)(?:[?#]|$)/i.test(r.url),
      );
      expect(encodedAssets.length).toBeGreaterThan(0);
      for (const asset of encodedAssets) {
        expect(asset.status).toBe(200);
        expect(['br', 'gzip']).toContain(asset.contentEncoding);
      }

      // Repeat visit with warm browser cache in the same context.
      currentTransfers = [];
      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('main')).toBeVisible();

      const repeatVisitTotal = currentTransfers.reduce((sum, item) => sum + item.encodedDataLength, 0);
      expect(repeatVisitTotal).toBeLessThanOrEqual(REPEAT_VISIT_BUDGET_BYTES);
    } finally {
      await context.close();
    }
  });

  test('exposes zero host details or Server headers across all HTTP responses (FR-030, FR-072, SC-003)', async ({
    page,
  }) => {
    const responses: Response[] = [];
    page.on('response', (response) => {
      responses.push(response);
    });

    await page.goto('/', { waitUntil: 'networkidle' });
    await expect(page.getByRole('main')).toBeVisible();

    expect(responses.length).toBeGreaterThanOrEqual(2);
    for (const response of responses) {
      expect(response.status(), `Unexpected status for ${response.url()}`).toBeLessThan(400);
      await inspectHttpResponse(response);
    }

    for (const [path, expectedStatus] of [
      ['/health', 200],
      ['/robots.txt', 200],
      ['/api/runtime', 404],
    ] as const) {
      const apiResponse = await page.request.get(path);
      expect(apiResponse.status()).toBe(expectedStatus);
      const headers = apiResponse.headers();
      expect(headers['server'], `${path} must not send Server header`).toBeUndefined();
      expect(headers['x-powered-by'], `${path} must not send X-Powered-By`).toBeUndefined();
      for (const [name, value] of Object.entries(headers)) {
        assertNoHostLeak(`Header ${name} on ${path}`, value);
      }
      assertNoHostLeak(`Body of ${path}`, await apiResponse.text());
    }
  });

  test('renders full, compact, and narrow width modes with dismissible in-tab notice (FR-012, FR-033)', async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/');
    await expect(page.getByRole('main')).toBeVisible();

    const modeContainer = page.locator('[data-shell-mode]');
    await expect(modeContainer).toHaveAttribute('data-shell-mode', 'full');
    await expect(page.getByText(NARROW_NOTICE)).toHaveCount(0);

    const firstNavLabel = page.locator('.demo-nav-label').first();
    const fullLabelBox = await firstNavLabel.boundingBox();
    expect(fullLabelBox).not.toBeNull();
    expect(fullLabelBox!.width).toBeGreaterThan(20);

    const fullOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
    );
    expect(fullOverflow).toBe(true);

    // Compact mode (1024..1279 px)
    await page.setViewportSize({ width: 1100, height: 800 });
    await expect(modeContainer).toHaveAttribute('data-shell-mode', 'compact');
    await expect(page.getByText(NARROW_NOTICE)).toHaveCount(0);

    const compactSidebarBox = await page.locator('.demo-nav-sidebar').boundingBox();
    expect(compactSidebarBox).not.toBeNull();
    expect(compactSidebarBox!.width).toBeLessThanOrEqual(96);

    const compactOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
    );
    expect(compactOverflow).toBe(true);

    // Narrow mode (<1024 px): keeps 1024px desktop layout inside .demo-shell-viewport and shows dismissible notice.
    await page.setViewportSize({ width: 900, height: 800 });
    await expect(modeContainer).toHaveAttribute('data-shell-mode', 'narrow');

    const notice = page.locator('.demo-narrow-notice');
    await expect(notice).toBeVisible();
    await expect(notice).toContainText(NARROW_NOTICE);

    const noticeBox = await notice.boundingBox();
    expect(noticeBox).not.toBeNull();
    expect(noticeBox!.x).toBeGreaterThanOrEqual(0);
    expect(noticeBox!.x + noticeBox!.width).toBeLessThanOrEqual(900);

    const narrowMetrics = await page.evaluate(() => {
      const viewport = document.querySelector('.demo-shell-viewport');
      const desktop = document.querySelector('.demo-desktop-layout');
      return {
        docScrollWidth: document.documentElement.scrollWidth,
        docClientWidth: document.documentElement.clientWidth,
        viewportClientWidth: viewport?.clientWidth ?? 0,
        viewportScrollWidth: viewport?.scrollWidth ?? 0,
        desktopWidth: desktop instanceof HTMLElement ? desktop.offsetWidth : 0,
      };
    });
    expect(narrowMetrics.docScrollWidth).toBeLessThanOrEqual(narrowMetrics.docClientWidth);
    expect(narrowMetrics.desktopWidth).toBeGreaterThanOrEqual(1024);
    expect(narrowMetrics.viewportScrollWidth).toBeGreaterThanOrEqual(1024);
    expect(narrowMetrics.viewportScrollWidth).toBeGreaterThan(narrowMetrics.viewportClientWidth);

    await page.getByRole('button', { name: 'Dismiss narrow-window notice' }).click();
    await expect(page.getByText(NARROW_NOTICE)).toHaveCount(0);

    // Reload restores the notice because dismissal is tab-memory only (FR-012).
    await page.reload();
    await expect(page.getByRole('main')).toBeVisible();
    await expect(page.getByText(NARROW_NOTICE)).toBeVisible();
  });

  test('passes axe-core accessibility checks in both Dark (default) and Light themes (FR-021, SC-009)', async ({
    browser,
  }) => {
    // Emulate browser light preference to verify first visit still defaults to Dark (FR-021).
    const context = await browser.newContext({
      colorScheme: 'light',
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.4' },
    });
    try {
      const page = await context.newPage();
      await page.goto('/');
      await expect(page.getByRole('main')).toBeVisible();

      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('dark');
      const themeButton = page.getByRole('button', { name: 'Light theme' });
      await expect(themeButton).toHaveAttribute('aria-pressed', 'false');

      const darkAudit = await new AxeBuilder({ page }).analyze();

      await themeButton.click();
      await expect(themeButton).toHaveAttribute('aria-pressed', 'true');
      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('light');

      const lightAudit = await new AxeBuilder({ page }).analyze();

      expect(
        { dark: darkAudit.violations, light: lightAudit.violations },
        JSON.stringify({ dark: darkAudit.violations, light: lightAudit.violations }, null, 2),
      ).toEqual({ dark: [], light: [] });

      // Only the manual theme choice is persisted across reload (FR-007, FR-012).
      const storageKeys = await page.evaluate(() => Object.keys(localStorage));
      expect(storageKeys).toEqual(['mobademo-theme']);

      await page.reload();
      await expect(page.getByRole('main')).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('light');
    } finally {
      await context.close();
    }
  });
});
