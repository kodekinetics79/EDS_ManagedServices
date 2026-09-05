# Verification Record

## Completed in the build workspace

- Confirmed the solution contains no Node.js, npm, React, Vite, or third-party client runtime dependency.
- Confirmed Razor views reference the complete dashboard route set.
- Confirmed every state-changing browser request sends the ASP.NET Core antiforgery token.
- Confirmed user-supplied ticket values are inserted with DOM text nodes rather than HTML injection.
- Confirmed live API calls execute server-side through `HttpClient` with a 15-second timeout.
- Confirmed representative operational data is labelled separately from live health telemetry.
- Confirmed responsive, focus-visible, reduced-motion, empty-filter, 403, 404, and error states are present.

## Executed on .NET 8 (4 September 2026)

```bash
dotnet build Evostel.ManagedServices.sln -c Release   # succeeded, 0 warnings, 0 errors
dotnet publish -c Release -o ./publish                # succeeded
```

Driven with a headless browser at desktop 1440x1000 and mobile 390x844:

- All routes return the expected status: `/`, `/services`, `/integrations`, `/incidents`, `/support`, `/reports`, `/compliance`, `/settings`, `/activity` return 200; `/forbidden` returns 403; an unknown route returns 404.
- The Commercial API probe reached the live endpoint and returned a healthy state with a measured latency.
- State-changing endpoints return 400 without an antiforgery token and succeed with one; invalid ticket payloads return 400 with per-field messages.
- Incident acknowledgement, integration run checks, ticket creation, incident filtering, the empty-filter state, and preference saving all complete through the browser UI.
- No JavaScript console errors, and no horizontal document overflow at 390px.
- In the Production environment an unhandled error renders the branded error page rather than a stack trace.

## Defects found and fixed during that run

- `Dashboard.cshtml` parsed ticket age with `int.Parse`, so any ticket created through the UI (`Age = "Just now"`) made the overview page return 500 for the life of the process. Age predicates now live on `SupportTicket` and parse defensively.
- The service-details dialog handler bound to `[data-service]`, which also matched every incident row, so opening an incident also opened an empty service dialog on top of it and blocked the acknowledge button. Service-rail nodes now carry `data-service-node`.
- `status-escalated` had no stylesheet rule, so escalated tickets rendered without a status pill.
- The overview metric and the weekly reports quoted a hard-coded open-incident count that disagreed with the incident register; both now read the live count.

## Still required before production

Authenticated Evostel roles, API-side authorization, persistent storage, audit logging, monitoring adapters, secure response headers, and production-shaped support data. An automated test project should be added; the checks above were driven manually.
