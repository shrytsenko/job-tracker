import { expect, test } from '@playwright/test'
import type { APIRequestContext, Page } from '@playwright/test'

// README screenshots, taken from the fictional demo data in the isolated e2e stack.
// Everything shown (companies, people, CV) is made up; nothing comes from a real job search.

const outputDir = '../docs/screenshots'

const DemoCv =
  'Fictional demo profile: senior .NET developer, 10 years of C#, ASP.NET Core, EF Core and SQL Server, ' +
  'plus test automation with Playwright. Based in Barcelona, EU citizen. English C1, Spanish B2. Open to remote EU roles.'

test.describe.configure({ mode: 'serial' })

test.beforeAll(async ({ request }) => {
  const profile = await request.put('/api/profile', { data: { cvSummary: DemoCv, fullName: 'Alex Demo' } })
  expect(profile.ok()).toBeTruthy()

  // A sample analysis on the US-only demo post, so the red flag is visible. Saved through the same
  // validation and backstop as a real one; the model name marks it as a sample.
  const litwareId = await findApplicationId(request, 'Litware Health')
  const analysis = await request.put(`/api/applications/${litwareId}/analysis`, {
    data: {
      jobDescription:
        'Senior .NET Engineer at Litware Health (fictional). Remote (US only); you must be authorized to work in the United States. ' +
        'Stack: C#, .NET 8, Azure, SQL Server, Kafka. Salary: $150-170k per year.',
      matchScore: 34,
      stack: ['C#', '.NET 8', 'Azure', 'SQL Server', 'Kafka'],
      seniority: 'Senior',
      locationRestrictions: 'Remote, US only',
      employmentType: 'Permanent',
      salary: '$150-170k per year',
      strengths: ['10 years of C# and ASP.NET Core backend work', 'Strong SQL Server and EF Core experience'],
      gaps: ['No Kafka experience in the CV', 'Limited Azure experience'],
      redFlags: [],
      model: 'Claude (sample)',
    },
  })
  expect(analysis.ok(), await analysis.text()).toBeTruthy()

  const nextAction = await request.put(`/api/applications/${litwareId}/next-action`, {
    data: { description: 'Ask the recruiter whether EU-based candidates are considered', dueOn: null },
  })
  expect(nextAction.ok()).toBeTruthy()
})

test('applications list', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('row').filter({ hasText: 'Northwind Traders' })).toBeVisible()
  await capture(page, 'applications.png')
})

test('application page with stage track, next action and job analysis', async ({ page, request }) => {
  const id = await findApplicationId(request, 'Litware Health')
  await page.goto(`/applications/${id}`)
  await expect(page.getByRole('heading', { name: 'Job analysis' })).toBeVisible()
  await expect(page.getByText(/Restricted to the US/)).toBeVisible()
  await capture(page, 'application.png', { fullPage: true })
})

test('dashboard', async ({ page }) => {
  await page.goto('/dashboard')
  await expect(page.getByRole('heading', { name: 'Funnel by track' })).toBeVisible()
  await capture(page, 'dashboard.png', { fullPage: true })
})

test('follow-ups', async ({ page }) => {
  await page.goto('/follow-ups')
  await expect(page.getByRole('heading', { name: 'Needs follow-up' })).toBeVisible()
  await capture(page, 'follow-ups.png')
})

async function capture(page: Page, file: string, options: { fullPage?: boolean } = {}) {
  // Let the web font and any last layout settle so every run produces the same picture.
  await page.evaluate(() => document.fonts.ready)
  await page.mouse.move(0, 0)
  await page.screenshot({ path: `${outputDir}/${file}`, fullPage: options.fullPage ?? false, animations: 'disabled' })
}

async function findApplicationId(request: APIRequestContext, company: string): Promise<string> {
  const response = await request.get(`/api/applications?search=${encodeURIComponent(company)}`)
  const applications: { id: string; company: string }[] = await response.json()
  const match = applications.find((a) => a.company === company)
  if (!match) throw new Error(`Demo application "${company}" not found. Run npm run stack:down, then stack:up, for fresh demo data.`)
  return match.id
}
