# ADR 002 — In-Process Channel Pipeline for Probe Execution

**Status:** Accepted  
**Date:** 2026-05-08

## Context

Probes must fire on configurable intervals (15 s – 1 h) without blocking each other. Options considered:

| Option | Problem |
|--------|---------|
| Run HTTP calls directly in the scheduler loop | One slow probe (10 s timeout) blocks all others |
| `Task.WhenAll` in the scheduler | Unbounded concurrency, no back-pressure |
| `Parallel.ForEachAsync` | Better, but couples scheduling and execution, no graceful drain |
| External queue (RabbitMQ, Redis Streams) | Operational overhead for a self-hosted single-binary tool |

## Decision

Use `System.Threading.Channels.Channel<ProbeJob>` as an in-process queue between one `ProbeScheduler` and four `ProbeWorker` instances:

```
ProbeScheduler (1×, 5 s tick)
    ↓ Channel<ProbeJob> (bounded 1000, DropOldest)
ProbeWorker (4×, concurrent consumers)
```

`ProbeJob` carries full `ProbeAssertion` objects (not just IDs) so workers do not need an extra DB round-trip to evaluate assertions.

`BoundedChannelFullMode.DropOldest` prevents unbounded memory growth under overload. A dropped job is recovered on the next scheduler tick (probes recheck `LastCheckedAt`).

## Consequences

- Zero external dependencies for the probe execution path.
- Four concurrent workers handle bursts without blocking each other.
- Graceful shutdown: `IHostApplicationLifetime.ApplicationStopping` triggers cancellation; `ReadAllAsync` drains the channel naturally, completing all in-flight probes before exit.
- Horizontal scaling beyond one process requires replacing the channel with a durable queue. This is documented here as the known scaling boundary rather than discovered later.
- `DropOldest` means a very slow probe could repeatedly be dropped if the channel stays full. In practice, the scheduler fires every 5 s and capacity is 1000 — this only becomes a concern above ~200 active probes with timeouts > 5 s.
