import { expect, test } from '@playwright/test';

test('every published page renders without a diagram error or broken image', async ({ browser, request }) => {
  test.setTimeout(180_000);

  const sitemapResponse = await request.get('/mississippi/sitemap.xml');
  expect(sitemapResponse.ok()).toBe(true);

  const sitemap = await sitemapResponse.text();
  const paths = [...sitemap.matchAll(/<loc>(.*?)<\/loc>/g)]
    .map((match) => new URL(match[1]).pathname);
  expect(paths.length).toBeGreaterThan(0);

  const failures: string[] = [];
  const correctedDiagramPaths = new Set([
    '/mississippi/docs/next/inlet/getting-started/',
    '/mississippi/docs/next/archived/reference/domain-registration-generators',
  ]);
  for (const path of correctedDiagramPaths) {
    expect(paths).toContain(path);
  }
  let next = 0;

  await Promise.all(Array.from({ length: 4 }, async () => {
    const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

    while (next < paths.length) {
      const path = paths[next++];

      try {
        const response = await page.goto(path, { waitUntil: 'networkidle' });

        if (correctedDiagramPaths.has(path)) {
          await page.locator('.docusaurus-mermaid-container svg, [class*="errorBoundaryFallback"]')
            .first().waitFor({ state: 'visible' });
        }

        const rendering = await page.evaluate(() => ({
          hasHeading: Boolean(document.querySelector('h1')),
          diagramCount: document.querySelectorAll('.docusaurus-mermaid-container svg').length,
          diagramErrors: Array.from(document.querySelectorAll('[class*="errorBoundaryFallback"]'))
            .map((element) => element.textContent?.trim()),
          brokenImages: Array.from(document.images)
            .filter((image) => !image.complete || image.naturalWidth === 0)
            .map((image) => image.src),
        }));

        if (!response?.ok() || !rendering.hasHeading || (correctedDiagramPaths.has(path) && !rendering.diagramCount) || rendering.diagramErrors.length || rendering.brokenImages.length) {
          failures.push(`${path}: HTTP ${response?.status()}, heading=${rendering.hasHeading}, diagrams=${rendering.diagramCount}, diagram errors=${rendering.diagramErrors.join('; ')}, broken images=${rendering.brokenImages.join(', ')}`);
        }
      } catch (error) {
        failures.push(`${path}: ${String(error)}`);
      }
    }

    await page.close();
  }));

  expect(failures).toEqual([]);
});
