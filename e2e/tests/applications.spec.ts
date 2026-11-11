import { expect, test } from '@playwright/test'
import { createApplication, isoDate, uniqueCompany } from './helpers'

// The three main flows from the project brief: create an application, change its status,
// see it in follow-ups. Locators use roles and visible labels, the way a user finds things.

test('create an application through the form and find it in the list', async ({ page }) => {
  const company = uniqueCompany()

  await page.goto('/applications/new')
  // The company field suggests existing names, so it is a combobox rather than a plain textbox.
  await page.getByRole('combobox', { name: /^Company/ }).fill(company)
  await page.getByRole('textbox', { name: /^Role title/ }).fill('Senior .NET Developer')
  await page.getByRole('combobox', { name: /^Status/ }).selectOption('Applied')
  await page.getByRole('button', { name: 'Create application' }).click()

  // Lands on the new application's page.
  await expect(page).toHaveURL(/\/applications\/[0-9a-f-]{36}$/)
  await expect(page.getByRole('heading', { level: 1, name: company })).toBeVisible()

  // And it is listed, with the status chosen in the form.
  await page.getByRole('link', { name: 'Applications', exact: true }).click()
  const row = page.getByRole('row').filter({ hasText: company })
  await expect(row).toBeVisible()
  await expect(row).toContainText('Applied')
})

test('change the status on the stage track and keep it after a reload', async ({ page, request }) => {
  const company = uniqueCompany()
  const id = await createApplication(request, { company, status: 'Applied' })

  await page.goto(`/applications/${id}`)
  await page.getByRole('button', { name: /^Tech interview/ }).click()

  const techStage = page.getByRole('button', { name: /^Tech interview/ })
  await expect(techStage).toHaveAttribute('aria-pressed', 'true')

  // Saved on the server, not just in the page.
  await page.reload()
  await expect(page.getByRole('button', { name: /^Tech interview/ })).toHaveAttribute('aria-pressed', 'true')

  // Closing it keeps the stage where it ended.
  await page.getByRole('button', { name: 'Rejected' }).click()
  await expect(page.getByText('Rejected after tech interview.')).toBeVisible()
})

test('an overdue next action shows up in follow-ups', async ({ page, request }) => {
  const company = uniqueCompany()
  await createApplication(request, {
    company,
    status: 'Applied',
    nextActionDescription: 'Follow up with the recruiter',
    // Yesterday, so the test does not depend on the browser and server agreeing on today's date.
    nextActionDueOn: isoDate(-1),
  })

  await page.goto('/follow-ups')

  const row = page.getByRole('row').filter({ hasText: company })
  await expect(row).toBeVisible()
  await expect(row).toContainText('Action due')
  await expect(row).toContainText('Follow up with the recruiter')

  // The nav badge counts it too.
  await expect(page.getByRole('link', { name: /^Follow-ups/ }).getByLabel(/need follow-up/)).toBeVisible()
})
