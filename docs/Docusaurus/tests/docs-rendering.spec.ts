import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { expect, test } from '@playwright/test';

test('every published page renders without a diagram error or broken image', async ({ browser, request }) => {
  test.setTimeout(180_000);

  const sitemapResponse = await request.get('/mississippi/sitemap.xml');
  expect(sitemapResponse.ok()).toBe(true);

  const sitemap = await sitemapResponse.text();
  const paths = [...sitemap.matchAll(/<loc>(.*?)<\/loc>/g)]
    .map((match) => new URL(match[1]).pathname);
  expect(paths.length).toBeGreaterThan(0);

  const docsDirectory = resolve(__dirname, '..');
  const metadataDirectory = resolve(docsDirectory, '.docusaurus/docusaurus-plugin-content-docs/default');
  const diagramCounts = new Map<string, number>();
  for (const file of readdirSync(metadataDirectory).filter((name) => name.endsWith('.json'))) {
    const metadata = JSON.parse(readFileSync(resolve(metadataDirectory, file), 'utf8'));
    if (!metadata.source?.startsWith('@site/')) {
      continue;
    }

    const source = readFileSync(resolve(docsDirectory, metadata.source.slice('@site/'.length)), 'utf8');
    diagramCounts.set(metadata.permalink, [...source.matchAll(/^[ \t]*```mermaid[ \t]*\r?$/gm)].length);
  }
  for (const [path, count] of diagramCounts) {
    if (count > 0) {
      expect(paths).toContain(path);
    }
  }

  const failures: string[] = [];
  const correctedDiagramPaths = new Set([
    '/mississippi/docs/next/inlet/getting-started/',
    '/mississippi/docs/next/archived/reference/domain-registration-generators',
  ]);
  for (const path of correctedDiagramPaths) {
    expect(paths).toContain(path);
    expect(diagramCounts.get(path)).toBeGreaterThan(0);
  }
  let next = 0;

  await Promise.all(Array.from({ length: 4 }, async () => {
    const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

    while (next < paths.length) {
      const path = paths[next++];
      const expectedDiagrams = diagramCounts.get(path) ?? 0;

      try {
        const response = await page.goto(path, { waitUntil: 'networkidle' });

        if (expectedDiagrams > 0) {
          await expect(page.locator('.docusaurus-mermaid-container svg, [class*="errorBoundaryFallback"]'))
            .toHaveCount(expectedDiagrams);
          for (const diagram of await page.locator('.docusaurus-mermaid-container svg').all()) {
            await expect(diagram).toBeVisible();
          }
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

        if (!response?.ok() || !rendering.hasHeading || rendering.diagramCount !== expectedDiagrams || rendering.diagramErrors.length || rendering.brokenImages.length) {
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
