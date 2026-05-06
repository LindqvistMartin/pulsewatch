# PulseWatch

Real-time HTTP endpoint monitoring dashboard. Add URLs, watch status and response time live without refreshing the page.

> Early WIP — solution scaffold only. Backend logic, frontend, and deploy land in the next iterations.

## Planned stack

**Backend** — .NET 10, ASP.NET Core Minimal API, EF Core, SignalR, PostgreSQL, Serilog
**Frontend** — React 18, TypeScript, Vite, TanStack Query, Radix UI, Tailwind CSS, recharts
**Deploy** — Docker on Render, Neon Postgres

## Run locally

```
dotnet run --project src/PulseWatch.Api
```

`GET /healthz` returns `{ "status": "ok" }`.

## License

[MIT](LICENSE)
