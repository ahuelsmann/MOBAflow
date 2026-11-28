import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';
import { attachTransferRecorder } from '../helpers';

const FIRST_VISIT_BUDGET_BYTES = 1_500_000;
const SCRIPT_STYLE_BUDGET_BYTES = 200_000;
const REPEAT_VISIT_BUDGET_BYTES = 100_000;

const DEMO_PAGES = ['Train Control', 'Workflows', 'Track Plan', 'Signal Box'] as const;

async function openDemoPage(page: Page, title: (typeof DEMO_PAGES)[number]): Promise<void> {
  const nav = page.getByRole('navigation', { name: 'MOBAflow pages' });
  const entry = nav.getByRole('button', { name: title });
  await expect(entry).toBeEnabled();
  await expect(entry).not.toHaveAttribute('aria-disabled', 'true');
  await entry.click();
  await expect(entry).toHaveAttribute('aria-current', 'page');
  await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible();
  await page.waitForLoadState('networkidle');
}

test.describe('MOBAdemo cross-page browser checks (slice 19)', () => {
  test('SC-007: round 1 loads only unloaded static files once and round 2 sends zero requests', async ({ browser }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.181' },
    });
    try {
      const page = await context.newPage();
      const initialUrls = new Set<string>();
      let phase: 'initial' | 'round1' | 'round2' = 'initial';
      const round1Requests: string[] = [];
      const round2Requests: string[] = [];
      const badStatuses: string[] = [];

      page.on('request', (request) => {
        const url = request.url();
        if (phase === 'initial') initialUrls.add(url);
        else if (phase === 'round1') round1Requests.push(url);
        else round2Requests.push(url);
      });
      page.on('response', (response) => {
        if (phase !== 'initial' && response.status() >= 400) {
          badStatuses.push(`${response.status()} ${response.url()}`);
        }
      });

      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { level: 1, name: 'Train Control' })).toBeVisible();

      const nav = page.getByRole('navigation', { name: 'MOBAflow pages' });
      await expect(nav.locator('button')).toHaveCount(22);
      const mainButtonCounts = { 'Workflows': 19, 'Track Plan': 43, 'Signal Box': 24, 'Train Control': 40 };

      const origin = new URL(page.url()).origin;
      phase = 'round1';
      for (const title of ['Workflows', 'Track Plan', 'Signal Box', 'Train Control'] as const) {
        await openDemoPage(page, title);
        await expect(page.getByRole('main').locator('button')).toHaveCount(mainButtonCounts[title]);
      }

      expect(badStatuses).toEqual([]);
      for (const url of round1Requests) {
        expect(url.startsWith(`${origin}/assets/`), `Non-static request in round 1: ${url}`).toBe(true);
        expect(initialUrls.has(url), `Duplicate initial asset requested in round 1: ${url}`).toBe(false);
      }
      expect(new Set(round1Requests).size).toBe(round1Requests.length);

      phase = 'round2';
      for (const title of ['Workflows', 'Track Plan', 'Signal Box', 'Train Control'] as const) {
        await openDemoPage(page, title);
      }

      expect(round2Requests).toEqual([]);
    } finally {
      await context.close();
    }
  });

  test('FR-021 & US3 S2: navigates all four demo pages and preserves page & slider on theme toggle', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.182' },
    });
    try {
      const page = await context.newPage();
      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { level: 1, name: 'Train Control' })).toBeVisible();

      const slider = page.getByRole('main').getByRole('slider', { name: 'Speed' });
      await slider.fill('64');
      await expect(slider).toHaveAttribute('aria-valuetext', 'Step 64');

      const themeToggle = page.getByRole('button', { name: 'Light theme' });
      await themeToggle.click();
      await expect(themeToggle).toHaveAttribute('aria-pressed', 'true');
      await expect(slider).toHaveValue('64');
      await expect(slider).toHaveAttribute('aria-valuetext', 'Step 64');

      await themeToggle.click();
      await expect(themeToggle).toHaveAttribute('aria-pressed', 'false');
      await expect(slider).toHaveValue('64');

      await expect(
        page.getByRole('navigation', { name: 'MOBAflow pages' }).getByRole('button', { name: 'Locomotives' }),
      ).toHaveAttribute('aria-disabled', 'true');

      for (const title of ['Workflows', 'Track Plan', 'Signal Box'] as const) {
        await openDemoPage(page, title);
        await themeToggle.click();
        await expect(themeToggle).toHaveAttribute('aria-pressed', 'true');
        await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible();
        await expect(
          page.getByRole('navigation', { name: 'MOBAflow pages' }).getByRole('button', { name: title }),
        ).toHaveAttribute('aria-current', 'page');
        await themeToggle.click();
        await expect(themeToggle).toHaveAttribute('aria-pressed', 'false');
        await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible();
      }
    } finally {
      await context.close();
    }
  });

  test('SC-009: passes axe-core on all four pages in both themes at full, compact, and narrow widths', async ({ browser }) => {
    test.setTimeout(90_000);
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.183' },
    });
    try {
      const page = await context.newPage();
      await page.goto('/', { waitUntil: 'networkidle' });

      const findings: Array<{ width: number; page: string; theme: string; violations: unknown[] }> = [];
      const lightButton = page.getByRole('button', { name: 'Light theme' });

      for (const width of [1280, 1100, 900]) {
        await page.setViewportSize({ width, height: 800 });
        for (const theme of ['dark', 'light']) {
          await expect(lightButton).toHaveAttribute('aria-pressed', String(theme === 'light'));
          for (const title of DEMO_PAGES) {
            await openDemoPage(page, title);
            const audit = await new AxeBuilder({ page }).analyze();
            if (audit.violations.length > 0) {
              findings.push({ width, page: title, theme, violations: audit.violations });
            }
          }
          await lightButton.click();
        }
      }

      expect(findings, JSON.stringify(findings, null, 2)).toEqual([]);
    } finally {
      await context.close();
    }
  });

  test('SC-006: stays within compressed transfer budgets with all four demo pages enabled', async ({ browser }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.184' },
    });
    try {
      const page = await context.newPage();
      const recorder = await attachTransferRecorder(context, page);

      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { level: 1, name: 'Train Control' })).toBeVisible();

      const firstVisitTransfers = recorder.getTransfers();
      const firstVisitTotal = firstVisitTransfers.reduce((sum, item) => sum + item.encodedDataLength, 0);
      const scriptAndStyleBytes = firstVisitTransfers
        .filter((item) => /\.(?:mjs|js|css)(?:[?#]|$)/i.test(item.url))
        .reduce((sum, item) => sum + item.encodedDataLength, 0);

      expect(scriptAndStyleBytes).toBeGreaterThan(0);
      expect(scriptAndStyleBytes).toBeLessThanOrEqual(SCRIPT_STYLE_BUDGET_BYTES);
      expect(firstVisitTotal).toBeLessThanOrEqual(FIRST_VISIT_BUDGET_BYTES);

      recorder.resetTransfers();
      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('main')).toBeVisible();
      const repeatVisitTotal = recorder.getTransfers().reduce((sum, item) => sum + item.encodedDataLength, 0);
      expect(repeatVisitTotal).toBeLessThanOrEqual(REPEAT_VISIT_BUDGET_BYTES);
    } finally {
      await context.close();
    }
  });
});
