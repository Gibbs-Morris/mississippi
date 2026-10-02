import {chromium, expect} from '@playwright/test';

const browser = await chromium.launch();
const context = await browser.newContext({
  viewport: {width: 390, height: 844},
  colorScheme: 'dark',
  baseURL: 'http://127.0.0.1:3000/mississippi/',
});
await context.addInitScript(() => localStorage.setItem('theme', 'dark'));

try {
  const page = await context.newPage();
  await page.goto('./');
  await page.screenshot({path: 'evidence/landing-mobile-dark.png', fullPage: true, animations: 'disabled'});
  await page.getByRole('button', {name: 'Toggle navigation bar'}).click();
  await expect(page.locator('nav.navbar')).toHaveClass(/navbar-sidebar--show/);
  await expect(page.locator('.navbar-sidebar').getByRole('link', {name: 'Docs'})).toBeInViewport();
  await page.screenshot({path: 'evidence/landing-mobile-menu-dark.png', animations: 'disabled'});
} finally {
  await browser.close();
}
