import {expect, test} from '@playwright/test';

test.describe('Mississippi landing page', () => {
  test('presents the product, maturity, and verified reading paths', async ({page}) => {
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => {
      if (message.type() === 'error') errors.push(message.text());
    });

    await page.goto('./');

    await expect(page).toHaveTitle(/Mississippi/);
    await expect(page.getByRole('heading', {level: 1})).toHaveText(
      'Give every change a reason you can trace.',
    );
    await expect(page.getByText('Early alpha', {exact: false})).toBeVisible();
    await expect(page.getByText('Not recommended for production use', {exact: false})).toBeVisible();
    await expect(page.getByRole('link', {name: 'MIT License'})).toHaveAttribute(
      'href',
      'https://github.com/Gibbs-Morris/mississippi/blob/main/LICENSE',
    );
    await expect(page.getByRole('link', {name: 'Capability and package map Reference'})).toHaveAttribute(
      'href',
      /\/docs\/next\/reference\/capability-map\/?$/,
    );
    expect(errors).toEqual([]);
  });

  test('keyboard evaluation action reaches current technical docs', async ({page}) => {
    await page.goto('./');
    const action = page.getByRole('link', {name: /Evaluate the architecture/});

    for (let attempt = 0; attempt < 20; attempt += 1) {
      await page.keyboard.press('Tab');
      if (await action.evaluate(element => element === document.activeElement)) break;
    }
    await expect(action).toBeFocused();
    await expect(action).toHaveCSS('outline-style', 'solid');
    await page.keyboard.press('Enter');

    await expect(page).toHaveURL(/\/docs\/next\/concepts\/concepts-architectural-model\/?$/);
    await expect(page.getByRole('heading', {level: 1})).toHaveText('Architectural Model');
  });

  test('dark source action stays legible while hovered', async ({page}) => {
    await page.addInitScript(() => localStorage.setItem('theme', 'dark'));
    await page.goto('./');
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    const source = page.getByRole('link', {name: 'Inspect the source'});
    await source.hover();
    const contrast = await source.evaluate(element => {
      const style = getComputedStyle(element);
      const luminance = (color: string) => color.match(/[\d.]+/g)!.slice(0, 3)
        .map(Number).map(channel => channel / 255)
        .map(channel => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4)
        .reduce((sum, channel, index) => sum + channel * [0.2126, 0.7152, 0.0722][index], 0);
      const foreground = luminance(style.color);
      const background = luminance(style.backgroundColor);
      return (Math.max(foreground, background) + 0.05) / (Math.min(foreground, background) + 0.05);
    });
    expect(contrast).toBeGreaterThanOrEqual(4.5);
  });

  for (const width of [390, 768]) {
    test(`fits a ${width}px viewport without horizontal overflow`, async ({page}) => {
      await page.setViewportSize({width, height: 844});
      await page.goto('./');
      await expect(page.getByRole('heading', {level: 1})).toBeVisible();
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );
      expect(overflow).toBeLessThanOrEqual(1);
    });
  }

  test('dark mobile navigation exposes the docs link', async ({page}) => {
    await page.setViewportSize({width: 390, height: 844});
    await page.emulateMedia({colorScheme: 'dark'});
    await page.addInitScript(() => localStorage.setItem('theme', 'dark'));
    await page.goto('./');

    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    await page.getByRole('button', {name: 'Toggle navigation bar'}).click();
    await expect(page.locator('nav.navbar')).toHaveClass(/navbar-sidebar--show/);
    await expect(page.locator('.navbar-sidebar').getByRole('link', {name: 'Docs'})).toBeInViewport();
  });
});
