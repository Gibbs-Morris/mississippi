const path = require('path');
const { defineConfig, devices } = require(path.resolve(__dirname, '../../docs/Docusaurus/node_modules/@playwright/test'));
module.exports = defineConfig({
  testDir: path.resolve(__dirname, '../../docs/Docusaurus/tests'),
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['json', { outputFile: path.resolve(__dirname, 'docs-playwright-results.json') }]],
  outputDir: path.resolve(__dirname, 'docs-test-results'),
  use: {
    baseURL: 'http://127.0.0.1:33189/mississippi',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm exec -- docusaurus serve --dir ../../.scratchpad/pr819/ci-docs-build --port 33189 --host 127.0.0.1 --no-open',
    cwd: path.resolve(__dirname, '../../docs/Docusaurus'),
    url: 'http://127.0.0.1:33189/mississippi',
    reuseExistingServer: false,
    timeout: 120000
  }
});

