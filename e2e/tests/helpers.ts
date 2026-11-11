import { expect } from '@playwright/test'
import type { APIRequestContext } from '@playwright/test'

/** Unique per test and per run, so parallel tests and re-runs never see each other's data. */
export function uniqueCompany(prefix = 'E2E Co'): string {
  return `${prefix} ${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`
}

/** "YYYY-MM-DD", n days from today (negative for the past). */
export function isoDate(daysFromToday: number): string {
  const date = new Date()
  date.setDate(date.getDate() + daysFromToday)
  return date.toISOString().slice(0, 10)
}

/**
 * Creates an application through the API: fast setup for tests that are about something else than the form.
 * Returns its id.
 */
export async function createApplication(request: APIRequestContext, fields: Record<string, unknown>): Promise<string> {
  const response = await request.post('/api/applications', {
    data: { roleTitle: 'Senior .NET Developer', track: 'Developer', channel: 'LinkedIn', employmentType: 'Permanent', ...fields },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  return (await response.json()).id
}
