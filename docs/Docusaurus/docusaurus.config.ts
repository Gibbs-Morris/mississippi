import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'Mississippi Documentation',
  tagline: 'Documentation for the Mississippi framework',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Enable Mermaid diagram support
  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],

  // Set the production url of your site here
  // NOSONAR: hardcoded URL is intentional for GitHub Pages deployment configuration
  url: 'https://gibbs-morris.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/mississippi/',

  // GitHub pages deployment config.
  // NOSONAR: organization and project names are intentionally hardcoded
  organizationName: 'Gibbs-Morris',
  projectName: 'mississippi',

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Enable versioning
          versions: {
            current: {
              label: 'Next',
              path: 'next',
            },
          },
        },
        blog: false, // Disable blog
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Mississippi',
      logo: {
        alt: 'Mississippi Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'docsVersionDropdown',
          position: 'right',
        },
        {
          // NOSONAR: GitHub repository URL is intentionally hardcoded for navigation
          href: 'https://github.com/Gibbs-Morris/mississippi',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/next/intro',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              // NOSONAR: GitHub repository URL is intentionally hardcoded for footer link
              href: 'https://github.com/Gibbs-Morris/mississippi',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Mississippi Project. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash', 'json'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
