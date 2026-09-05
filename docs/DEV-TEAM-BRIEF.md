# Brief for the Evostel development team

## What this repository is

A managed-services operations dashboard for Evostel — a separate ASP.NET Core 8 MVC app
with Razor views and no client framework. It is deployed at
<https://eds-managedservices.onrender.com> and shows service health, an incident register,
integration run status, the L1/L2/L3 support queue, reports, and compliance readiness.

**Today, one panel is real.** The service rail measures live latency by probing the
Commercial API. Everything else — incidents, SLA figures, support tickets, integration
runs, compliance — is representative data hardcoded in
[`Services/ManagedServicesStore.cs`](../Services/ManagedServicesStore.cs). The dashboard
labels this on screen and does not present invented numbers as measured ones.

Making the rest real depends on endpoints that do not exist yet. That is what we need
from you.

## What we need, in priority order

### 1. Admin-scoped operations endpoints — this is the blocker

The endpoints the storefront uses are **customer-scoped**. `GET
/api/commercial/Orders/support-tickets` returns the signed-in customer's tickets;
`GET /api/commercial/Notifications` returns their notifications. A managed-services
dashboard needs the tenant-wide view — every ticket across every customer.

Two endpoints unblock two panels:

- `GET /api/operations/tickets` — all support tickets, all customers
- `GET /api/operations/activity` — the operational activity stream, tenant-wide

Exact payloads are specified in
[`docs/OPERATIONS-API-CONTRACT.md`](OPERATIONS-API-CONTRACT.md). Two mismatches to
resolve while you build them:

**Status vocabulary.** You return `Pending`, `PendingApproval`, `Approved`, `Completed`,
`Rejected`. The dashboard renders `open`, `in-progress`, `escalated`, `resolved` — these
map directly to CSS classes, so an unexpected value renders an unstyled badge. Decide
which side does the mapping and say which; either is fine, but it must be one of them.

**Missing fields.** Your tickets carry `orderId`, `subject`, `status`, `updated`. The
dashboard's support queue also shows support level (L1/L2/L3), customer, owner, and age.
If the L1/L2/L3 tier breakdown is to be real rather than representative, the API has to
supply a level. If it cannot, tell us and we will drop the tier columns rather than
invent them.

### 2. Integration run history

The integrations panel — product and inventory sync, customer registration, sales order
creation, advance payment journal, order notifications — is entirely invented. It needs,
per sync job: last run time, result, and record count.

**Prefer exposing your own log of these runs over reading D365's job history.** You know
what you attempted, including the runs that failed before reaching D365; D365 only knows
what arrived.

### 3. Decide the authentication model before we wire anything

The dashboard has no authentication at all right now, which is the blocker for putting it
in front of a client regardless of data. Two options:

- **A service credential** — the dashboard has its own identity and your API trusts it.
  Quicker to build.
- **Forwarding the signed-in user's token** — required for per-role visibility, e.g. an
  Evostel operations lead seeing different data from a Kode Kinetics engineer.

Retrofitting the second onto the first means redoing the call path, so decide now rather
than later.

### 4. Please do not give this dashboard D365 credentials

We were offered the D365 APIs and are declining them deliberately. The dashboard should
call your API only:

- You already own the D365 integration. A second path means two credential sets and two
  failure modes for the same data.
- D365 throttles. A dashboard that hits it on every page render will be rate-limited.
- The dashboard is publicly reachable and currently unauthenticated. D365
  service-principal credentials must not sit inside it.

If a panel needs D365 data, the right shape is: your API reads D365, the dashboard reads
your API.

## What we do not need from you

**Service health.** Already solved. The dashboard probes real endpoints on a timer and
reports measured status and latency. It needs no credentials, because any HTTP response —
including the 401s from token-protected endpoints — proves the service answered.

## What no API can fix

**Incidents and SLA.** These need a monitoring system of record — Application Insights,
Azure Monitor, or similar. There is nothing to expose because nothing is being captured.

**Compliance evidence.** Same: needs a system of record that does not exist yet.

Until those exist, both panels stay representative and stay labelled as such.

## Working in this repository

- Branch off `main`. Render auto-deploys `main` on push, so anything merged is live.
- **The seam is [`IManagedServicesData`](../Services/IManagedServicesData.cs).** Every page
  and every state change goes through it. To add a live source, implement that interface
  and register it in `Program.cs` where `Evostel:OperationsMode` is checked. Do not scatter
  HTTP calls through the controllers.
- `Evostel:OperationsMode` fails fast at startup if it is set to anything other than
  `Representative` while no live source is registered. That is deliberate — it prevents
  shipping a build that looks connected but is serving representative data.
- **Do not edit the Razor views or `site.js` to accommodate an API shape.** Map the API
  onto the existing models instead; the views are already accessible, responsive, and
  antiforgery-protected, and the status strings are load-bearing.
- Follow the failure pattern in
  [`Services/CommercialApiProbeService.cs`](../Services/CommercialApiProbeService.cs):
  typed `HttpClient`, explicit timeout, catch and return a labelled `unavailable` state
  rather than throwing. A live source must degrade section by section, so one dead
  endpoint cannot blank the whole dashboard.
- `SnapshotAsync` runs on every page render across nine routes. Cache, or normal
  navigation will hammer your API.

## Questions we need answered

1. Do admin-scoped ticket and activity endpoints exist anywhere already, or do they need
   building from scratch?
2. Is support level (L1/L2/L3) recorded against tickets at all? If not, where does
   escalation tier live?
3. Are integration runs logged anywhere today — your own tables, or only D365's job
   history?
4. Service credential or forwarded user token?
5. Is there a Swagger/OpenAPI document for the Commercial API? Nothing is served at
   `/swagger` on the deployed environment, and it would save a lot of guessing.
