import { expect, test } from '@playwright/test'
import { createApplication, uniqueCompany } from './helpers'

// The security headers nginx adds, and proof that the app still works under its Content-Security-Policy.

test('pages and API responses carry the security headers', async ({ request }) => {
  for (const path of ['/', '/dashboard', '/api/dashboard']) {
    const response = await request.get(path)
    const headers = response.headers()

    expect(headers['content-security-policy'], path).toContain("default-src 'self'")
    expect(headers['content-security-policy'], path).toContain("frame-ancestors 'none'")
    expect(headers['x-content-type-options'], path).toBe('nosniff')
    expect(headers['x-frame-options'], path).toBe('DENY')
    expect(headers['referrer-policy'], path).toBe('strict-origin-when-cross-origin')
    expect(headers['server'] ?? '', path).not.toMatch(/nginx\/\d/)
  }
})

test('the main pages render without Content-Security-Policy violations', async ({ page, request }) => {
  const id = await createApplication(request, { company: uniqueCompany('CSP Co'), status: 'Applied' })

  // Collect every violation the browser reports, from the first script on.
  await page.addInitScript(() => {
    const w = window as unknown as { cspViolations: string[] }
    w.cspViolations = []
    document.addEventListener('securitypolicyviolation', (e) => {
      w.cspViolations.push(`${e.violatedDirective}: ${e.blockedURI}`)
    })
  })

  for (const path of [
    '/',
    `/applications/${id}`,
    '/applications/new',
    '/follow-ups',
    '/dashboard',
    '/contacts',
    '/profile',
  ]) {
    await page.goto(path)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    const violations = await page.evaluate(() => (window as unknown as { cspViolations: string[] }).cspViolations)
    expect(violations, `CSP violations on ${path}`).toEqual([])
  }
})
