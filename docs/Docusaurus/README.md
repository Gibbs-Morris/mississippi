# Mississippi Product And Documentation Site

This Docusaurus application publishes Mississippi's product landing page and
technical documentation from one build.

## Local Development

### Prerequisites

- Node.js 20.0 or higher
- npm

### Installation

```bash
npm install
```

### Build

```bash
npm run build
```

This command generates static content into the `build` directory that can be served using any static contents hosting service.

### Testing

```bash
# Install Playwright browsers (first time only)
npx playwright install chromium

# Run tests
npm test

# Run tests with headed browser
npm run test:headed
```

### Development Server

```bash
npm start
```

This command starts a local development server and opens up a browser window. Most changes are reflected live without having to restart the server.

### Serve Built Site

```bash
npm run serve
```

This command serves the built website locally for testing.

## Documentation Versions

The technical documentation currently publishes one live corpus at `/docs/`.
Do not create a version snapshot until Mississippi has a supported release whose
documentation must remain available alongside newer material.

When that lifecycle exists, Docusaurus can create the first snapshot with:

```bash
npm run docusaurus docs:version 1.0.0
```

## Features

- One unversioned product landing page
- One current technical documentation corpus
- Playwright tests for build and runtime verification
- GitHub Actions workflow for CI/CD
- Dark mode support (respects system preference)

## Structure

- `docs/` - Published technical documentation, organized by reader intent:
  getting started, tutorials, how-to guides, concepts, and reference
- `archive/` - Historical documentation retained in source but excluded from the
  published sidebar and routes, including the retired subsystem/page-type matrix
- `src/pages/` - The unversioned marketing landing page and its page-scoped styles
- `src/css/` - Shared site-shell and technical-documentation styles
- `static/` - Static assets
- `tests/` - Playwright tests
- `docusaurus.config.ts` - Site configuration
- `sidebars.ts` - Sidebar navigation configuration

## GitHub Actions

The site is automatically built and tested on every push and pull request via the `.github/workflows/docusaurus.yml` workflow.
