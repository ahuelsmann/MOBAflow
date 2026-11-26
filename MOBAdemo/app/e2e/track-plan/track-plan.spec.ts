import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page, type Response } from '@playwright/test';
import { assertNoHostLeak, clickAtCenter } from '../helpers';

const INERT_HINT = 'Available in the MOBAflow desktop app';

async function snapshotMainWithoutHints(page: Page): Promise<string> {
  return page.evaluate(() => {
    const main = document.querySelector('main');
    if (!main) return '';
    const clone = main.cloneNode(true) as HTMLElement;
    clone.querySelectorAll('[role="tooltip"]').forEach((el) => el.remove());
    return clone.innerHTML;
  });
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
  test('opens Track Plan from navigation and renders prepared panes and SVG layout without overlap (US2 scenario 1)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      viewport: { width: 1280, height: 800 },
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

      // Zoom bar sits below the canvas frame and must not cover any part of /assets/track-plan.svg.
      const svgBox = await svgImage.boundingBox();
      const zoomBox = await zoomGroup.boundingBox();
      expect(svgBox).not.toBeNull();
      expect(zoomBox).not.toBeNull();
      expect(zoomBox!.y).toBeGreaterThanOrEqual(svgBox!.y + svgBox!.height);

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

  test('keeps every button and zoom slider on Track Plan inert with identical DOM snapshot (SC-004)', async ({
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
      const zoomGroup = main.getByRole('group', { name: 'Zoom controls' });
      const zoomSlider = zoomGroup.getByRole('slider', { name: 'Zoom' });

      const initialSnapshot = await snapshotMainWithoutHints(page);
      const initialStatusText = await page.getByRole('contentinfo', { name: 'Demo status' }).textContent();

      const buttons = main.locator('button');
      const count = await buttons.count();
      expect(count).toBe(43); // 15 toolbar + 24 catalogue + 2 zoom +/- + 2 context section buttons

      for (let i = 0; i < count; i++) {
        const button = buttons.nth(i);
        await expect(button).toHaveAttribute('aria-disabled', 'true');
        await expect(button).toHaveAccessibleDescription(INERT_HINT);
        await clickAtCenter(page, button);
        await expect(page.getByRole('tooltip')).toContainText(INERT_HINT);
        await page.keyboard.press('Escape');
      }

      // Drag/click and key interaction on the inert zoom slider must leave 100% intact.
      await expect(zoomSlider).toHaveAttribute('aria-disabled', 'true');
      await expect(zoomSlider).toHaveAccessibleDescription(INERT_HINT);
      const sliderBox = await zoomSlider.boundingBox();
      expect(sliderBox).not.toBeNull();
      await page.mouse.click(sliderBox!.x + sliderBox!.width * 0.85, sliderBox!.y + sliderBox!.height / 2);
      await zoomSlider.focus();
      await expect(zoomGroup.getByRole('tooltip')).toHaveText(INERT_HINT);
      await page.keyboard.press('ArrowRight');
      await page.keyboard.press('Escape');
      await page.locator('body').focus();

      await expect(zoomSlider).toHaveValue('100');
      await expect(zoomGroup).toContainText('100%');
      await expect(trackPlanEntry).toHaveAttribute('aria-current', 'page');
      expect(await snapshotMainWithoutHints(page)).toBe(initialSnapshot);
      expect(await page.getByRole('contentinfo', { name: 'Demo status' }).textContent()).toBe(initialStatusText);
    } finally {
      await context.close();
    }
  });

  test('passes axe-core checks in Dark and Light, fits compact width, and honours reduced motion (FR-021, FR-046, SC-009)', async ({
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

      // Compact width mode (1100 px) keeps zero horizontal overflow and zero zoom-bar overlap.
      await page.setViewportSize({ width: 1100, height: 800 });
      await expect(page.locator('[data-shell-mode]')).toHaveAttribute('data-shell-mode', 'compact');
      const compactNoOverflow = await page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      );
      expect(compactNoOverflow).toBe(true);
      const compactSvgBox = await page
        .getByRole('img', { name: 'Example model railway layout on Track Plan canvas' })
        .boundingBox();
      const compactZoomBox = await page.getByRole('group', { name: 'Zoom controls' }).boundingBox();
      expect(compactSvgBox).not.toBeNull();
      expect(compactZoomBox).not.toBeNull();
      expect(compactZoomBox!.y).toBeGreaterThanOrEqual(compactSvgBox!.y + compactSvgBox!.height);
    } finally {
      await context.close();
    }

    const reduced = await browser.newContext({
      reducedMotion: 'reduce',
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.54' },
    });
    try {
      const page = await reduced.newPage();
      await openTrackPlan(page);
      const animated = await page.evaluate(() => {
        const root = document.querySelector('main .track-plan');
        if (!root) return ['missing .track-plan'];
        const bad: string[] = [];
        for (const el of [root, ...root.querySelectorAll('*')]) {
          const cs = getComputedStyle(el);
          if (cs.transitionDuration !== '0s') bad.push(`${el.tagName}.${el.className}: ${cs.transitionDuration}`);
          if (cs.animationName !== 'none') bad.push(`${el.tagName}.${el.className}: ${cs.animationName}`);
        }
        return bad;
      });
      expect(animated).toEqual([]);
    } finally {
      await reduced.close();
    }
  });
});
