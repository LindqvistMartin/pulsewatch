# PulseWatch

Self-hosted reliability dashboard. Probes, SLOs, public status pages — without the SaaS bill.

## Features

- HTTP probes with configurable assertions (status code, latency threshold, body regex, JSON path)
- SLO tracking: availability + error budget burn rate over configurable windows
- Incident autodetection when availability drops below SLO target
- Real-time dashboard updates over SignalR — no polling
- YAML config-as-code + REST API + OpenAPI (Scalar UI at `/scalar`)
- Multi-tenancy: Organizations → Projects → Probes
- Self-instrumented: OpenTelemetry traces + `/metrics` Prometheus endpoint

## Roadmap

- Public status pages with incident timelines
- React 18 frontend (TanStack Query, shadcn/ui, recharts)
- Docker + Render deploy
- Slack/Discord webhook on SLO breach

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

**Assertion engine:** `ProbeWorker` passes `(statusCode, responseTimeMs, body)` to `AssertionEvaluatorFactory`, which dispatches to `StatusCodeEvaluator`, `LatencyEvaluator`, `BodyRegexEvaluator`, or `JsonPathEvaluator`. All assertions must pass for `IsSuccess = true`.

Architecture Decision Records:
- [ADR 001 — Transactional Outbox](docs/adr/001-outbox-pattern.md)
- [ADR 002 — Channel Pipeline](docs/adr/002-channel-pipeline.md)
- [ADR 003 — PostgreSQL Rollups vs TimescaleDB](docs/adr/003-postgres-rollups-vs-timescale.md)

## Stack

**Backend** — .NET 10, ASP.NET Core Minimal API, EF Core 9, SignalR, PostgreSQL, Serilog  
**Frontend** — React 18, TypeScript, Vite, TanStack Query, shadcn/ui, Tailwind, recharts *(planned)*  
**Deploy** — Docker, Render, Neon Postgres *(planned)*

## Quick start

```bash
docker run --name pulse-pg \
  -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=pulsewatch \
  -p 5499:5432 -d postgres:16

# appsettings.Local.json (gitignored):
# { "ConnectionStrings": { "Postgres": "Host=localhost;Port=5499;Database=pulsewatch;Username=postgres;Password=dev" } }

dotnet run --project src/PulseWatch.Api
```

API: `http://localhost:5000`  
Scalar UI: `http://localhost:5000/scalar`  
Metrics: `http://localhost:5000/metrics`

## Create a probe with assertions

```bash
# 1. Create org + project
ORG=$(curl -s -X POST localhost:5000/api/v1/organizations \
  -H 'Content-Type: application/json' \
  -d '{"name":"My Org","slug":"my-org"}' | jq -r .id)

PROJ=$(curl -s -X POST localhost:5000/api/v1/organizations/$ORG/projects \
  -H 'Content-Type: application/json' \
  -d '{"name":"My Project","slug":"my-project"}' | jq -r .id)

# 2. Add a probe with assertions
PROBE=$(curl -s -X POST localhost:5000/api/v1/projects/$PROJ/probes \
  -H 'Content-Type: application/json' \
  -d '{
    "name": "API Health",
    "url": "https://api.example.com/health",
    "intervalSeconds": 30,
    "assertions": [
      { "type": "StatusCode", "operator": "Equals", "expectedValue": "200" },
      { "type": "LatencyMs",  "operator": "LessThan", "expectedValue": "500" },
      { "type": "JsonPath",   "operator": "Equals", "expectedValue": "ok",
        "jsonPathExpression": "$.status" }
    ]
  }' | jq -r .id)
```

## SLO Tracking

Define an SLO for any probe. PulseWatch computes availability, error budget, and burn rate
automatically every 60 seconds using PostgreSQL materialized rollup views.

```bash
# Define an SLO: 99.9% availability over a 30-day window
curl -X POST localhost:5000/api/v1/projects/$PROJ/probes/$PROBE/slos \
  -H 'Content-Type: application/json' \
  -d '{"targetAvailabilityPct": 99.9, "windowDays": 30}'

# Read the latest measurement snapshot
curl localhost:5000/api/v1/projects/$PROJ/probes/$PROBE/slos
```

Response includes `latestMeasurement` with:
- `availabilityPct` — rolling availability over the window
- `burnRate` — ratio of actual to allowed error consumption (>1 = burning too fast)
- `errorBudgetConsumedSeconds` / `errorBudgetTotalSeconds`
- `projectedExhaustionAt` — when the budget runs out at current rate

```bash
# Incidents (auto-opened when availability drops below target)
curl localhost:5000/api/v1/projects/$PROJ/probes/$PROBE/incidents
```

## Run tests

```bash
dotnet test
```

Unit tests: < 1 s. Integration tests (Testcontainers): ~30 s.

## License

[MIT](LICENSE)
