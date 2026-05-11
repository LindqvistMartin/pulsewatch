import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './playwright/scripts',
  testMatch: 'take-screenshots.spec.ts',
  timeout: 120_000,
  workers: 1,
  reporter: 'list',
  use: {
    headless: true,
    ...devices['Desktop Chrome'],
  },
})
