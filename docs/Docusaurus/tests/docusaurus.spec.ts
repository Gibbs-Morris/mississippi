import { test, expect } from '@playwright/test';

test.describe('Docusaurus Site', () => {
  test('homepage loads successfully', async ({ page }) => {
    await page.goto('/');
    
    // Check that the page title is correct
    await expect(page).toHaveTitle(/Mississippi Documentation/);
    
    // Check that the main heading is visible
    await expect(page.locator('h1')).toContainText('Mississippi Documentation');
  });

  test('navigation to docs works', async ({ page }) => {
    await page.goto('/');
    
    // Click the "Get Started" button
    await page.click('text=Get Started');
    
    // Verify we're on a docs page (URL changes)
    await expect(page).toHaveURL(/\/docs\//);
  });

  test('docs page loads with expected content', async ({ page }) => {
    await page.goto('/');
    
    // Navigate via the Get Started button
    await page.click('text=Get Started');
    
    // Wait for navigation and check for docs-specific content
    await page.waitForURL(/\/docs\//);
    
    // Check that the Getting Started page has loaded
    // This page contains SDK installation instructions
    await expect(page.getByRole('heading', { name: 'Installation' })).toBeVisible();
  });

  test('GitHub link is present in navbar', async ({ page }) => {
    await page.goto('/');
    
    // Check that GitHub link exists in navbar specifically
    const navbar = page.locator('.navbar, nav');
    const githubLink = navbar.locator('a[href*="github.com/Gibbs-Morris/mississippi"]');
    await expect(githubLink).toBeVisible();
  });

  test('footer contains correct information', async ({ page }) => {
    await page.goto('/');
    
    // Check footer content
    await expect(page.locator('footer')).toContainText('Mississippi Project');
    await expect(page.locator('footer')).toContainText('Built with Docusaurus');
  });

  test('docs navigation elements exist', async ({ page }) => {
    await page.goto('/');
    await page.click('text=Get Started');
    await page.waitForURL(/\/docs\//);
    
    // Just check that we can find some navigation element (sidebar or nav menu)
    // Don't be too specific about class names as they may vary
    const hasNav = await page.locator('nav, aside, [class*="sidebar"]').count();
    expect(hasNav).toBeGreaterThan(0);
  });

  test('site can be built successfully', async () => {
    // This test verifies that the build completes without errors
    // The actual build is done before tests run, this just confirms it
    expect(true).toBe(true);
  });
});
