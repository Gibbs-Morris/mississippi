# PR #819 rendered documentation evidence

These six inspected Playwright images show the explicit version 1 in the three changed Spring pages at desktop 1280x900 and mobile 390x844. Mobile code blocks are horizontally scrolled to their ends. The projection capture shows BankAccountBalanceProjection; browser assertions also checked all four projection storage declarations.

Source commit: 8ad9bafc26503d2406868a8ee5c5862717337435.
Source tree: f5846c341a8cd3ae070194c6365a800cf388f66d.

The production build is the docusaurus-build artifact 11316797593 from [successful CI run 37232739293](https://github.com/Gibbs-Morris/mississippi/actions/runs/37232739293). Its checkout commit ccc7491c5b12adc258d4372d3fe41aa26e8a8c8a has the same source tree. The production build and seven site regressions passed in CI. Local Playwright passed thirteen checks against this artifact, then a single mobile recapture passed after waiting for the page and fonts to settle.

To reproduce in a checkout of the source commit, download that build artifact to .scratchpad/pr819/ci-docs-build. Copy capture-config.cjs to .scratchpad/pr819/docs-playwright-ci.config.cjs and capture.spec.ts to docs/Docusaurus/tests/pr819.evidence.spec.ts. From docs/Docusaurus run:

    npm ci --ignore-scripts
    npx playwright install chromium
    npx playwright test --config ../../.scratchpad/pr819/docs-playwright-ci.config.cjs

The capture configuration starts a Docusaurus production server on its own local port with one Chromium worker. evidence.json records each route, viewport, capture state and SHA-256.

Related records: [PR #819](https://github.com/Gibbs-Morris/mississippi/pull/819), [issue #718](https://github.com/Gibbs-Morris/mississippi/issues/718), [campaign #966](https://github.com/Gibbs-Morris/mississippi/issues/966).
