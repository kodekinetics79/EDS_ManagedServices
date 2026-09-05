# Evostel Managed Services Dashboard — ASP.NET Core

Native ASP.NET Core 8 MVC implementation of the Evostel managed-services dashboard. It uses C#, Razor views, and browser-native JavaScript/CSS. It does not require React, Vite, npm, or Node.js.

## Requirements

- .NET 8 SDK
- Visual Studio 2022 17.8+ with the **ASP.NET and web development** workload, or VS Code with C# Dev Kit

## Run from Visual Studio

1. Open `Evostel.ManagedServices.sln`.
2. Allow Visual Studio to restore the project.
3. Select the `https` profile.
4. Press **F5** or click **Run**.
5. The dashboard opens at `https://localhost:7174`.

If the local HTTPS certificate is not trusted, run once:

```powershell
dotnet dev-certs https --trust
```

## Run from the terminal

```bash
dotnet restore
dotnet run
```

Open `https://localhost:7174` or the URL printed by ASP.NET Core.

## Production build

```bash
dotnet publish -c Release -o ./publish
```

Run the published application:

```bash
dotnet ./publish/Evostel.ManagedServices.Web.dll
```

## Docker

```bash
docker build -t evostel-managed-services .
docker run --rm -p 8080:8080 evostel-managed-services
```

Open `http://localhost:8080`.

## Render

`render.yaml` is a Blueprint that builds the repository `Dockerfile` and runs one
always-on instance in Frankfurt, so the in-memory store and the Data Protection key
ring survive between requests.

1. Render dashboard, **New > Blueprint**, connect `kodekinetics79/EDS_ManagedServices`.
2. Render reads `render.yaml` and creates the service. Apply.

The app binds to `$PORT` when Render sets it and falls back to 8080 otherwise, so the
same image runs unchanged locally, on Render, and on any other container host.
`/healthz` is the health check and answers without calling the Commercial API, so an
upstream outage shows as a degraded badge in the UI rather than restarting the service.

## Fly.io (recommended host)

`fly.toml` runs the repository `Dockerfile` as a single always-on instance in `fra`,
which keeps the in-memory store and the Data Protection key ring stable between
requests — the two things autoscaling functions break.

```bash
flyctl apps create eds-managed-services --org personal
flyctl deploy --remote-only
```

`force_https` and `auto_stop_machines = 'off'` are already set. The app listens on
8080 inside the container; Fly terminates TLS and forwards `X-Forwarded-Proto`, which
the app honours.

## Vercel

Vercel has no .NET runtime, so the app deploys as a container image. `Dockerfile.vercel`
at the project root is detected automatically during the build; Vercel builds it, pushes
it to the Vercel Container Registry, and routes all traffic to the HTTP server it starts
on port 80. No `vercel.json` is required. Container Images must be enabled for the team.

Two consequences of running on autoscaling functions:

- The `ManagedServicesStore` is per-instance and in-memory, so tickets, acknowledgements,
  and integration-check results are lost when an instance scales down (5 minutes idle in
  production) and are not shared between instances.
- Data Protection keys are generated per instance, so an antiforgery token issued by one
  instance is rejected by another. Persist the key ring to shared storage before relying
  on the state-changing endpoints under load.

Neither affects the live Commercial API health probe, which is stateless.

## Configuration

Configuration is under the `Evostel` section in `appsettings.json`:

| Setting | Purpose |
|---|---|
| `CommercialApiBaseUrl` | Base URL used by the server-side Commercial API health probe |
| `OperationsMode` | Identifies the current representative operations mode |
| `SupportEmail` | Managed-services support contact |

For deployment, override values with environment variables such as:

```text
Evostel__CommercialApiBaseUrl=https://your-uat-api.example.com
```

Do not store credentials in `appsettings.json`. Use Azure Key Vault, environment secrets, or your approved secret manager.

## Included modules

- Executive managed-services overview
- Server-side live Commercial API health probe
- Service health rail and ownership details
- Incident register, filtering, details, and acknowledgement
- L1/L2/L3 support queues and validated ticket creation
- Integration run checks
- Downloadable TXT/CSV operational reports
- Compliance evidence-readiness view
- Operations activity and local preferences
- Responsive navigation, accessible dialogs, 403, 404, and 500 states

## Data and security boundary

Only the Commercial API health probe is live. Incidents, SLA figures, integration records, support tickets, activity, reports, and compliance records use representative in-memory data until Evostel supplies authenticated administrative and monitoring endpoints.

Before production deployment, integrate corporate SSO/RBAC/MFA, API-side authorization, persistent storage, audit logging, monitoring adapters, secure response headers, and client-approved escalation/SLA rules. The dashboard does not claim regulatory certification.

## Integration points

| Endpoint | Purpose |
|---|---|
| `GET /api/operations/health` | Server-side Commercial API probe |
| `POST /api/operations/incidents/{id}/acknowledge` | Acknowledge an incident |
| `POST /api/operations/tickets` | Create a support ticket |
| `POST /api/operations/integrations/{id}/check` | Run an integration check |
| `GET /reports/weekly.txt` | Download weekly text report |
| `GET /reports/weekly.csv` | Download weekly CSV report |

All state-changing endpoints require ASP.NET Core antiforgery tokens. Production APIs must additionally enforce authenticated role authorization.
