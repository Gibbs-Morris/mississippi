const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { createRequire } = require('node:module');
const requireDocs = createRequire(path.join(process.cwd(), 'docs/Docusaurus/package.json'));
const { chromium, expect } = requireDocs('@playwright/test');
const build = path.join(process.cwd(), 'docs/Docusaurus/build');
const output = path.join(__dirname, 'browser-evidence');
fs.mkdirSync(output, { recursive: true });
const files = fs.readdirSync(build, { recursive: true }).filter(x => x.endsWith('.html') && x.includes('cosmos'));
const target = files.find(x => {
  const html = fs.readFileSync(path.join(build, x), 'utf8');
  return html.includes('Brooks Cosmos DB Provider') && html.includes('Pending append recovery');
});
if (!target) throw new Error('Built Cosmos provider reference was not found');
const route = '/mississippi/' + target.replaceAll('\\', '/').replace(/index\.html$/, '').replace(/\.html$/, '');
const server = http.createServer((request, response) => {
  let relative;
  try { relative = decodeURIComponent(new URL(request.url, 'http://127.0.0.1:31826').pathname); }
  catch { response.writeHead(400); response.end(); return; }
  if (!relative.startsWith('/mississippi/')) { response.writeHead(404); response.end(); return; }
  const candidate = path.resolve(build, '.' + relative.slice('/mississippi'.length));
  if (!candidate.startsWith(build + path.sep)) { response.writeHead(403); response.end(); return; }
  let file = candidate;
  if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
  if (!fs.existsSync(file) && fs.existsSync(file + '.html')) file += '.html';
  if (!fs.existsSync(file)) { response.writeHead(404); response.end(); return; }
  const types = { '.html':'text/html', '.js':'application/javascript', '.css':'text/css', '.png':'image/png', '.svg':'image/svg+xml', '.json':'application/json', '.woff2':'font/woff2' };
  response.setHeader('Content-Type', types[path.extname(file)] || 'application/octet-stream');
  fs.createReadStream(file).pipe(response);
});
let browser;
(async () => {
  await new Promise((resolve, reject) => { server.once('error', reject); server.listen(31826, '127.0.0.1', resolve); });
  try {
    browser = await chromium.launch({ headless: true });
    const evidence = { head:'62a08812b8445063e87d57bf519abfb4f317e2a6', route, browser:browser.version(), checkedAtUtc:new Date().toISOString(), cases:[] };
    for (const viewport of [{name:'desktop',width:1440,height:1000},{name:'mobile',width:390,height:844}]) {
      const context = await browser.newContext({ viewport:{width:viewport.width,height:viewport.height}, colorScheme:'light' });
      const page = await context.newPage();
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      const response = await page.goto('http://127.0.0.1:31826' + route, { waitUntil:'networkidle' });
      expect(response.status()).toBe(200);
      const article = page.locator('article');
      await expect(article.getByRole('heading',{name:'Brooks Cosmos DB Provider',exact:true})).toBeVisible();
      await expect(article).toContainText('retains pending evidence.');
      await expect(article).toContainText('do not fence Cosmos requests already in flight after lease expiry.');
      await expect(article).toContainText('retrying transient cleanup under renewed lease.');
      await page.evaluate(() => document.fonts.ready);
      const top = path.join(output, viewport.name + '-top.png');
      await page.screenshot({ path:top, animations:'disabled' });
      const recovery = article.getByRole('heading',{name:/^Pending append recovery/});
      await recovery.scrollIntoViewIfNeeded();
      await expect(recovery).toBeVisible();
      const pending = path.join(output, viewport.name + '-pending.png');
      await page.screenshot({ path:pending, animations:'disabled' });
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
      expect(errors).toEqual([]);
      evidence.cases.push({ viewport:{width:viewport.width,height:viewport.height}, screenshots:[top,pending], pageErrors:errors, assertions:'HTTP200; heading and recovery contract; no document overflow; no page errors' });
      await context.close();
    }
    fs.writeFileSync(path.join(output, 'evidence.json'), JSON.stringify(evidence, null, 2));
    console.log('BROWSER_VALIDATION_PASSED=' + route);
  } finally {
    if (browser) await browser.close();
    await new Promise(resolve => server.close(resolve));
  }
})().catch(error => { console.error(error); process.exitCode = 1; });

