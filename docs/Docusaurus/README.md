# Mississippi Documentation Site

This is a minimal Docusaurus site for Mississippi framework documentation.

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

## Versioning

This site is configured with versioning support. To create a new version:

```bash
npm run docusaurus docs:version 1.0.0
```

## Features

- Minimal setup with essential features only
- Versioning support configured
- Playwright tests for build and runtime verification
- GitHub Actions workflow for CI/CD
- Dark mode support (respects system preference)

## Structure

- `docs/` - Documentation content (current/next version)
- `src/` - Custom pages and components
- `static/` - Static assets
- `tests/` - Playwright tests
- `docusaurus.config.ts` - Site configuration
- `sidebars.ts` - Sidebar navigation configuration

## GitHub Actions

The site is automatically built and tested on every push and pull request via the `.github/workflows/docusaurus.yml` workflow.
