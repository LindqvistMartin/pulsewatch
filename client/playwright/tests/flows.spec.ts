import { test, expect } from '@playwright/test'

// These tests require a running backend at localhost:5000.
// Run locally with: dotnet run --project src/PulseWatch.Api (from repo root)
// then: npx playwright test
// In CI: set BACKEND_AVAILABLE=true to enable.

const backendAvailable = !!process.env['BACKEND_AVAILABLE']

test.describe('probe flows', () => {
  test.skip(!backendAvailable, 'requires backend at localhost:5000 and BACKEND_AVAILABLE=true')

  test('create probe via form and verify it appears in table', async ({ page }) => {
    await page.goto('/dashboard')
    await page.getByRole('button', { name: /add probe/i }).click()
    await page.getByLabel('Name').fill('Test API')
    await page.getByLabel('URL').fill('https://httpbin.org/status/200')
    await page.getByRole('button', { name: /create probe/i }).click()
    await expect(page.getByRole('cell', { name: 'Test API' })).toBeVisible()
  })

  test('probe detail page renders stat cards and chart', async ({ page }) => {
    await page.goto('/dashboard')
    // Click the first probe link in the table
    const probeLink = page.getByTestId('probe-table').getByRole('link').first()
    await probeLink.click()
    await expect(page.getByTestId('stat-card-status')).toBeVisible()
    await expect(page.getByTestId('response-time-chart')).toBeVisible()
  })

  test('probe row updates last-check column without page reload', async ({ page }) => {
    await page.goto('/dashboard')
    // Wait for SignalR to deliver a check update (up to 25s)
    await expect(
      page.getByTestId('probe-table').getByText(/seconds? ago/).first()
    ).toBeVisible({ timeout: 25_000 })
  })
})
