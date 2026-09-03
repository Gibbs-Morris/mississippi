import {expect, test} from '@playwright/test';

test.describe('Mississippi site', () => {
  test('landing page presents the product and maturity clearly', async ({page}) => {
    await page.goto('./');

    await expect(page).toHaveTitle(/Mississippi/);
    await expect(page.getByRole('main')).toHaveCount(1);
    await expect(page.locator('h1')).toHaveCount(1);
    await expect(page.locator('h1')).toContainText(
      'Software should remember why it changed.',
    );
    await expect(page.getByText('Early alpha', {exact: false}).first()).toBeVisible();
    await expect(
      page.getByText('not recommended for production use', {exact: false}).first(),
    ).toBeVisible();
  });

  test('primary evaluation action opens the architectural model', async ({page}) => {
    await page.goto('./');

    const primaryAction = page.getByRole('link', {name: 'Evaluate the architecture'}).first();
    await primaryAction.focus();
    await expect(primaryAction).toHaveCSS('outline-color', 'rgb(255, 253, 247)');
    await expect(primaryAction).toHaveCSS(
      'box-shadow',
      'rgb(7, 20, 24) 0px 0px 0px 4px',
    );
    await primaryAction.click();

    await expect(page).toHaveURL(/\/docs\/concepts\/architectural-model\/?$/);
    await expect(page.getByRole('heading', {level: 1})).toHaveText(
      'Architectural Model',
    );
  });

  test('navigation exposes the product path, docs, and source', async ({page}) => {
    await page.goto('./');

    const navbar = page.getByRole('navigation').first();
    await expect(navbar.getByRole('link', {name: 'How It Works'})).toHaveAttribute(
      'href',
      /#how-it-works$/,
    );
    await expect(navbar.getByRole('link', {name: 'Docs'})).toBeVisible();
    await expect(
      navbar.getByRole('link', {name: 'GitHub'}),
    ).toHaveAttribute('href', 'https://github.com/Gibbs-Morris/mississippi');
    const colorModeToggle = navbar.getByRole('button', {
      name: /Switch between dark and light mode/,
    });
    await expect(colorModeToggle).toHaveCSS('color', 'rgb(242, 233, 216)');
    await colorModeToggle.hover();
    await expect(colorModeToggle).toHaveCSS(
      'background-color',
      'rgba(63, 224, 182, 0.14)',
    );
    await expect(colorModeToggle).toHaveCSS('color', 'rgb(255, 253, 247)');
  });

  test('dark mobile navigation remains readable', async ({page}) => {
    await page.setViewportSize({width: 390, height: 844});
    await page.emulateMedia({colorScheme: 'dark'});
    await page.addInitScript(() => window.localStorage.setItem('theme', 'dark'));
    await page.goto('./');

    await page.getByRole('button', {name: 'Toggle navigation bar'}).click();

    const navbar = page.locator('nav.navbar');
    await expect(navbar).toHaveClass(/navbar-sidebar--show/);
    const sidebar = navbar.locator('.navbar-sidebar');
    await expect(sidebar.locator('.navbar-sidebar__items')).toHaveCSS(
      'background-color',
      'rgb(9, 24, 28)',
    );
    await expect(sidebar.locator('.menu__link:not(.menu__link--active)').first()).toHaveCSS(
      'color',
      'rgb(233, 226, 214)',
    );
    await expect(sidebar.locator('.menu__link--active').first()).toHaveCSS(
      'color',
      'rgb(63, 224, 182)',
    );
  });

  test('social preview metadata uses Mississippi branding', async ({page}) => {
    await page.goto('./');

    await expect(page.locator('meta[property="og:image"]')).toHaveAttribute(
      'content',
      /mississippi-social-card\.png$/,
    );
    await expect(page.locator('meta[name="description"]')).toHaveAttribute(
      'content',
      /opinionated .NET application model/,
    );
  });

  test('footer provides project and documentation destinations', async ({page}) => {
    await page.goto('./');

    const footer = page.getByRole('contentinfo');
    await expect(footer).toContainText('Technical Documentation');
    await expect(footer).toContainText('Built in the open');
  });

  test('landing page does not overflow a mobile viewport', async ({page}) => {
    await page.setViewportSize({width: 390, height: 844});
    await page.goto('./');

    const dimensions = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }));

    expect(dimensions.scrollWidth).toBeLessThanOrEqual(dimensions.clientWidth);
    await expect(page.getByRole('heading', {level: 1})).toBeVisible();
  });

  test('landing page does not overflow a desktop viewport', async ({page}) => {
    await page.goto('./');

    const dimensions = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }));

    expect(dimensions.scrollWidth).toBeLessThanOrEqual(dimensions.clientWidth);
  });

  test('technical documentation uses intent-first navigation', async ({page}) => {
    await page.goto('docs/');

    await expect(page.getByRole('heading', {level: 1})).toHaveText(
      'Mississippi Documentation',
    );
    const sidebar = page.locator('.theme-doc-sidebar-menu');
    await expect(sidebar.getByRole('link', {name: 'Getting Started'})).toBeVisible();
    await expect(sidebar.getByRole('link', {name: 'Tutorials'})).toBeVisible();
    await expect(sidebar.getByRole('link', {name: 'How-To Guides'})).toBeVisible();
    await expect(sidebar.getByRole('link', {name: 'Concepts'})).toBeVisible();
    await expect(sidebar.getByRole('link', {name: 'Reference'})).toBeVisible();
    await expect(sidebar.getByRole('link', {name: 'Subsystems'})).toHaveCount(0);
    await expect(page.getByRole('link', {name: 'Next', exact: true})).toHaveCount(0);
  });
});
