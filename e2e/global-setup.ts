import type { FullConfig } from '@playwright/test'

/**
 * Runs once before the tests:
 * 1. refuses to run against the everyday stack (port 8080), which holds real job-hunt data;
 * 2. waits until the API behind the UI answers, so the first test doesn't race the database migration.
 */
export default async function globalSetup(config: FullConfig): Promise<void> {
  const baseURL = config.projects[0].use.baseURL
  if (!baseURL) throw new Error('No baseURL configured.')

  const url = new URL(baseURL)
  if (url.port === '8080' && !process.env.E2E_ALLOW_PORT_8080) {
    throw new Error(
      'Refusing to run end-to-end tests against port 8080: that is the everyday stack with real data. ' +
        'Use the isolated stack (npm run stack:up, port 8081).',
    )
  }

  const deadline = Date.now() + 120_000
  let lastError = ''
  while (Date.now() < deadline) {
    try {
      const response = await fetch(new URL('/api/dashboard', baseURL))
      if (response.ok) return
      lastError = `HTTP ${response.status}`
    } catch (error) {
      lastError = String(error)
    }
    await new Promise((resolve) => setTimeout(resolve, 2_000))
  }

  throw new Error(`The app at ${baseURL} did not become ready within 2 minutes (${lastError}). Did you run npm run stack:up?`)
}
