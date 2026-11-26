import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Browser, type BrowserContext, type BrowserContextOptions, type Page } from '@playwright/test';
import { clickAtCenter } from '../helpers';

const INERT_HINT = 'Available in the MOBAflow desktop app';

async function newClientContext(
  browser: Browser,
  clientIndex: number,
  options: BrowserContextOptions = {},
): Promise<BrowserContext> {
  return browser.newContext({
    ...options,
    extraHTTPHeaders: {
      ...options.extraHTTPHeaders,
      'X-Forwarded-For': `198.51.100.${100 + clientIndex}`,
    },
  });
}

async function openSignalBox(page: Page): Promise<void> {
  await page.goto('/');
  await expect(page.getByRole('main')).toBeVisible();
  const navButton = page.getByRole('button', { name: 'Signal Box' });
  await expect(navButton).toBeEnabled();
  await expect(navButton).not.toHaveAttribute('aria-disabled', 'true');
  await navButton.click();
  await expect(navButton).toHaveAttribute('aria-current', 'page');
  await expect(page.getByRole('heading', { name: 'Signal Box' })).toBeVisible();
}

test.describe('Signal Box page browser checks (slice 16)', () => {
  test('renders prepared Signal Box schematic, aspects, multiplexer config, and footer (User Story 2 Scenario 2)', async ({
    browser,
  }) => {
    const context = await newClientContext(browser, 1);
    try {
      const page = await context.newPage();
      const consoleErrors: string[] = [];
      const pageErrors: string[] = [];
      page.on('console', (msg) => {
        if (msg.type() === 'error') consoleErrors.push(msg.text());
      });
      page.on('pageerror', (err) => pageErrors.push(err.message));

      await openSignalBox(page);

      const toolbox = page.getByRole('region', { name: 'Elements' });
      for (const label of ['Straight', 'Curve', 'Turnout', 'Detector', 'Ks Signal', 'Fit to view']) {
        await expect(toolbox.getByRole('button', { name: label })).toBeVisible();
      }
      await expect(toolbox.getByText('Grid: 20 × 12 (60 px)')).toBeVisible();

      const canvas = page.getByRole('region', { name: 'Signal box canvas' });
      const svg = canvas.locator('svg.signal-box-svg');
      await expect(svg).toBeVisible();
      await expect(svg).toHaveAttribute('viewBox', '0 120 600 360');
      await expect(svg).toContainText('W1');
      await expect(svg).toContainText('W2');
      await expect(svg).toContainText('Block 1');
      await expect(svg).toContainText('G2HBFA1');
      await expect(svg).toContainText('Ks 1');

      const stageBox = await canvas.locator('.signal-box-canvas-stage').boundingBox();
      expect(stageBox?.width ?? 0).toBeGreaterThanOrEqual(480);

      const detectorButtonBox = await canvas.locator('[data-element-id="e5"] button').boundingBox();
      expect(detectorButtonBox?.width ?? 0).toBeGreaterThanOrEqual(24);
      expect(detectorButtonBox?.height ?? 0).toBeGreaterThanOrEqual(24);

      const labelBox = await svg.locator('[data-label-for="e5"] rect').boundingBox();
      const nodeBox = await svg.locator('.signal-box-detector-node').boundingBox();
      expect(labelBox).not.toBeNull();
      expect(nodeBox).not.toBeNull();
      const overlapsDetector =
        labelBox!.x < nodeBox!.x + nodeBox!.width &&
        labelBox!.x + labelBox!.width > nodeBox!.x &&
        labelBox!.y < nodeBox!.y + nodeBox!.height &&
        labelBox!.y + labelBox!.height > nodeBox!.y;
      expect(overlapsDetector).toBe(false);

      const signalButton = canvas.getByRole('button', { name: 'Ks signal G2HBFA1, selected, Ks 1' });
      await expect(signalButton).toHaveAttribute('aria-pressed', 'true');

      const contextPane = page.getByRole('region', { name: 'Context' });
      await expect(contextPane.getByText('G2HBFA1')).toBeVisible();
      await expect(contextPane.getByText('System: Ks')).toBeVisible();

      const ks1Tile = contextPane.getByRole('button', { name: /Ks 1/ });
      await expect(ks1Tile).toHaveAttribute('aria-pressed', 'true');
      await expect(ks1Tile.getByText('Prepared')).toBeVisible();
      await expect(ks1Tile.locator('svg.signal-aspect-glyph')).toBeVisible();

      for (const aspectName of ['Hp 0', 'Ks 1', 'Ks 2', 'Sh 1']) {
        const tile = contextPane.getByRole('button', { name: new RegExp(aspectName) });
        await expect(tile).toBeVisible();
        await expect(tile.locator('svg.signal-aspect-glyph')).toBeVisible();
      }

      const mux = contextPane.getByLabel('Multiplexer configuration');
      await expect(mux).toContainText('5229');
      await expect(mux).toContainText('201');
      await expect(mux).toContainText('Main signal');
      await expect(mux).toContainText('4046');
      await expect(mux).toContainText('Distant signal');
      await expect(mux).toContainText('4040');

      const summary = page.getByRole('region', { name: 'Signal box summary' });
      await expect(summary).toContainText('12 elements · Signal G2HBFA1 (Ks 1)');

      expect(consoleErrors).toEqual([]);
      expect(pageErrors).toEqual([]);
    } finally {
      await context.close();
    }
  });

  test('keeps toolbox, canvas elements, toolbar and aspect tiles inert with hints (SC-004)', async ({
    browser,
  }) => {
    const context = await newClientContext(browser, 2);
    try {
      const page = await context.newPage();
      await openSignalBox(page);

      const snapshotWithoutHints = () =>
        page.locator('main.app-shell').evaluate((main) => {
          const clone = main.cloneNode(true) as HTMLElement;
          clone.querySelectorAll('.demo-inert-hint').forEach((node) => node.remove());
          return clone.innerHTML;
        });

      const initialDom = await snapshotWithoutHints();

      const buttons = page.getByRole('main').locator('button');
      const count = await buttons.count();
      expect(count).toBe(24); // 2 toolbar + 6 toolbox + 12 grid elements + 4 aspect tiles

      for (let i = 0; i < count; i++) {
        const control = buttons.nth(i);
        await expect(control).toHaveAttribute('aria-disabled', 'true');
        await expect(control).toHaveAccessibleDescription(INERT_HINT);
        await clickAtCenter(page, control);
        const tooltipId = await control.getAttribute('aria-describedby');
        expect(tooltipId).toBeTruthy();
        await expect(page.locator(`#${tooltipId}`)).toBeVisible();
        await page.keyboard.press('Escape');
      }

      const ks1Tile = page.getByRole('region', { name: 'Context' }).getByRole('button', { name: /Ks 1/ });
      await expect(ks1Tile).toHaveAttribute('aria-pressed', 'true');
      expect(await snapshotWithoutHints()).toBe(initialDom);
    } finally {
      await context.close();
    }
  });

  test('passes axe-core in Dark and Light, fits compact width, and preserves Signal Box across theme switch (SC-009)', async ({
    browser,
  }) => {
    const context = await newClientContext(browser, 3);
    try {
      const page = await context.newPage();
      await openSignalBox(page);

      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('dark');
      const darkAudit = await new AxeBuilder({ page }).analyze();

      const themeButton = page.getByRole('button', { name: 'Light theme' });
      await themeButton.click();
      await expect(themeButton).toHaveAttribute('aria-pressed', 'true');
      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('light');

      await expect(page.getByRole('button', { name: 'Signal Box' })).toHaveAttribute('aria-current', 'page');
      await expect(page.getByRole('heading', { name: 'Signal Box' })).toBeVisible();

      const lightAudit = await new AxeBuilder({ page }).analyze();
      expect(
        { dark: darkAudit.violations, light: lightAudit.violations },
        JSON.stringify({ dark: darkAudit.violations, light: lightAudit.violations }, null, 2),
      ).toEqual({ dark: [], light: [] });

      await page.setViewportSize({ width: 1100, height: 800 });
      await expect(page.locator('[data-shell-mode]')).toHaveAttribute('data-shell-mode', 'compact');
      const compactNoOverflow = await page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      );
      expect(compactNoOverflow).toBe(true);
    } finally {
      await context.close();
    }
  });

  test('renders flashing aspect steady with "Flashing" text and zero animation under reduced motion (FR-046)', async ({
    browser,
  }) => {
    const context = await newClientContext(browser, 4, { reducedMotion: 'reduce' });
    try {
      const page = await context.newPage();
      await openSignalBox(page);

      const ks2Tile = page.getByRole('region', { name: 'Context' }).getByRole('button', { name: /Ks 2/ });
      await expect(ks2Tile.getByText('Flashing')).toBeVisible();

      const animationName = await ks2Tile.locator('.signal-lamp-flashing').evaluate(
        (element) => getComputedStyle(element).animationName,
      );
      expect(animationName).toBe('none');

      const audit = await new AxeBuilder({ page }).analyze();
      expect(audit.violations, JSON.stringify(audit.violations, null, 2)).toEqual([]);
    } finally {
      await context.close();
    }
  });
});
