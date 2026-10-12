import {test, expect} from '@playwright/test';

test('ordinary docs load and policy files have no public routes', async ({page}, testInfo) => {
  await page.setViewportSize({width: 1280, height: 900});
  const docs = await page.goto('/mississippi/docs/next/');
  expect(docs?.status()).toBe(200);
  await expect(page.locator('main')).toBeVisible();
  await testInfo.attach('docs-next-desktop', {body: await page.screenshot({fullPage: true}), contentType: 'image/png'});

  const adr = await page.goto('/mississippi/docs/next/adr');
  expect(adr?.status()).toBe(200);
  await expect(page.locator('main')).toBeVisible();
  await testInfo.attach('adr-desktop', {body: await page.screenshot({fullPage: true}), contentType: 'image/png'});

  for (const path of ['/mississippi/docs/next/AGENTS', '/mississippi/docs/next/adr/AGENTS']) {
    const policy = await page.goto(path);
    expect(policy?.status(), path).toBe(404);
    await expect(page.locator('main')).not.toContainText('Public documentation contracts');
    await expect(page.locator('main')).not.toContainText('Architecture decisions');
    await testInfo.attach(path.includes('/adr/') ? 'excluded-adr-policy' : 'excluded-docs-policy', {body: await page.screenshot({fullPage: true}), contentType: 'image/png'});
  }

  await page.setViewportSize({width: 390, height: 844});
  const mobileDocs = await page.goto('/mississippi/docs/next/');
  expect(mobileDocs?.status()).toBe(200);
  await testInfo.attach('docs-next-mobile', {body: await page.screenshot({fullPage: true}), contentType: 'image/png'});
});