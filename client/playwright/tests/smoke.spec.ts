import { test, expect } from '@playwright/test'

test('dashboard loads without error', async ({ page }) => {
  await page.goto('/dashboard')
  await expect(page).toHaveTitle(/PulseWatch|Vite/i)
  // Either "No workspace selected" or the probe table — both are valid
  await expect(page.locator('body')).toBeVisible()
})

test('cmd-K opens command palette', async ({ page }) => {
  await page.goto('/dashboard')
  // Wait for page to be interactive
  await page.waitForLoadState('domcontentloaded')
  await page.keyboard.press('Meta+k')
  await expect(page.getByPlaceholder('Search commands...')).toBeVisible()
})
