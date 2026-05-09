export type Org = {
  id: string
  name: string
  slug: string
  createdAt: string
}

export type Project = {
  id: string
  organizationId: string
  name: string
  slug: string
  createdAt: string
}

export type Probe = {
  id: string
  projectId: string
  name: string
  url: string
  method: string
  intervalSeconds: number
  isActive: boolean
  createdAt: string
  lastCheckedAt: string | null
  lastCheckSuccess: boolean | null
}

export type HealthCheck = {
  id: string
  statusCode: number | null
  responseTimeMs: number
  isSuccess: boolean
  failureReason: string | null
  checkedAt: string
}

export type SloMeasurement = {
  id: string
  computedAt: string
  availabilityPct: number
  p95LatencyMs: number | null
  errorBudgetTotalSeconds: number
  errorBudgetConsumedSeconds: number
  burnRate: number
  projectedExhaustionAt: string | null
}

export type SloDefinition = {
  id: string
  probeId: string
  targetAvailabilityPct: number
  windowDays: number
  targetLatencyP95Ms: number | null
  createdAt: string
  latestMeasurement: SloMeasurement | null
}

export type IncidentUpdate = {
  id: string
  status: string
  message: string
  createdAt: string
}

export type Incident = {
  id: string
  probeId: string
  openedAt: string
  closedAt: string | null
  reason: string
  autoDetected: boolean
  updates: IncidentUpdate[]
}

export type CreateOrgRequest = { name: string; slug: string }
export type CreateProjectRequest = { name: string; slug: string }

export type CreateAssertionRequest = {
  type: string
  operator: string
  expectedValue: string
  jsonPathExpression?: string
}

export type CreateProbeRequest = {
  name: string
  url: string
  intervalSeconds: number
  method?: string
  assertions?: CreateAssertionRequest[]
}

export type CreateSloRequest = {
  targetAvailabilityPct: number
  windowDays: number
  targetLatencyP95Ms?: number
}
