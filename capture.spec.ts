import { test, expect } from '@playwright/test';
import path from 'path';
const pages = [
  { name: 'key-concepts', route: '/mississippi/docs/next/samples/spring-sample/concepts/spring-key-concepts', attribute: 'EventStorageName', snippets: 1 },
  { name: 'building-a-saga', route: '/mississippi/docs/next/samples/spring-sample/tutorials/spring-building-a-saga', attribute: 'SnapshotStorageName', snippets: 1 },
  { name: 'building-projections', route: '/mississippi/docs/next/samples/spring-sample/tutorials/spring-building-projections', attribute: 'SnapshotStorageName', snippets: 4 },
];
for (const viewport of [{ name: 'desktop', width: 1280, height: 900 }, { name: 'mobile', width: 390, height: 844 }]) {
  for (const document of pages) {
    test(document.name + ' explicit V1 at ' + viewport.name, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const response = await page.goto(document.route);
      expect(response?.ok()).toBe(true);
      await expect(page.locator('h1')).toBeVisible();
      const snippets = page.locator('pre').filter({ hasText: document.attribute + '(' });
      await expect(snippets).toHaveCount(document.snippets);
      for (let index = 0; index < document.snippets; index++) {
        await expect(snippets.nth(index)).toContainText(/StorageName\("[A-Z]+",\s*"[A-Z]+",\s*"[A-Z]+",\s*1\)/);
      }
      await page.waitForLoadState('networkidle');
      await page.evaluate(() => document.fonts.ready);
      await snippets.first().evaluate((node, mobile) => {
        node.scrollIntoView({ block: 'start' });
        window.scrollBy(0, -100);
        if (mobile) node.scrollLeft = node.scrollWidth;
      }, viewport.name === 'mobile');
      await page.screenshot({ path: path.resolve(__dirname, '../../../.scratchpad/pr819/screenshots', document.name + '-' + viewport.name + '.png'), fullPage: false });
    });
  }
}



