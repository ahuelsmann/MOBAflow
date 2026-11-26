import { hostname, userInfo } from 'node:os';
import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page, type Response } from '@playwright/test';

const INERT_HINT = 'Available in the MOBAflow desktop app';

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

function assertNoHostLeak(label: string, text: string): void {
  for (const pattern of HOST_DETAIL_PATTERNS) {
    const match = pattern.exec(text);
    expect(match, `${label} exposed host detail "${match?.[0] ?? ''}" matching ${pattern}`).toBeNull();
  }
}

async function openTrackPlan(page: Page): Promise<void> {
  await page.goto('/', { waitUntil: 'networkidle' });
  await expect(page.getByRole('main')).toBeVisible();

  const nav = page.getByRole('navigation', { name: 'MOBAflow pages' });
  const trackPlanEntry = nav.getByRole('button', { name: 'Track Plan' });
  await trackPlanEntry.click();
  await expect(trackPlanEntry).toHaveAttribute('aria-current', 'page');
  await expect(page.getByRole('heading', { level: 1, name: 'Track Plan' })).toBeVisible();
}

test.describe('Track Plan browser checks (slice 15)', () => {
  test('opens Track Plan from navigation and renders prepared panes and SVG layout (US2 scenario 1)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.51' },
    });
    try {
      const page = await context.newPage();
      const consoleErrors: string[] = [];
      const pageErrors: string[] = [];
      const responses: Response[] = [];

      page.on('console', (msg) => {
        if (msg.type() === 'error') consoleErrors.push(msg.text());
      });
      page.on('pageerror', (err) => pageErrors.push(err.message));
      page.on('response', (response) => {
        responses.push(response);
      });

      await openTrackPlan(page);

      const main = page.getByRole('main');
      const toolbar = main.getByRole('toolbar', { name: 'Track Plan toolbar' });
      await expect(toolbar).toBeVisible();
      for (const label of ['Undo', 'Redo', 'Fit', '100%', 'Snap', 'Grid']) {
        await expect(toolbar.getByRole('button', { name: label })).toBeVisible();
      }

      const catalogue = main.getByRole('region', { name: 'Piko A-Track catalogue' });
      await expect(catalogue).toBeVisible();
      for (const itemLabel of ['G231 (230.93 mm)', 'R1 (360 mm / 30°)', 'WL Left Turnout 15°', 'DKW Double Slip Switch']) {
        await expect(catalogue.getByRole('button', { name: itemLabel })).toBeVisible();
      }

      const canvas = main.getByRole('region', { name: 'Track Plan canvas' });
      const svgImage = canvas.getByRole('img', { name: 'Example model railway layout on Track Plan canvas' });
      await expect(svgImage).toBeVisible();

      const imageMetrics = await svgImage.evaluate((img: HTMLImageElement) => ({
        complete: img.complete,
        naturalWidth: img.naturalWidth,
        naturalHeight: img.naturalHeight,
      }));
      expect(imageMetrics.complete).toBe(true);
      expect(imageMetrics.naturalWidth).toBeGreaterThan(0);
      expect(imageMetrics.naturalHeight).toBeGreaterThan(0);

      const svgResponse = responses.find((r) => r.url().endsWith('/assets/track-plan.svg'));
      expect(svgResponse, 'Expected /assets/track-plan.svg HTTP response').toBeDefined();
      expect(svgResponse!.status()).toBe(200);
      expect(svgResponse!.headers()['content-type']).toContain('image/svg+xml');
      assertNoHostLeak('/assets/track-plan.svg', await svgResponse!.text());

      const zoomGroup = canvas.getByRole('group', { name: 'Zoom controls' });
      const zoomSlider = zoomGroup.getByRole('slider', { name: 'Zoom' });
      await expect(zoomSlider).toHaveValue('100');
      await expect(zoomGroup).toContainText('100%');

      const contextPane = main.getByRole('complementary', { name: 'Context properties' });
      await expect(contextPane).toBeVisible();
      await expect(contextPane).toContainText('MOBAflow example layout');
      await expect(contextPane).toContainText('H0 (1:87)');
      await expect(contextPane).toContainText('Piko A-Track');
      await expect(contextPane).toContainText('12');
      await expect(contextPane).toContainText('11');

      const footer = main.getByRole('region', { name: 'Track Plan status' });
      await expect(footer).toContainText('Ready — Example layout loaded');
      await expect(footer).toContainText('X: 0 mm, Y: 0 mm');
      assertNoHostLeak('Track Plan page text', await main.innerText());

      expect(consoleErrors).toEqual([]);
      expect(pageErrors).toEqual([]);
    } finally {
      await context.close();
    }
  });

  test('keeps toolbar, catalogue, zoom slider and context controls inert with hints (SC-004)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.52' },
    });
    try {
      const page = await context.newPage();
      await openTrackPlan(page);

      const main = page.getByRole('main');
      const nav = page.getByRole('navigation', { name: 'MOBAflow pages' });
      const trackPlanEntry = nav.getByRole('button', { name: 'Track Plan' });
      const toolbar = main.getByRole('toolbar', { name: 'Track Plan toolbar' });
      const snap = toolbar.getByRole('button', { name: 'Snap' });
      const grid = toolbar.getByRole('button', { name: 'Grid' });
      const catalogueButton = main.getByRole('region', { name: 'Piko A-Track catalogue' }).getByRole('button', {
        name: 'G231 (230.93 mm)',
      });
      const createSection = main
        .getByRole('complementary', { name: 'Context properties' })
        .getByRole('button', { name: 'Create Section' });
      const zoomGroup = main.getByRole('group', { name: 'Zoom controls' });
      const zoomSlider = zoomGroup.getByRole('slider', { name: 'Zoom' });
      const zoomIn = zoomGroup.getByRole('button', { name: 'Zoom in' });

      await expect(snap).toHaveAttribute('aria-disabled', 'true');
      await expect(snap).toHaveAccessibleDescription(INERT_HINT);
      await expect(grid).toHaveAttribute('aria-disabled', 'true');
      await expect(catalogueButton).toHaveAttribute('aria-disabled', 'true');
      await expect(createSection).toHaveAttribute('aria-disabled', 'true');
      await expect(zoomSlider).toHaveAttribute('aria-disabled', 'true');
      await expect(zoomSlider).toHaveAccessibleDescription(INERT_HINT);

      await snap.click({ force: true });
      await expect(toolbar.getByRole('tooltip', { name: INERT_HINT })).toBeVisible();
      await page.keyboard.press('Escape');

      await grid.click({ force: true });
      await catalogueButton.click({ force: true });
      await createSection.click({ force: true });
      await zoomIn.click({ force: true });

      // Drag/click and key interaction on the inert zoom slider must leave 100% intact.
      const sliderBox = await zoomSlider.boundingBox();
      expect(sliderBox).not.toBeNull();
      await page.mouse.click(sliderBox!.x + sliderBox!.width * 0.85, sliderBox!.y + sliderBox!.height / 2);
      await zoomSlider.focus();
      await expect(zoomGroup.getByRole('tooltip')).toHaveText(INERT_HINT);
      await page.keyboard.press('ArrowRight');

      await expect(zoomSlider).toHaveValue('100');
      await expect(zoomGroup).toContainText('100%');
      await expect(snap).toHaveAttribute('aria-pressed', 'true');
      await expect(grid).toHaveAttribute('aria-pressed', 'false');
      await expect(main.getByRole('complementary', { name: 'Context properties' })).toContainText('No selection');
      await expect(trackPlanEntry).toHaveAttribute('aria-current', 'page');
    } finally {
      await context.close();
    }
  });

  test('passes axe-core checks in Dark and Light themes and keeps Track Plan open across theme toggle (FR-021, SC-009)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      colorScheme: 'light',
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.53' },
    });
    try {
      const page = await context.newPage();
      await openTrackPlan(page);

      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('dark');
      const darkAudit = await new AxeBuilder({ page }).analyze();

      const themeButton = page.getByRole('button', { name: 'Light theme' });
      await themeButton.click();
      await expect(themeButton).toHaveAttribute('aria-pressed', 'true');
      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('light');

      const trackPlanEntry = page
        .getByRole('navigation', { name: 'MOBAflow pages' })
        .getByRole('button', { name: 'Track Plan' });
      await expect(trackPlanEntry).toHaveAttribute('aria-current', 'page');
      await expect(page.getByRole('heading', { level: 1, name: 'Track Plan' })).toBeVisible();

      const lightAudit = await new AxeBuilder({ page }).analyze();
      expect(
        { dark: darkAudit.violations, light: lightAudit.violations },
        JSON.stringify({ dark: darkAudit.violations, light: lightAudit.violations }, null, 2),
      ).toEqual({ dark: [], light: [] });
    } finally {
      await context.close();
    }
  });
});
