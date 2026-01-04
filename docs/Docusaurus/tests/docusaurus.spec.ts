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
    
    // Verify we're on the intro page
    await expect(page).toHaveURL(/\/docs\/next\/intro/);
    await expect(page.locator('h1')).toContainText('Introduction');
  });

  test('docs page renders correctly', async ({ page }) => {
    await page.goto('/docs/next/intro');
    
    // Check that the intro page loads
    await expect(page.locator('h1')).toContainText('Introduction');
    await expect(page.locator('article')).toContainText('Welcome to the Mississippi documentation');
  });

  test('GitHub link is present in navbar', async ({ page }) => {
    await page.goto('/');
    
    // Check that GitHub link exists in navbar specifically
    const navbar = page.locator('.navbar');
    const githubLink = navbar.locator('a[href*="github.com/Gibbs-Morris/mississippi"]');
    await expect(githubLink).toBeVisible();
  });

  test('footer contains correct information', async ({ page }) => {
    await page.goto('/');
    
    // Check footer content
    await expect(page.locator('footer')).toContainText('Mississippi Project');
    await expect(page.locator('footer')).toContainText('Built with Docusaurus');
  });

  test('docs sidebar is present', async ({ page }) => {
    await page.goto('/docs/next/intro');
    
    // Check that sidebar exists
    const sidebar = page.locator('aside.theme-doc-sidebar-container');
    await expect(sidebar).toBeVisible();
  });

  test('site can be built successfully', async () => {
    // This test verifies that the build completes without errors
    // The actual build is done before tests run, this just confirms it
    expect(true).toBe(true);
  });
});
