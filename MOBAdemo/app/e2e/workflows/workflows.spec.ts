import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

async function openWorkflows(page: Page) {
  await page.goto('/');
  await expect(page.getByRole('main')).toBeVisible();

  const navLink = page.getByRole('navigation', { name: 'MOBAflow pages' }).getByRole('button', { name: 'Workflows' });
  await expect(navLink).toBeEnabled();
  await navLink.click();

  const main = page.getByRole('main');
  await expect(main.getByRole('heading', { level: 1, name: 'Workflows' })).toBeVisible();
  return main;
}

test.describe('MOBAdemo Workflows page browser checks (slice 17)', () => {
  test('renders three panes, connected graph nodes, and read-only properties with zero errors (US2 S3, FR-037)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.151' },
    });
    try {
      const page = await context.newPage();
      const consoleErrors: string[] = [];
      const pageErrors: string[] = [];
      page.on('console', (msg) => {
        if (msg.type() === 'error') consoleErrors.push(msg.text());
      });
      page.on('pageerror', (err) => pageErrors.push(err.message));

      const main = await openWorkflows(page);

      const listPane = main.getByRole('region', { name: 'Workflows list' });
      const actionsPane = main.getByRole('region', { name: 'Actions' });
      const propertiesPane = main.getByRole('region', { name: 'Properties' });
      await expect(listPane).toBeVisible();
      await expect(actionsPane).toBeVisible();
      await expect(propertiesPane).toBeVisible();

      await expect(listPane.getByRole('button', { name: 'Arrival Main Station' })).toHaveAttribute('aria-pressed', 'true');
      await expect(listPane.getByRole('button', { name: 'Departure Main Station' })).toHaveAttribute('aria-pressed', 'false');
      await expect(listPane.getByRole('button', { name: 'Shunting Announcement' })).toHaveAttribute('aria-pressed', 'false');

      await expect(actionsPane.getByRole('button', { name: 'Graph view' })).toHaveAttribute('aria-pressed', 'true');
      await expect(actionsPane.getByRole('list', { name: 'Workflow nodes' }).getByRole('listitem')).toHaveCount(5);
      await expect(actionsPane.locator('svg.workflows-graph-svg path.workflows-edge-path')).toHaveCount(5);

      await expect(propertiesPane.getByRole('textbox', { name: 'Name' })).toHaveValue('Arrival Main Station');
      await expect(propertiesPane.getByRole('textbox', { name: 'Steps' })).toHaveValue('5');
      await expect(propertiesPane.getByRole('textbox', { name: 'Error policy' })).toHaveValue('Stop on error');

      expect(consoleErrors).toEqual([]);
      expect(pageErrors).toEqual([]);
    } finally {
      await context.close();
    }
  });

  test('keeps every inert control unchanged on activation (US2 S4, SC-004)', async ({ browser }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.152' },
    });
    try {
      const page = await context.newPage();
      const main = await openWorkflows(page);

      const snapshotWithoutHints = () =>
        main.evaluate((el) => {
          const clone = el.cloneNode(true) as HTMLElement;
          clone.querySelectorAll('[role="tooltip"]').forEach((node) => node.remove());
          return clone.innerHTML;
        });
      const initialDom = await snapshotWithoutHints();

      const inertButtons = main.locator('button[aria-disabled="true"]');
      const count = await inertButtons.count();
      expect(count).toBe(19); // 2 list actions + 3 workflow items + 2 view toggle + 7 toolbar + 5 graph nodes

      for (let index = 0; index < count; index++) {
        const button = inertButtons.nth(index);
        await expect(button).toBeVisible();
        await button.scrollIntoViewIfNeeded();
        const box = await button.boundingBox();
        expect(box).not.toBeNull();
        await page.mouse.click(box!.x + box!.width / 2, box!.y + box!.height / 2);
        const tooltipId = await button.getAttribute('aria-describedby');
        expect(tooltipId).toBeTruthy();
        await expect(page.locator(`#${tooltipId}`)).toBeVisible();
        await page.keyboard.press('Escape');
      }

      await expect(main.getByRole('heading', { level: 1, name: 'Workflows' })).toBeVisible();
      await expect(main.getByRole('button', { name: 'Arrival Main Station', exact: true })).toHaveAttribute('aria-pressed', 'true');
      await expect(main.getByRole('button', { name: 'Departure Main Station', exact: true })).toHaveAttribute('aria-pressed', 'false');
      await expect(main.getByRole('button', { name: 'Graph view', exact: true })).toHaveAttribute('aria-pressed', 'true');
      await expect(main.getByRole('button', { name: 'List view', exact: true })).toHaveAttribute('aria-pressed', 'false');
      await expect(main.getByRole('textbox', { name: 'Name' })).toHaveValue('Arrival Main Station');
      expect(await snapshotWithoutHints()).toBe(initialDom);
    } finally {
      await context.close();
    }
  });

  test('passes axe-core in Dark and Light themes and preserves Workflows across theme toggle (FR-021, SC-009)', async ({
    browser,
  }) => {
    const context = await browser.newContext({
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.153' },
    });
    try {
      const page = await context.newPage();
      const main = await openWorkflows(page);

      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('dark');
      const darkAudit = await new AxeBuilder({ page }).analyze();

      const themeButton = page.getByRole('button', { name: 'Light theme' });
      await themeButton.click();
      await expect(themeButton).toHaveAttribute('aria-pressed', 'true');
      expect(await page.evaluate(() => document.documentElement.dataset.theme)).toBe('light');
      await expect(main.getByRole('heading', { level: 1, name: 'Workflows' })).toBeVisible();

      const lightAudit = await new AxeBuilder({ page }).analyze();
      expect(
        { dark: darkAudit.violations, light: lightAudit.violations },
        JSON.stringify({ dark: darkAudit.violations, light: lightAudit.violations }, null, 2),
      ).toEqual({ dark: [], light: [] });
    } finally {
      await context.close();
    }
  });

  test('has zero horizontal document overflow at 1100 px compact width (FR-033)', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: { width: 1100, height: 800 },
      extraHTTPHeaders: { 'X-Forwarded-For': '198.51.100.154' },
    });
    try {
      const page = await context.newPage();
      await openWorkflows(page);
      await expect(page.locator('[data-shell-mode]')).toHaveAttribute('data-shell-mode', 'compact');

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      );
      expect(overflow).toBe(true);
    } finally {
      await context.close();
    }
  });
});
