// Capture real Spring journeys and accessibility evidence. No response is fulfilled with invented data.
// Optional dependencies: playwright@1.62.1 and @axe-core/playwright@4.13.0; see the adjacent README.
const playwrightModule = process.env.SPRING_PLAYWRIGHT_MODULE || 'playwright';
const { chromium } = require(playwrightModule);
const { expect } = require(playwrightModule + '/test');
const { AxeBuilder } = require(process.env.SPRING_AXE_MODULE || '@axe-core/playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

(async () => {
  const base = process.env.SPRING_URL || 'http://localhost:5101';
  const localAuth = process.env.SPRING_LOCAL_AUTH || 'On';
  assert.ok(['On', 'Off'].includes(localAuth), 'SPRING_LOCAL_AUTH must match the launcher mode: On or Off');
  for (const key of ['SPRING_SOURCE_REF', 'SPRING_CLIENT_TREE', 'SPRING_REFRACTION_TREE']) {
    assert.match(process.env[key] || '', /^[0-9a-f]{40}$/, key + ' must identify the committed source used by the running app');
  }
  const output = path.resolve(process.env.SPRING_CAPTURE_DIR || 'artifacts/spring-redesign/final');
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const manifest = { browser: browser.version(), playwright: require(playwrightModule + '/package.json').version, axe: require((process.env.SPRING_AXE_MODULE || '@axe-core/playwright') + '/package.json').version, source: { commit: process.env.SPRING_SOURCE_REF, clientTree: process.env.SPRING_CLIENT_TREE, refractionTree: process.env.SPRING_REFRACTION_TREE }, base, localAuth, capturedAt: new Date().toISOString(), captures: [], checks: [], errors: [] };
  manifest.status = 'RUNNING';
  manifest.completed = false;
  manifest.phase = 'start';
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true, hasTouch: true });
  const page = await context.newPage();
  const observe = (p) => {
    p.on('pageerror', e => manifest.errors.push({ kind: 'exception', message: e.message }));
    p.on('console', m => { if (m.type() === 'error') manifest.errors.push({ kind: 'console', message: m.text() }); });
    p.on('response', r => { if (r.status() >= 400) manifest.errors.push({ kind: 'http', status: r.status(), url: r.url() }); });
    p.on('requestfailed', r => manifest.errors.push({ kind: 'network', url: r.url(), error: r.failure()?.errorText }));
  };
  observe(page);
  page.setDefaultTimeout(60000);
  const connected = () => page.getByRole('button', { name: 'Connection status: Connected', exact: true }).filter({ hasText: /^\s*Connected\s*$/ }).waitFor();
  const balance = (p, account, amount) => expect(p.getByLabel('Account ' + account + ' live balance', { exact: true })).toHaveText(amount, { timeout: 120000 });
  const waitForGate = async (promise, phase, timeout = 60000) => {
    manifest.phase = phase;
    let timer;
    try {
      return await Promise.race([promise, new Promise((_, reject) => {
        timer = setTimeout(() => reject(new Error('Timed out waiting for ' + phase)), timeout);
      })]);
    } finally {
      clearTimeout(timer);
    }
  };
  const verifyProtectedRead = async (response, status) => {
    assert.equal(response.status(), status);
    const outcome = page.locator('.spring-auth-outcome');
    const count = page.getByLabel('Authenticated access events observed', { exact: true });
    if (status === 200) {
      const body = await waitForGate(response.json(), 'protected-read-body');
      await expect(outcome.getByRole('alert')).toHaveCount(0);
      await expect(count).toHaveText(String(body.authenticatedAccessCount));
    } else if (status === 404) {
      await expect(outcome.getByRole('alert')).toHaveCount(0);
      await expect(count).toHaveCount(0);
      await expect(outcome).toContainText('No projection data received yet.');
    } else {
      // ResponseHeadersRead denies and disposes the response without consuming its body.
      await expect(outcome.getByRole('alert')).toContainText('HTTP ' + status);
      await expect(count).toHaveCount(0);
    }
    await expect(outcome.locator('p[role="status"]')).toHaveCount(0);
    await expect(outcome.getByRole('button', { name: 'Refresh protected read', exact: true })).toBeEnabled();
  };
  const capture = async (name, width = 390, height = 844, audit = true) => {
    manifest.phase = 'capture:' + name;
    await page.setViewportSize({ width, height });
    await page.screenshot({ path: path.join(output, name + '.png'), fullPage: true });
    const preview = ['home-desktop', 'home-phone', 'accounts-ready-desktop', 'accounts-ready-phone'].includes(name) ? name + '-viewport.png' : null;
    if (preview) {
      await page.evaluate(() => window.scrollTo({ top: 0, left: 0, behavior: 'instant' }));
      await page.screenshot({ path: path.join(output, preview), fullPage: false });
    }
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    assert.equal(overflow, false, name + ' overflows the viewport');
    const axe = audit ? await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze() : null;
    const summarize = results => results.map(v => ({ id: v.id, impact: v.impact, description: v.description, nodes: v.nodes.map(n => ({ target: n.target, failureSummary: n.failureSummary })) }));
    const violations = axe?.violations || [];
    manifest.captures.push({ name, preview, capturedAt: new Date().toISOString(), route: page.url().slice(base.length), viewport: { width, height }, theme: await page.locator('html').getAttribute('data-rf-theme'), audited: audit, forcedColors: await page.evaluate(() => matchMedia('(forced-colors: active)').matches), overflow, violations: summarize(violations), incomplete: summarize(axe?.incomplete || []) });
    console.log(JSON.stringify({ name, overflow, violations: violations.length }));
    manifest.phase = 'journey-after:' + name;
  };
  const task = async (account, name) => {
    const d = page.locator('#account-' + account + '-' + name + '-task');
    if (await d.getAttribute('open') === null) await d.locator('summary').press('Enter');
  };
  const hasHttpPersona = (request, persona) => {
    const headers = request.headers();
    const anonymous = headers['x-spring-anonymous'] === 'true';
    if (persona === 'Unauthenticated') return anonymous;
    const roles = ['AuthProof Role', 'Full Access'].includes(persona)
      ? 'banking-operator,transfer-operator,auth-proof-operator'
      : 'banking-operator,transfer-operator';
    const claims = ['AuthProof Claim', 'Full Access'].includes(persona) ? 'spring.permission=auth-proof' : undefined;
    return !anonymous && headers['x-spring-roles'] === roles && headers['x-spring-claims'] === claims;
  };
  const refreshUntilCount = async (endpoint, persona, expectedCount) => {
    const deadline = Date.now() + 120000;
    for (;;) {
      const remaining = deadline - Date.now();
      assert.ok(remaining > 0, 'Projection did not reach authenticated count ' + expectedCount);
      const refresh = page.getByRole('button', { name: 'Refresh protected read', exact: true });
      await expect(refresh).toBeEnabled({ timeout: remaining });
      const read = page.waitForRequest(r => r.method() === 'GET' && r.url().endsWith(endpoint) && hasHttpPersona(r, persona), { timeout: Math.max(1, deadline - Date.now()) });
      await refresh.tap({ timeout: Math.max(1, deadline - Date.now()) });
      const request = await read;
      const response = await waitForGate(request.response(), 'protected-refresh-response', Math.max(1, deadline - Date.now()));
      assert.ok(response, 'Protected refresh received no HTTP response');
      assert.ok([200, 404].includes(response.status()), 'Unexpected protected-read status ' + response.status());
      await waitForGate(verifyProtectedRead(response, response.status()), 'protected-read-catch-up', Math.max(1, deadline - Date.now()));
      if (response.status() === 200) {
        const body = await waitForGate(response.json(), 'protected-count-body', Math.max(1, deadline - Date.now()));
        assert.ok(Number.isInteger(body.authenticatedAccessCount) && body.authenticatedAccessCount >= 0 && body.authenticatedAccessCount <= expectedCount);
        if (body.authenticatedAccessCount === expectedCount) return;
      }
    }
  };
  const captureAuthOff = async () => {
    await page.goto(base + '/auth-proof', { waitUntil: 'domcontentloaded' });
    const entity = 'capture-auth-off-' + Date.now();
    const endpoint = '/api/projections/auth-proof/' + entity;
    await page.getByLabel('Auth Proof entity ID', { exact: true }).fill(entity);
    for (const persona of ['Unauthenticated', 'Operator Roles', 'AuthProof Role', 'AuthProof Claim', 'Full Access']) {
      const read = page.waitForResponse(r => r.request().method() === 'GET' && r.url().endsWith(endpoint) && hasHttpPersona(r.request(), persona));
      await page.getByRole('button', { name: persona, exact: true }).tap();
      const observed = await read;
      await verifyProtectedRead(observed, 401);
      await expect(page.locator('.spring-auth-outcome').getByRole('alert')).toContainText('HTTP 401');
      await expect(page.getByLabel('Authenticated access events observed', { exact: true })).toHaveCount(0);
      for (const [route, button] of [['authenticated', 'Record Authenticated Access'], ['policy', 'Record Policy Access'], ['role', 'Record Role Access']]) {
        const command = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith('/' + entity + '/' + route));
        await page.getByRole('button', { name: button, exact: true }).tap();
        assert.equal((await command).status(), 401);
        await expect(page.getByRole('region', { name: 'Auth Proof command responses', exact: true }).getByRole('alert')).toContainText('401');
      }
      const saga = page.waitForResponse(r => r.request().method() === 'POST' && r.url().includes('/api/sagas/auth-proof/'));
      await page.getByRole('button', { name: 'Start AuthProof Saga', exact: true }).tap();
      assert.equal((await saga).status(), 401);
      await expect(page.getByRole('region', { name: 'Auth Proof saga-start responses', exact: true }).getByRole('alert')).toContainText('401');
      await capture('auth-off-' + persona.replaceAll(' ', '-').toLowerCase() + '-phone');
    }
    await capture('auth-off-full-access-desktop', 1440, 900);
    manifest.checks.push('LocalAuth Off: all five browser personas receive actual HTTP401 for all three commands, saga start and protected projection read; no count is displayed');
  };
  try {
    if (localAuth === 'Off') {
      await captureAuthOff();
    } else {
      for (const route of ['/', '/operations', '/accounts', '/investigations', '/auth-proof']) {
        await page.goto(base + route, { waitUntil: 'domcontentloaded' });
        await page.getByRole('heading', { level: 1 }).waitFor();
        if (route === '/accounts' || route === '/operations') await connected();
        const name = route === '/' ? 'home' : route.slice(1);
        await capture(name + '-desktop', 1440, 900);
        await capture(name + '-phone');
      }
      await page.goto(base + '/', { waitUntil: 'domcontentloaded' });
      await expect(page.locator('h1')).toBeFocused();
      await page.getByRole('link', { name: 'Skip to content', exact: true }).focus();
      await page.keyboard.press('Enter');
      await expect(page.locator('#main-content')).toBeFocused();
      manifest.checks.push('Heading navigation focus and skip link focus');
      await page.getByRole('link', { name: 'Start with two accounts', exact: true }).tap();
      await connected();
      await page.getByRole('button', { name: 'Initialize demo accounts', exact: true }).tap();
      await page.getByRole('link', { name: 'Go to Operations', exact: true }).tap();
      await balance(page, 'A', '£500.00');
      await balance(page, 'B', '£500.00');
      manifest.checks.push('Real touch first-run path: both projected opening balances £500');
      const pair = page.url();
      let releaseReads;
      let readsStarted;
      const readGate = new Promise(resolve => { releaseReads = resolve; });
      const readWaiting = new Promise(resolve => { readsStarted = resolve; });
      await page.route('**/api/projections/bank-account-balance/*', async route => { readsStarted(); await readGate; await route.continue(); });
      await page.goto(pair, { waitUntil: 'domcontentloaded' });
      await waitForGate(readWaiting, 'initial-projection-request');
      await expect(page.locator('#account-a-operations-panel').getByRole('heading', { name: 'Loading the live account', exact: true })).toBeVisible();
      await capture('accounts-loading-phone');
      releaseReads();
      await balance(page, 'A', '£500.00');
      await balance(page, 'B', '£500.00');
      await page.unroute('**/api/projections/bank-account-balance/*');
      manifest.checks.push('Real initial projection requests held for loading capture, then continued to the real backend');
      await capture('accounts-ready-desktop', 1440, 900);
      await capture('accounts-ready-phone');
      const connectionButton = page.getByRole('button', { name: 'Connection status: Connected', exact: true });
      await connectionButton.press('Enter');
      await expect(page.getByRole('region', { name: 'Live connection details', exact: true })).toBeVisible();
      await capture('connection-details-phone');
      await capture('connection-details-narrow-phone', 320, 740);
      await page.setViewportSize({ width: 390, height: 844 });
      await page.getByRole('button', { name: 'Close', exact: true }).press('Enter');
      await expect(connectionButton).toBeFocused();
      manifest.checks.push('Connection disclosure keyboard open/close and restored focus');
      await page.getByRole('link', { name: 'Account B ↓', exact: true }).press('Enter');
      await expect(page.locator('#account-b-operations-panel')).toBeFocused();
      assert.equal(new URL(page.url()).pathname, '/operations');
      await page.getByRole('link', { name: 'Account A ↓', exact: true }).tap();
      await expect(page.locator('#account-a-operations-panel')).toBeFocused();
      assert.equal(new URL(page.url()).pathname, '/operations');
      await page.getByRole('button', { name: 'View Investigations', exact: true }).tap();
      await page.getByRole('link', { name: 'Move money', exact: true }).tap();
      await balance(page, 'A', '£500.00');
      await balance(page, 'B', '£500.00');
      await page.locator('.spring-share-pair summary').press('Enter');
      const sharedPair = await page.locator('.spring-share-pair > a').getAttribute('href');
      assert.equal(sharedPair, pair);
      await capture('shared-pair-after-navigation-phone');
      manifest.checks.push('Keyboard/touch account anchors retain operations/pair/focus; rendered shared link retains both IDs after task navigation');
      const second = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 } });
      const observer = await second.newPage();
      observe(observer);
      await observer.goto(sharedPair, { waitUntil: 'domcontentloaded' });
      await balance(observer, 'A', '£500.00');
      let release;
      let intercepted;
      const waiting = new Promise(resolve => { intercepted = resolve; });
      const gate = new Promise(resolve => { release = resolve; });
      await page.route('**/api/aggregates/bank-account/*/deposit', async route => { intercepted(); await gate; await route.continue(); });
      await page.getByLabel('Account A deposit amount (£)', { exact: true }).fill('25');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Deposit £', exact: true }).tap();
      await waitForGate(waiting, 'deposit-command-request');
      await expect(page.getByRole('region', { name: 'Banking responses · this browser', exact: true }).getByRole('status')).toContainText('awaiting a response');
      await capture('command-pending-phone');
      release();
      await balance(page, 'A', '£525.00');
      await balance(observer, 'A', '£525.00');
      await page.unroute('**/api/aggregates/bank-account/*/deposit');
      await expect(page.locator('#account-a-operations-panel').getByRole('cell', { name: '£25.00', exact: true })).toBeVisible();
      manifest.checks.push('Real deposit response delayed for pending capture; both browsers then show £525 and actual ledger entry');
      await task('a', 'transfer');
      await page.getByLabel('Account A transfer amount (£)', { exact: true }).fill('12.');
      await expect(page.locator('#account-a-operations-panel').getByRole('button', { name: 'Start Transfer', exact: true })).toBeDisabled();
      await capture('invalid-transfer-phone');
      await page.getByLabel('Account A transfer amount (£)', { exact: true }).fill('25');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Start Transfer', exact: true }).tap();
      await expect(page.locator('#account-a-transfer-status')).toContainText('Phase: Completed', { timeout: 120000 });
      await balance(page, 'A', '£500.00');
      await balance(page, 'B', '£525.00');
      await capture('transfer-completed-desktop', 1440, 900);
      await capture('transfer-completed-phone');
      await task('a', 'withdraw');
      await page.getByLabel('Account A withdrawal amount (£)', { exact: true }).fill('2000');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Withdraw', exact: true }).tap();
      await expect(page.getByRole('region', { name: 'Banking responses · this browser', exact: true }).getByRole('alert')).toContainText('Insufficient');
      await balance(page, 'A', '£500.00');
      await capture('withdrawal-rejected-phone');
      manifest.checks.push('Invalid draft prevented submission; completed saga changed both balances; rejected withdrawal preserved balance');
      const appearance = page.locator('details.spring-appearance');
      if (await appearance.getAttribute('open') === null) await appearance.locator('summary').press('Enter');
      for (const [label, theme] of [['Light', 'light'], ['High contrast', 'high-contrast'], ['Dark', 'dark']]) {
        await page.getByRole('group', { name: 'Color theme', exact: true }).getByRole('button', { name: label, exact: true }).press('Enter');
        await expect(page.locator('html')).toHaveAttribute('data-rf-theme', theme);
        await task('a', 'transfer');
        await page.getByLabel('Account A transfer amount (£)', { exact: true }).fill('12.');
        await expect(page.locator('#account-a-operations-panel').getByRole('button', { name: 'Start Transfer', exact: true })).toBeDisabled();
        await capture('money-' + theme + '-phone');
      }
      manifest.checks.push('All three themes selected by keyboard, with invalid draft, actual provider attributes and contrast/semantic audit');
      await page.emulateMedia({ forcedColors: 'active' });
      await capture('money-forced-colors-phone');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Deposit £', exact: true }).screenshot({ path: path.join(output, 'forced-colors-primary-control.png') });
      await page.emulateMedia({ forcedColors: 'none' });
      manifest.checks.push('Forced colors captured with invalid input and focus/error semantics; native primary control screenshot retained for manual label/backplate inspection');
      await page.getByLabel('Account A deposit amount (£)', { exact: true }).fill('10001');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Deposit £', exact: true }).tap();
      await balance(page, 'A', '£10,501.00');
      const source = await page.locator('#account-a-panel-heading code').innerText();
      await page.getByRole('button', { name: 'View Investigations', exact: true }).tap();
      const row = page.getByRole('region', { name: 'Flagged deposit entries', exact: true }).getByRole('row').filter({ hasText: source });
      await expect(row).toContainText('£10,001.00', { timeout: 120000 });
      await capture('investigation-populated-desktop', 1440, 900);
      await capture('investigation-populated-phone');
      manifest.checks.push('Real high-value deposit appears in separate live investigation queue');
      await page.goto(base + '/operations?a=' + encodeURIComponent(source) + '&b=unopened-capture-' + Date.now(), { waitUntil: 'domcontentloaded' });
      await balance(page, 'A', '£10,501.00');
      const compensationLedger = page.locator('#account-a-operations-panel tbody tr');
      await expect(compensationLedger.first()).toContainText('£10,001.00', { timeout: 120000 });
      const priorEntries = await compensationLedger.count();
      await task('a', 'transfer');
      await page.getByLabel('Account A transfer amount (£)', { exact: true }).fill('25');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Start Transfer', exact: true }).tap();
      await expect(page.locator('#account-a-transfer-status')).toContainText('Phase: Compensated', { timeout: 120000 });
      await balance(page, 'A', '£10,501.00');
      await expect(compensationLedger).toHaveCount(priorEntries + 2, { timeout: 120000 });
      await expect(compensationLedger.nth(0)).toContainText('Deposit');
      await expect(compensationLedger.nth(0)).toContainText('£25.00');
      await expect(compensationLedger.nth(1)).toContainText('Withdrawal');
      await expect(compensationLedger.nth(1)).toContainText('£25.00');
      await capture('transfer-compensated-phone');
      manifest.checks.push('Defined compensation after unopened destination reverses source withdrawal');
      await page.goto(base + '/auth-proof', { waitUntil: 'domcontentloaded' });
      const entity = 'capture-auth-' + Date.now();
      const readPath = '/api/projections/auth-proof/' + entity;
      await page.getByLabel('Auth Proof entity ID', { exact: true }).fill(entity);
      let authenticatedCount = 0;
      for (const persona of ['Unauthenticated', 'Operator Roles', 'AuthProof Role', 'AuthProof Claim', 'Full Access']) {
        const expectedRead = persona === 'Unauthenticated' ? 401 : ['AuthProof Claim', 'Full Access'].includes(persona) ? 200 : 403;
        const personaRead = page.waitForResponse(r => r.request().method() === 'GET' && r.url().endsWith(readPath) && hasHttpPersona(r.request(), persona));
        await page.getByRole('button', { name: persona, exact: true }).tap();
        const selectedRead = await personaRead;
        if (expectedRead === 200) {
          assert.ok([200, 404].includes(selectedRead.status()), 'Unexpected persona read status ' + selectedRead.status());
          await verifyProtectedRead(selectedRead, selectedRead.status());
          await refreshUntilCount(readPath, persona, authenticatedCount);
        } else {
          await verifyProtectedRead(selectedRead, expectedRead);
        }
        const repeatedPersonaRead = page.waitForResponse(r => r.request().method() === 'GET' && r.url().endsWith(readPath) && hasHttpPersona(r.request(), persona));
        await page.getByRole('button', { name: persona, exact: true }).tap();
        const repeatedRead = await repeatedPersonaRead;
        await verifyProtectedRead(repeatedRead, expectedRead);
        const commandRequest = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith('/' + entity + '/authenticated'));
        await page.getByRole('button', { name: 'Record Authenticated Access', exact: true }).tap();
        const commandResponse = await commandRequest;
        assert.equal(commandResponse.status(), persona === 'Unauthenticated' ? 401 : 200);
        assert.equal(await waitForGate(commandResponse.finished(), 'authenticated-command-complete'), null);
        if (persona !== 'Unauthenticated') authenticatedCount++;
        const response = page.getByRole('region', { name: 'Auth Proof command responses', exact: true }).getByRole('status');
        await expect(response).toContainText(persona === 'Unauthenticated' ? 'Latest request: failed.' : 'Latest response: accepted.');
        if (expectedRead === 200) {
          await refreshUntilCount(readPath, persona, authenticatedCount);
          await expect(page.getByLabel('Authenticated access events observed', { exact: true })).toHaveText(String(authenticatedCount));
        } else {
          const manualRead = page.waitForResponse(r => r.request().method() === 'GET' && r.url().endsWith(readPath) && hasHttpPersona(r.request(), persona));
          await page.getByRole('button', { name: 'Refresh protected read', exact: true }).tap();
          await verifyProtectedRead(await manualRead, expectedRead);
          await expect(page.locator('.spring-auth-outcome').getByRole('alert')).toContainText('HTTP ' + expectedRead);
          await expect(page.getByLabel('Authenticated access events observed', { exact: true })).toHaveCount(0);
        }
        await capture('auth-' + persona.replaceAll(' ', '-').toLowerCase() + '-phone');
      }
      manifest.checks.push('Selecting and reselecting every persona initiates a real correlated read with its expected HTTP authorization result');
      await expect(page.getByLabel('Authenticated access events observed', { exact: true })).toHaveText('4');
      let releaseOldRead;
      let oldReadStarted;
      const oldReadGate = new Promise(resolve => { releaseOldRead = resolve; });
      const oldReadWaiting = new Promise(resolve => { oldReadStarted = resolve; });
      let reads = 0;
      await page.route('**' + readPath, async route => {
        if (++reads === 1) { oldReadStarted(); await oldReadGate; }
        await route.continue();
      });
      const obsoleteResponse = page.waitForResponse(r => r.url().endsWith(readPath) && r.status() === 200);
      await page.getByRole('button', { name: 'Refresh protected read', exact: true }).tap();
      await waitForGate(oldReadWaiting, 'obsolete-protected-read-request');
      const deniedResponse = page.waitForResponse(r => r.url().endsWith(readPath) && r.status() === 401);
      await page.getByRole('button', { name: 'Unauthenticated', exact: true }).tap();
      assert.equal((await deniedResponse).status(), 401);
      await expect(page.locator('.spring-auth-outcome').getByRole('alert')).toContainText('401');
      releaseOldRead();
      const completedObsolete = await obsoleteResponse;
      assert.equal(completedObsolete.status(), 200);
      assert.equal(await waitForGate(completedObsolete.finished(), 'obsolete-protected-read-complete'), null);
      assert.equal((await completedObsolete.json()).authenticatedAccessCount, 4);
      await page.locator('details.spring-auth-snapshots summary').press('Enter');
      await expect(page.getByRole('heading', { name: 'Aggregate State Snapshot', exact: true })).toBeVisible();
      await expect(page.locator('.spring-auth-outcome').getByRole('alert')).toContainText('401');
      await expect(page.getByLabel('Authenticated access events observed', { exact: true })).toHaveCount(0);
      await capture('auth-obsolete-read-ignored-phone');
      await page.unroute('**' + readPath);
      await page.getByRole('button', { name: 'Full Access', exact: true }).tap();
      await expect(page.getByLabel('Authenticated access events observed', { exact: true })).toHaveText('4');
      manifest.checks.push('Real older allowed GET200 released after new persona GET401; current denied outcome retained independently of subscription snapshots');
      // The raw snapshots disclosure is already open after the ordering check.
      await capture('auth-snapshots-desktop', 1440, 900);
      manifest.checks.push('All five personas exercised; actual authenticated event count 4 through protected read; correlated read and raw snapshots remain discoverable');
      await page.goto(pair, { waitUntil: 'domcontentloaded' });
      await balance(page, 'A', '£10,501.00');
      await page.setViewportSize({ width: 320, height: 740 });
      await capture('money-narrow-phone', 320, 740);
      await context.setOffline(true);
      await expect(page.getByRole('region', { name: 'Live connection notice', exact: true })).toBeVisible({ timeout: 60000 });
      await capture('connection-offline-phone', 390, 844, false);
      await context.setOffline(false);
      await connected();
      await balance(page, 'A', '£10,501.00');
      manifest.checks.push('Real browser offline/reconnect preserves stale-view warning and resumes the connection');
      await page.goto(base + '/accounts', { waitUntil: 'domcontentloaded' });
      await connected();
      const custom = page.locator('details.spring-custom-accounts');
      if (await custom.getAttribute('open') === null) await custom.locator('summary').press('Enter');
      const longId = 'long-account-' + 'x'.repeat(100) + Date.now();
      await page.getByRole('textbox', { name: 'Account A ID', exact: true }).fill(longId);
      await page.getByRole('textbox', { name: 'Account B ID', exact: true }).fill('long-other-' + Date.now());
      await page.getByRole('button', { name: 'Use these accounts', exact: true }).tap();
      await connected();
      await page.getByLabel('Account A holder name', { exact: true }).fill('A very long holder name with multiple words '.repeat(6));
      await page.getByLabel('Account A initial deposit (£)', { exact: true }).fill('999999999.99');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Open Account', exact: true }).tap();
      await balance(page, 'A', '£999,999,999.99');
      await capture('long-account-phone');
      await page.locator('details.spring-tools summary').press('Enter');
      await capture('developer-tools-phone');
      manifest.checks.push('Real custom account, long ID/name/balance and unopened opposite account fit phone layout; developer tools remain discoverable');
      const allInputsLabeled = await page.locator('input:not([type=hidden])').evaluateAll(inputs => inputs.every(input => input.labels?.length || input.getAttribute('aria-label')));
      assert.equal(allInputsLabeled, true);
      manifest.checks.push('Visible/native inputs have accessible labels');
      const lostReplyPath = '**/api/aggregates/bank-account/' + encodeURIComponent(longId) + '/deposit';
      let committedRequests = 0;
      let committedStatus;
      let committedBody;
      await page.route(lostReplyPath, async route => {
        committedRequests++;
        const response = await route.fetch({ maxRetries: 0, maxRedirects: 0 });
        try {
          committedStatus = response.status();
          committedBody = await response.json();
          await route.abort('failed');
        } finally {
          await response.dispose();
        }
      });
      await page.getByLabel('Account A deposit amount (£)', { exact: true }).fill('1');
      await page.locator('#account-a-operations-panel').getByRole('button', { name: 'Deposit £', exact: true }).tap();
      const failedResponse = page.getByRole('region', { name: 'Banking responses · this browser', exact: true });
      await expect(failedResponse.getByRole('status')).toContainText('Latest request: failed.');
      await expect(failedResponse.getByRole('alert')).toContainText('Network error:');
      await expect(failedResponse).toContainText('does not establish the server outcome');
      await balance(page, 'A', '£1,000,000,000.99');
      const committedLedger = page.locator('#account-a-operations-panel tbody tr');
      await expect(committedLedger).toHaveCount(1);
      await expect(committedLedger.first()).toContainText('Deposit');
      await expect(committedLedger.first()).toContainText('£1.00');
      assert.equal(committedRequests, 1);
      assert.equal(committedStatus, 200);
      assert.equal(committedBody.success, true);
      manifest.lostReply = { requests: committedRequests, serverStatus: committedStatus, serverAccepted: committedBody.success, observedBalance: '£1,000,000,000.99', ledgerEntries: 1 };
      await capture('lost-reply-committed-phone');
      await page.unroute(lostReplyPath);
      manifest.checks.push('One real deposit committed with HTTP200/success before its browser reply was aborted; client failure remains visible alongside the actual balance and one ledger entry; no retry');
      await second.close();
    }
    manifest.phase = 'terminal-assertions';
    assert.equal(manifest.errors.filter(e => e.kind === 'exception').length, 0, 'Unexpected JavaScript exceptions');
    assert.equal(manifest.captures.reduce((n, c) => n + c.violations.length, 0), 0, 'Accessibility violations remain');
    manifest.completed = true;
    manifest.status = 'PASS';
    manifest.phase = 'complete';
  } catch (error) {
    manifest.status = 'FAIL';
    manifest.completed = false;
    manifest.failure = { phase: manifest.phase, route: page.url(), message: error.message };
    throw error;
  } finally {
    try {
      await browser.close();
    } catch (error) {
      manifest.status = 'FAIL';
      manifest.completed = false;
      manifest.failure ||= { phase: 'browser-close', message: error.message };
      throw error;
    } finally {
      fs.writeFileSync(path.join(output, 'manifest.json'), JSON.stringify(manifest, null, 2));
    }
  }
  console.log(JSON.stringify({ captures: manifest.captures.length, violations: manifest.captures.reduce((n, c) => n + c.violations.length, 0), exceptions: manifest.errors.filter(e => e.kind === 'exception').length }));
})().catch(error => { console.error(error); process.exitCode = 1; });
