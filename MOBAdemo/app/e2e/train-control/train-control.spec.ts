import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Browser, type BrowserContextOptions, type Page } from '@playwright/test';

const FIRST_VISIT_BUDGET_BYTES = 1_500_000;
const FIRST_VISIT_MAX_REQUESTS = 40;
const SCRIPT_STYLE_BUDGET_BYTES = 200_000;
const INERT_HINT = 'Available in the MOBAflow desktop app';

async function openWithForwardedIp(
  browser: Browser,
  ip: string,
  options?: BrowserContextOptions,
) {
  const context = await browser.newContext({
    ...options,
    extraHTTPHeaders: {
      ...options?.extraHTTPHeaders,
      'X-Forwarded-For': ip,
    },
  });
  const page = await context.newPage();
  return { context, page };
}

async function snapshotMainWithoutInertHints(page: Page): Promise<string> {
  return page.evaluate(() => {
    const main = document.querySelector('main');
    if (!main) return '';
    const clone = main.cloneNode(true) as HTMLElement;
    clone.querySelectorAll('.demo-inert-hint').forEach((el) => el.remove());
    return clone.innerHTML;
  });
}

async function snapshotMainWithoutSlider(page: Page): Promise<string> {
  return page.evaluate(() => {
    const main = document.querySelector('main');
    if (!main) return '';
    const clone = main.cloneNode(true) as HTMLElement;
    clone.querySelector('[aria-label="Speed control"]')?.remove();
    return clone.innerHTML;
  });
}

test.describe('Train Control browser checks (slice 13)', () => {
  test('smoke: lands on Train Control with BR 211, all panels visible, 0 errors, within transfer budget (FR-032, FR-036, SC-006)', async ({
    browser,
  }) => {
    const { context, page } = await openWithForwardedIp(browser, '198.51.100.10');
    try {
      const consoleErrors: string[] = [];
      const pageErrors: string[] = [];
      page.on('console', (msg) => {
        if (msg.type() === 'error') consoleErrors.push(msg.text());
      });
      page.on('pageerror', (err) => pageErrors.push(err.message));

      const cdp = await context.newCDPSession(page);
      await cdp.send('Network.enable');
      const responsesById = new Map<string, { url: string; contentEncoding?: string; status: number }>();
      const transfers: { url: string; encodedDataLength: number }[] = [];

      cdp.on('Network.responseReceived', (event) => {
        const encoding = Object.entries(event.response.headers).find(
          ([key]) => key.toLowerCase() === 'content-encoding',
        )?.[1];
        responsesById.set(event.requestId, {
          url: event.response.url,
          contentEncoding: encoding,
          status: event.response.status,
        });
      });
      cdp.on('Network.loadingFinished', (event) => {
        const meta = responsesById.get(event.requestId);
        if (meta) {
          transfers.push({ url: meta.url, encodedDataLength: event.encodedDataLength });
        }
      });

      await page.goto('/', { waitUntil: 'networkidle' });
      const main = page.getByRole('main');
      await expect(main).toBeVisible();

      await expect(page.getByRole('button', { name: 'Train Control' })).toHaveAttribute('aria-current', 'page');
      await expect(main.getByRole('heading', { name: 'Train Control' })).toBeVisible();
      await expect(main.getByRole('button', { name: 'BR 211' })).toHaveAttribute('aria-pressed', 'true');

      const speedometer = main.getByRole('region', { name: 'Speedometer' });
      await expect(speedometer).toContainText('82 km/h');
      await expect(speedometer).toContainText('Scale: 0 to 400 km/h');
      await expect(speedometer.getByRole('slider', { name: 'Speed' })).toHaveValue('63');
      await expect(speedometer).toContainText('Step 63');

      const current = main.getByRole('region', { name: 'Track current' });
      await expect(current).toContainText('0.42 A / 3.0 A');
      await expect(current).toContainText('Track voltage: 18.1 V');

      const cab = main.getByRole('region', { name: 'Cab controls' });
      await expect(cab.getByRole('button', { name: /Brake/ })).toHaveAttribute('aria-pressed', 'false');
      await expect(cab.getByRole('button', { name: /Door release/ })).toHaveAttribute('aria-pressed', 'false');
      await expect(cab.locator('.tc-cab-status')).toContainText('Door blocking');
      await expect(cab.locator('.tc-cab-status')).toContainText('Active');

      const timetable = main.getByRole('region', { name: 'Timetable' });
      await expect(timetable).toContainText('Regional service RB 21');
      await expect(timetable.locator('[aria-current="step"]')).toContainText('Riverside');
      await expect(timetable.locator('[aria-current="step"]')).toContainText('Current stop');

      const functions = main.getByRole('region', { name: 'Functions' });
      await expect(functions.getByRole('button')).toHaveCount(32);

      const totalBytes = transfers.reduce((sum, item) => sum + item.encodedDataLength, 0);
      const scriptAndStyleBytes = transfers
        .filter((item) => /\.(?:mjs|js|css)(?:[?#]|$)/i.test(item.url))
        .reduce((sum, item) => sum + item.encodedDataLength, 0);

      expect(transfers.length).toBeLessThanOrEqual(FIRST_VISIT_MAX_REQUESTS);
      expect(scriptAndStyleBytes).toBeGreaterThan(0);
      expect(scriptAndStyleBytes).toBeLessThanOrEqual(SCRIPT_STYLE_BUDGET_BYTES);
      expect(totalBytes).toBeLessThanOrEqual(FIRST_VISIT_BUDGET_BYTES);

      for (const asset of [...responsesById.values()].filter((r) => /\.(?:mjs|js|css)(?:[?#]|$)/i.test(r.url))) {
        expect(asset.status).toBe(200);
        expect(['br', 'gzip']).toContain(asset.contentEncoding);
      }

      expect(consoleErrors).toEqual([]);
      expect(pageErrors).toEqual([]);
    } finally {
      await context.close();
    }
  });

  test('SC-004: activating every inert control on Train Control changes nothing except showing its hint', async ({
    browser,
  }) => {
    const { context, page } = await openWithForwardedIp(browser, '198.51.100.11');
    try {
      await page.goto('/', { waitUntil: 'networkidle' });
      const main = page.getByRole('main');
      await expect(main).toBeVisible();

      const search = main.getByRole('searchbox', { name: 'Search by series' });
      await expect(search).toHaveAttribute('readonly');
      await expect(search).toHaveAttribute('aria-disabled', 'true');
      await expect(search).toHaveAccessibleDescription(INERT_HINT);

      const initialSnapshot = await snapshotMainWithoutInertHints(page);
      const initialStatusText = await page.getByRole('contentinfo', { name: 'Demo status' }).textContent();

      const buttons = main.locator('button');
      const count = await buttons.count();
      expect(count).toBe(40); // stop + 2 locos + 3 drive settings + 2 cab toggles + 32 functions

      for (let i = 0; i < count; i++) {
        const button = buttons.nth(i);
        await expect(button).toHaveAttribute('aria-disabled', 'true');
        await expect(button).toHaveAccessibleDescription(INERT_HINT);
        // FR-035 buttons intentionally carry aria-disabled="true", which blocks Playwright's enabled check without force.
        await button.click({ force: true });
        await expect(page.getByRole('tooltip')).toContainText(INERT_HINT);
        await page.keyboard.press('Escape');
      }

      expect(await snapshotMainWithoutInertHints(page)).toBe(initialSnapshot);
      expect(await page.getByRole('contentinfo', { name: 'Demo status' }).textContent()).toBe(initialStatusText);
    } finally {
      await context.close();
    }
  });

  test('SC-005 & FR-021: speed slider changes only itself and Step X text; theme switch preserves page and slider', async ({
    browser,
  }) => {
    const { context, page } = await openWithForwardedIp(browser, '198.51.100.12');
    try {
      await page.goto('/', { waitUntil: 'networkidle' });
      const main = page.getByRole('main');
      const slider = main.getByRole('slider', { name: 'Speed' });
      const speedControl = main.getByRole('group', { name: 'Speed control' });

      await expect(slider).toHaveValue('63');
      await expect(slider).toHaveAttribute('aria-valuetext', 'Step 63');

      const unchangedMain = await snapshotMainWithoutSlider(page);
      const unchangedStatus = await page.getByRole('contentinfo', { name: 'Demo status' }).textContent();

      for (const step of [0, 84, 126]) {
        await slider.fill(String(step));
        await expect(slider).toHaveValue(String(step));
        await expect(slider).toHaveAttribute('aria-valuetext', `Step ${step}`);
        await expect(speedControl).toContainText(`Step ${step}`);
        expect(await snapshotMainWithoutSlider(page)).toBe(unchangedMain);
        expect(await page.getByRole('contentinfo', { name: 'Demo status' }).textContent()).toBe(unchangedStatus);
      }

      await slider.fill('84');
      await page.getByRole('button', { name: 'Light theme' }).click();
      await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
      await expect(page.getByRole('button', { name: 'Train Control' })).toHaveAttribute('aria-current', 'page');
      await expect(slider).toHaveValue('84');
      await expect(slider).toHaveAttribute('aria-valuetext', 'Step 84');
    } finally {
      await context.close();
    }
  });

  test('SC-009 & FR-046: passes axe-core in Dark and Light themes and disables transitions under reduced motion', async ({
    browser,
  }) => {
    const { context, page } = await openWithForwardedIp(browser, '198.51.100.13', {
      colorScheme: 'light',
    });
    try {
      await page.goto('/', { waitUntil: 'networkidle' });
      await expect(page.getByRole('main')).toBeVisible();
      await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');

      const darkAudit = await new AxeBuilder({ page }).analyze();
      await page.getByRole('button', { name: 'Light theme' }).click();
      await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
      const lightAudit = await new AxeBuilder({ page }).analyze();

      expect(
        { dark: darkAudit.violations, light: lightAudit.violations },
        JSON.stringify({ dark: darkAudit.violations, light: lightAudit.violations }, null, 2),
      ).toEqual({ dark: [], light: [] });

      // Compact width mode (1100 px) must keep Train Control panels legible with zero document horizontal overflow.
      await page.setViewportSize({ width: 1100, height: 800 });
      await expect(page.locator('[data-shell-mode]')).toHaveAttribute('data-shell-mode', 'compact');
      await expect(page.getByRole('region', { name: 'Speedometer' })).toBeVisible();
      await expect(page.getByRole('region', { name: 'Timetable' })).toBeVisible();
      const compactNoOverflow = await page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      );
      expect(compactNoOverflow).toBe(true);
    } finally {
      await context.close();
    }

    const reduced = await openWithForwardedIp(browser, '198.51.100.14', {
      reducedMotion: 'reduce',
    });
    try {
      await reduced.page.goto('/', { waitUntil: 'networkidle' });
      await expect(reduced.page.getByRole('main')).toBeVisible();

      const animatedElements = await reduced.page.evaluate(() => {
        const root = document.querySelector('main .train-control');
        if (!root) return ['missing .train-control'];
        const results: string[] = [];
        for (const el of [root, ...root.querySelectorAll('*')]) {
          const style = getComputedStyle(el);
          if (style.transitionDuration !== '0s') {
            results.push(`${el.tagName}.${el.className}: transitionDuration=${style.transitionDuration}`);
          }
          if (style.animationName !== 'none') {
            results.push(`${el.tagName}.${el.className}: animationName=${style.animationName}`);
          }
        }
        return results;
      });
      expect(animatedElements).toEqual([]);
    } finally {
      await reduced.context.close();
    }
  });
});
