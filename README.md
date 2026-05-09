# PulseWatch

**Self-hosted reliability dashboard. Probes, SLOs, public status pages — without the SaaS bill.**

[![MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com)
[![React](https://img.shields.io/badge/React-18-61dafb.svg)](https://react.dev)

---

<p align="center">
  <img src="docs/screenshots/dashboard.png" alt="PulseWatch dashboard — monitoring stack with mixed statuses" width="900"/>
</p>

<p align="center">
  <img src="docs/screenshots/probe-detail-outage.png" alt="Probe detail — outage detected, SLO burn rate 1000×, incident auto-opened" width="900"/>
</p>

---

## Why

Better Stack costs $29/month. Datadog costs $300/month. Uptime Kuma is great but has no SLOs, no REST API, no config-as-code. PulseWatch fills the gap: SRE-grade tracking with one `docker compose up`.

## Features

- **HTTP probes** with configurable assertions — status code, latency threshold, body regex, JSON path
- **SLO tracking** — availability, error budget burn rate, projected exhaustion over 7/30-day windows
- **Incident autodetection** — opens automatically when availability drops below target, closes on recovery
- **Real-time dashboard** via SignalR — no polling, no F5
- **REST API + OpenAPI** — full API surface at `/scalar`, multiple SDK clients generated
- **Multi-tenancy** — Organizations → Projects → Probes
- **Self-instrumented** — OpenTelemetry traces + `/metrics` Prometheus endpoint

## Roadmap

- Public status pages with incident timelines and 90-day historical bars
- YAML config-as-code (`pulsewatch.yaml` defines monitors + SLOs)
- Docker + Render deploy
- Slack/Discord webhook on SLO breach
- Distributed probing from multiple regions

## Architecture

```
ProbeScheduler (5 s tick)
    │
    │  Channel<ProbeJob> — bounded 1000, DropWrite
    ▼
ProbeWorker ×4 (concurrent)
    │  HTTP probe + assertion evaluation
    │
    ├─ HealthCheck row ──┐
    └─ OutboxMessage row─┤  single transaction
                         │
                    OutboxRelay (200 ms poll, FOR UPDATE SKIP LOCKED)
                         │
                    SignalR hub → browser (live dashboard)

RollupRefresher (60 s)
    └─ REFRESH MATERIALIZED VIEW CONCURRENTLY (health_check_1m/1h/1d)

SloCalculator (60 s)
    └─ reads health_check_1h/1d → writes SloMeasurement + auto-opens/closes Incidents
```

**Assertion engine:** `ProbeWorker` dispatches to `StatusCodeEvaluator`, `LatencyEvaluator`, `BodyRegexEvaluator`, or `JsonPathEvaluator`. All assertions must pass for `IsSuccess = true`.

Architecture Decision Records:
- [ADR 001 — Transactional Outbox](docs/adr/001-outbox-pattern.md)
- [ADR 002 — Channel Pipeline](docs/adr/002-channel-pipeline.md)
- [ADR 003 — PostgreSQL Rollups vs TimescaleDB](docs/adr/003-postgres-rollups-vs-timescale.md)

## Stack

**Backend** — .NET 10, ASP.NET Core Minimal API, EF Core 9, SignalR, PostgreSQL, Serilog  
**Frontend** — React 18, TypeScript, Vite, TanStack Query, shadcn/ui, Tailwind, recharts  
**Observability** — OpenTelemetry (traces + metrics), Prometheus `/metrics`, Scalar OpenAPI UI  
**Tests** — xunit, FluentAssertions, Testcontainers (PostgreSQL), WireMock.Net

<p align="center">
  <img src="docs/screenshots/api-docs.png" alt="Scalar API documentation — Organizations, Projects, Probes, SLOs, Incidents" width="900"/>
</p>

## Quick start

```bash
docker run --name pulse-pg \
  -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=pulsewatch \
  -p 5499:5432 -d postgres:16

# appsettings.Local.json (gitignored):
# { "ConnectionStrings": { "Postgres": "Host=localhost;Port=5499;Database=pulsewatch;Username=postgres;Password=dev" } }

dotnet run --project src/PulseWatch.Api
cd client && npm install && npm run dev
```

Backend API: `http://localhost:5035`  
Frontend: `http://localhost:5173`  
Scalar UI: `http://localhost:5035/scalar`  
Metrics: `http://localhost:5035/metrics`

## Create a probe with assertions

```bash
ORG=$(curl -s -X POST localhost:5035/api/v1/organizations \
  -H 'Content-Type: application/json' \
  -d '{"name":"My Org","slug":"my-org"}' | jq -r .id)

PROJ=$(curl -s -X POST localhost:5035/api/v1/organizations/$ORG/projects \
  -H 'Content-Type: application/json' \
  -d '{"name":"My Project","slug":"my-project"}' | jq -r .id)

curl -s -X POST localhost:5035/api/v1/projects/$PROJ/probes \
  -H 'Content-Type: application/json' \
  -d '{
    "name": "API Health",
    "url": "https://api.example.com/health",
    "intervalSeconds": 30,
    "assertions": [
      { "type": "StatusCode", "operator": "Equals",   "expectedValue": "200" },
      { "type": "LatencyMs",  "operator": "LessThan", "expectedValue": "500" },
      { "type": "JsonPath",   "operator": "Equals",   "expectedValue": "ok",
        "jsonPathExpression": "$.status" }
    ]
  }'
```

## SLO tracking

```bash
# Define an SLO: 99.9% availability over a 30-day window
curl -X POST localhost:5035/api/v1/projects/$PROJ/probes/$PROBE/slos \
  -H 'Content-Type: application/json' \
  -d '{"targetAvailabilityPct": 99.9, "windowDays": 30}'

# Read the latest measurement snapshot
curl localhost:5035/api/v1/projects/$PROJ/probes/$PROBE/slos
```

`latestMeasurement` includes `availabilityPct`, `burnRate`, `errorBudgetConsumedSeconds`, and `projectedExhaustionAt`. Incidents open automatically when availability drops below target and close on recovery.

## Run tests

```bash
dotnet test
```

Unit tests: < 1 s. Integration tests (Testcontainers): ~30 s.

## License

[MIT](LICENSE)
