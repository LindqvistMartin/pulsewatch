import { test, expect } from '@playwright/test'
import { readFileSync } from 'fs'
import { join } from 'path'

const backendAvailable = !!process.env['BACKEND_AVAILABLE']
const apiBaseUrl = process.env['VITE_API_URL'] ?? 'http://localhost:5035'

test.describe('status page smoke', () => {
  test('unknown slug does not crash the app', async ({ page }) => {
    await page.goto('/#/p/this-slug-does-not-exist-ever')
    await expect(page.locator('body')).toBeVisible()
  })
})

test.describe('status page backend', () => {
  test.skip(!backendAvailable, 'set BACKEND_AVAILABLE=true with backend running at VITE_API_URL')

  test('yaml import creates status page', async ({ request }) => {
    const yaml = readFileSync(join(__dirname, '../fixtures/valid.yaml'), 'utf-8')
    const resp = await request.post(`${apiBaseUrl}/api/v1/yaml-import`, {
      headers: { 'Content-Type': 'text/yaml' },
      data: yaml,
    })
    expect(resp.status()).toBe(200)
  })

  test('status page route renders after yaml import', async ({ request, page }) => {
    const yaml = readFileSync(join(__dirname, '../fixtures/valid.yaml'), 'utf-8')
    await request.post(`${apiBaseUrl}/api/v1/yaml-import`, {
      headers: { 'Content-Type': 'text/yaml' },
      data: yaml,
    })
    await page.goto('/#/p/e2e-status')
    await expect(page.getByText('E2E Status')).toBeVisible()
  })
})
