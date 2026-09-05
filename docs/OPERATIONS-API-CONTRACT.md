# Evostel Operations API — contract required by the managed-services dashboard

The dashboard reads and writes operational data through one interface,
`IManagedServicesData` ([Services/IManagedServicesData.cs](../Services/IManagedServicesData.cs)).
Today the only implementation is `ManagedServicesStore`, which serves a representative
dataset. To show real numbers, the main Evostel project needs to expose the endpoints
below; the dashboard then adds an implementation that calls them and registers it in
`Program.cs` where `Evostel:OperationsMode` is checked.

Field names and value sets are not arbitrary — they are what the Razor views and
`wwwroot/js/site.js` already render. Status strings map directly to CSS classes
(`status-open`, `severity-high`, and so on), so returning an unexpected value produces
an unstyled badge rather than an error.

## Read endpoints

All return `200` with a JSON array. All are called on page render, so they should be
fast and cacheable.

### `GET /api/operations/services`

```json
[
  {
    "id": "commercial-api",
    "name": "Commercial API",
    "status": "healthy",
    "latency": 486,
    "availability": 99.72,
    "owner": "Kode Kinetics",
    "source": "Live catalogue probe",
    "detail": "Commerce API used for products, customers, carts, and orders."
  }
]
```

`status` — one of `healthy`, `degraded`, `unavailable`. `latency` in milliseconds,
`availability` as a percentage.

### `GET /api/operations/incidents`

```json
[
  {
    "id": "INC-1043",
    "title": "Elevated API latency",
    "service": "Commercial API",
    "severity": "high",
    "owner": "Kode Kinetics",
    "elapsed": "2h 14m",
    "slaRemaining": "5h 46m",
    "slaPercent": 72,
    "status": "investigating",
    "openedAt": "4 Sep 2026, 12:13 AST",
    "summary": "Catalogue requests are completing above the normal threshold."
  }
]
```

`severity` — `high`, `medium`, `low`. `status` — `investigating`, `in-progress`,
`monitoring`, `acknowledged`, `resolved`. `slaPercent` drives the progress bar (0-100).
`elapsed`, `slaRemaining` and `openedAt` are display strings, formatted server-side.

Anything not `resolved` counts as open; that count drives the overview metric and the
weekly reports.

### `GET /api/operations/tickets`

```json
[
  {
    "id": "ST-3321",
    "subject": "Payment query",
    "customer": "Al Noor Trading",
    "level": "L1",
    "age": "1h 12m",
    "owner": "Service Desk",
    "status": "open"
  }
]
```

`level` — `L1`, `L2`, `L3`. `status` — `open`, `in-progress`, `escalated`, `resolved`.

`age` is parsed for the overview's "new / aging" split: a value containing `d` counts as
aging, otherwise the leading number before `h` is read as whole hours (under 4 is new,
over 12 is aging). Values that do not parse are treated as zero hours, so `"Just now"` is
safe. Prefer `"1h 12m"`, `"18h 08m"` or `"1d 2h"`.

### `GET /api/operations/integrations`

```json
[
  {
    "id": "INT-01",
    "name": "Product and inventory sync",
    "system": "D365 F&O",
    "cadence": "Every 15 min",
    "lastRun": "14:15 AST",
    "result": "success",
    "records": "1,284",
    "owner": "Evostel IT"
  }
]
```

`result` — `success`, `warning`, `unavailable`. `records` is a display string, so
`"8 / 9"` is valid.

### `GET /api/operations/activity`

```json
[
  { "time": "14:27", "title": "Commercial API latency", "detail": "Response time exceeded threshold", "tone": "warning" }
]
```

`tone` — `info`, `success`, `warning`.

### `GET /api/operations/reports` and `GET /api/operations/compliance`

Match `ReportItem` and `ComplianceItem` in
[Models/OperationsModels.cs](../Models/OperationsModels.cs).

## Write endpoints

The dashboard currently mutates its in-memory copy. Once connected, these must be the
system of record — otherwise the dashboard shows a ticket that does not exist upstream.

### `POST /api/operations/incidents/{id}/acknowledge`

Returns the updated incident in the shape above, or `404` if the id is unknown.

### `POST /api/operations/tickets`

```json
{ "subject": "...", "customer": "...", "level": "L2", "description": "..." }
```

Validation already enforced dashboard-side, and worth enforcing again server-side:
subject 3-120 characters, customer 2-100, description 10-1000, level matching `L1|L2|L3`.
Returns the created ticket, including the id the main system assigned.

### `POST /api/operations/integrations/{id}/check`

Triggers a run check and returns the updated integration record, or `404`.

## Cross-cutting requirements

**Authentication.** The dashboard has none today, which is the blocker for putting it in
front of a client. Decide the model before wiring: a service credential (the dashboard
has its own identity and the API trusts it) is quicker, but forwarding the signed-in
user's token is what you need for per-role visibility, and retrofitting it later means
redoing the call path. Whichever you choose, the dashboard must not hold API secrets in
`appsettings.json` — use environment variables or a secret manager.

**Failure behaviour.** Do not let an upstream outage take the dashboard down. Follow the
pattern already in [Services/CommercialApiProbeService.cs](../Services/CommercialApiProbeService.cs):
a typed `HttpClient` with a timeout, catching `HttpRequestException` and cancellation,
returning a labelled `unavailable` state rather than throwing. The live implementation of
`IManagedServicesData` should degrade section by section, falling back to representative
data with a visible marker, so one dead endpoint does not blank the whole dashboard.

**Caching.** `SnapshotAsync` runs on every page render across nine routes. Without a short
cache in front of these calls, normal navigation will hammer the API.

**Partial cutover.** Because everything goes through one interface, sections can go live
one at a time. A live implementation can call the API for incidents and integrations while
still returning representative tickets, so you are not blocked on the whole surface being
ready at once.
