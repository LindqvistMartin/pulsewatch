/**
 * One-off screenshot script — run with:
 *   npx playwright test --config=playwright.screenshots.config.ts
 *
 * Requires the live site at pulsewatch-ui.onrender.com to be up with demo data.
 * Saves PNG files to docs/screenshots/ (relative to repo root).
 */
import { test, chromium, request as playwrightRequest } from '@playwright/test'
import path from 'path'
import { mkdir } from 'fs/promises'

const SITE = 'https://pulsewatch-ui.onrender.com'
const API  = 'https://pulsewatch-v074.onrender.com'
const OUT  = path.resolve(process.cwd(), '../docs/screenshots')

test.setTimeout(120_000)

test('take all portfolio screenshots', async () => {
  await mkdir(OUT, { recursive: true })

  // ── 1. Discover org / project / probe IDs via server-side request ─────────
  const apiCtx = await playwrightRequest.newContext({ baseURL: API })

  const orgs: { id: string }[] = await (await apiCtx.get('/api/v1/organizations')).json()
  if (!orgs.length) throw new Error('No organizations found on live instance')

  const orgId = orgs[0].id
  const projects: { id: string }[] = await (
    await apiCtx.get(`/api/v1/organizations/${orgId}/projects`)
  ).json()
  const projId = projects[0].id

  const probes: { id: string; name: string }[] = await (
    await apiCtx.get(`/api/v1/projects/${projId}/probes`)
  ).json()
  console.log('Probes found:', probes.map(p => p.name))

  // Use status page snapshot to identify which probes are Down vs Healthy
  const snapshot: { probes: { id: string; status: string }[] } = await (
    await apiCtx.get('/public/status/demo')
  ).json()
  const downProbeId   = snapshot.probes.find(p => p.status === 'Down')?.id   ?? probes[probes.length - 1].id
  const healthyProbeId = snapshot.probes.find(p => p.status === 'Healthy')?.id ?? probes[0].id
  console.log('Down probe:',    probes.find(p => p.id === downProbeId)?.name)
  console.log('Healthy probe:', probes.find(p => p.id === healthyProbeId)?.name)

  await apiCtx.dispose()

  // ── 2. Browser setup: inject localStorage before any page load ────────────
  const browser = await chromium.launch({ headless: true })
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    colorScheme: 'dark',
  })

  // addInitScript fires before React initialises — localStorage is ready
  await ctx.addInitScript(({ o, p }: { o: string; p: string }) => {
    localStorage.setItem('pw-org-id', o)
    localStorage.setItem('pw-project-id', p)
  }, { o: orgId, p: projId })

  const page = await ctx.newPage()

  // ── 3. Status page ────────────────────────────────────────────────────────
  await page.goto(`${SITE}/#/p/demo`, { waitUntil: 'networkidle', timeout: 60_000 })
  await page.waitForTimeout(1500)
  await page.screenshot({ path: path.join(OUT, 'status-page.png') })
  console.log('✓ status-page.png')

  // ── 4. Dashboard ──────────────────────────────────────────────────────────
  await page.goto(`${SITE}/#/dashboard`, { waitUntil: 'networkidle', timeout: 60_000 })
  await page.waitForTimeout(2500)
  await page.screenshot({ path: path.join(OUT, 'dashboard.png') })
  console.log('✓ dashboard.png')

  // ── 5. Healthy probe detail ───────────────────────────────────────────────
  await page.goto(`${SITE}/#/probes/${healthyProbeId}`, { waitUntil: 'networkidle', timeout: 60_000 })
  await page.waitForTimeout(2500)
  await page.screenshot({ path: path.join(OUT, 'probe-detail.png') })
  console.log(`✓ probe-detail.png  (${probes.find(p => p.id === healthyProbeId)?.name})`)

  // ── 6. Down probe detail ──────────────────────────────────────────────────
  await page.goto(`${SITE}/#/probes/${downProbeId}`, { waitUntil: 'networkidle', timeout: 60_000 })
  await page.waitForTimeout(2500)
  await page.screenshot({ path: path.join(OUT, 'probe-detail-outage.png') })
  console.log(`✓ probe-detail-outage.png  (${probes.find(p => p.id === downProbeId)?.name})`)

  // ── 7. Scalar API docs ────────────────────────────────────────────────────
  await page.goto(`${API}/scalar`, { waitUntil: 'networkidle', timeout: 60_000 })
  await page.waitForTimeout(2000)
  await page.screenshot({ path: path.join(OUT, 'api-docs.png') })
  console.log('✓ api-docs.png')

  await browser.close()
  console.log(`\nAll screenshots saved to:\n  ${OUT}`)
})
