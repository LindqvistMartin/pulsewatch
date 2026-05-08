# ADR 003 — PostgreSQL Materialized Views for SLO Rollups

**Status:** Accepted  
**Date:** 2026-05-08

## Context

SLO calculations require aggregating availability and latency percentiles over 7-day and 30-day
windows. `HealthChecks` grows at `probe_count × checks_per_day` rows. At 50 active probes with
60-second intervals, that is ~72,000 rows per day. A 30-day SLO window scans ~2.16M rows per
calculation tick, which runs every 60 seconds.

Options considered:

| Option | Trade-offs |
|--------|-----------|
| Raw query on `HealthChecks` per tick | Simple, but full table scan every 60s per SLO definition |
| TimescaleDB hypertables | Excellent query performance, but requires a non-standard Postgres extension — breaks plain-Postgres deploy on Render/Neon |
| PostgreSQL materialized views | No extra dependencies; one-time 60s refresh amortizes the scan cost across all SLO calculations |
| Application-level in-memory aggregation | No DB overhead for calculations, but loses state on restart and requires separate persistence |

## Decision

Use three PostgreSQL materialized views (`health_check_1m`, `health_check_1h`, `health_check_1d`)
refreshed every 60 seconds by `RollupRefresher`. SLO windows up to 30 days query `health_check_1h`;
larger windows query `health_check_1d`. `REFRESH MATERIALIZED VIEW CONCURRENTLY` requires a unique
index on `(probe_id, bucket)`, which the `SloRollups` migration creates.

`SloCalculator` queries the appropriate view with a single aggregation rather than scanning
`HealthChecks` directly.

## Consequences

- SLO calculation queries run in milliseconds regardless of the `HealthChecks` table size.
- Materialized views contain only the last N days of data per their `WHERE` clause. Older rows
  remain in `HealthChecks` but are excluded from SLO windows automatically.
- `REFRESH MATERIALIZED VIEW CONCURRENTLY` on an empty view succeeds (produces an empty view).
  On a fresh instance the first SLO tick computes 100% availability (zero checks = zero failures).
  Correct data appears on the next tick after health checks accumulate.
- TimescaleDB remains a natural upgrade path if the instance grows beyond ~500 active probes or
  the 60-second refresh lag becomes unacceptable for real-time SLO dashboards.
