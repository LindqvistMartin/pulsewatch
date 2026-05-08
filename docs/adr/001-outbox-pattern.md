# ADR 001 — Transactional Outbox for Real-Time Events

**Status:** Accepted  
**Date:** 2026-05-08

## Context

`ProbeWorker` must broadcast each `HealthCheck` result to connected browsers via SignalR so the dashboard updates without polling. The naive approach — call `IHubContext.SendAsync` directly inside the probe execution — has two failure modes:

1. The process crashes between `db.SaveChanges()` and the hub send. The health check is in the DB but the browser never sees it.
2. The hub send is attempted while the DB transaction is still open, creating a race where a connected client might query the API before the row is visible.

## Decision

Use the **Transactional Outbox** pattern:

- `ProbeWorker` writes an `OutboxMessage` row in the **same transaction** as the `HealthCheck` row. Either both commit or neither does.
- `OutboxRelay` (a `BackgroundService` in the `Api` layer) polls the `outbox_messages` table every 2 seconds with `FOR UPDATE SKIP LOCKED`, dispatches to the SignalR hub, and marks rows `processed_at = now()`.

`OutboxRelay` lives in the `Api` project (not `Infrastructure`) because it needs `IHubContext<PulseHub>`, which is an ASP.NET Core SignalR type. Infrastructure must not depend on ASP.NET Core.

## Consequences

- Probe execution is decoupled from message delivery: a SignalR hiccup does not affect probe recording.
- Up to 2-second relay lag before the browser sees an update. Acceptable for a reliability dashboard.
- `FOR UPDATE SKIP LOCKED` makes the relay safe to run in multiple replicas without double-dispatch.
- If relay dispatch fails, the row stays unprocessed and is retried on the next batch. Downstream clients may receive duplicate events; they are idempotent (they call `queryClient.invalidateQueries`, not append).
- The outbox table grows unbounded unless old processed rows are pruned (fast-follow task).
