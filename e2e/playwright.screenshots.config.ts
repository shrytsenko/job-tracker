import { defineConfig, devices } from '@playwright/test'

/**
 * Captures the README screenshots from the isolated e2e stack (fictional demo data, never your own).
 * Run with: npm run screenshots   (after npm run stack:up)
 */
export default defineConfig({
  testDir: './screenshots',
  globalSetup: './global-setup.ts',
  // One page at a time: the captures share the demo data they set up.
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8081',
    ...devices['Desktop Chrome'],
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: 'light',
  },
})
