using Evostel.ManagedServices.Web.Models;

namespace Evostel.ManagedServices.Web.Services;

public sealed class ManagedServicesStore
{
    private readonly object _gate = new();
    private int _nextTicket = 3322;

    private readonly List<ServiceStatus> _services =
    [
        new() { Id = "storefront", Name = "Storefront", Status = "healthy", Latency = 22, Availability = 99.98m, Owner = "Evostel Digital", Source = "Synthetic monitor", Detail = "Public storefront navigation and checkout entry points." },
        new() { Id = "commercial-api", Name = "Commercial API", Status = "degraded", Latency = 486, Availability = 99.72m, Owner = "Kode Kinetics", Source = "Live catalogue probe", Detail = "Commerce API used for products, customers, carts, and orders." },
        new() { Id = "d365", Name = "D365 F&O", Status = "healthy", Latency = 118, Availability = 99.91m, Owner = "Evostel IT", Source = "Integration adapter", Detail = "Customer, inventory, sales order, payment, cancellation, and return synchronization." },
        new() { Id = "payments", Name = "Payments", Status = "healthy", Latency = 240, Availability = 99.95m, Owner = "Finance Operations", Source = "Gateway adapter", Detail = "Hosted card payment initiation and order payment verification." },
        new() { Id = "notifications", Name = "Notifications", Status = "healthy", Latency = 96, Availability = 99.89m, Owner = "Support Lead", Source = "Application monitor", Detail = "Customer order, support, and operational notification delivery." },
        new() { Id = "ticketing", Name = "Ticketing", Status = "healthy", Latency = 134, Availability = 99.96m, Owner = "Service Desk", Source = "D365 ticketing adapter", Detail = "L1, L2, and L3 support requests and escalation tracking." }
    ];

    private readonly List<Incident> _incidents =
    [
        new() { Id = "INC-1043", Title = "Elevated API latency", Service = "Commercial API", Severity = "high", Owner = "Kode Kinetics", Elapsed = "2h 14m", SlaRemaining = "5h 46m", SlaPercent = 72, Status = "investigating", OpenedAt = "4 Sep 2026, 12:13 AST", Summary = "Catalogue requests are completing above the normal response-time threshold. Functionality remains available." },
        new() { Id = "INC-1042", Title = "Payment webhook retries", Service = "Payments", Severity = "medium", Owner = "Evostel IT", Elapsed = "4h 32m", SlaRemaining = "3h 28m", SlaPercent = 43, Status = "in-progress", OpenedAt = "4 Sep 2026, 09:55 AST", Summary = "A subset of payment callbacks required retry. No duplicate order creation has been observed." },
        new() { Id = "INC-1040", Title = "Notification delivery delays", Service = "Notifications", Severity = "medium", Owner = "Support Lead", Elapsed = "6h 11m", SlaRemaining = "1h 49m", SlaPercent = 22, Status = "in-progress", OpenedAt = "4 Sep 2026, 08:16 AST", Summary = "Transactional notifications are queued longer than the service target during peak processing." },
        new() { Id = "INC-1039", Title = "Inventory sync variance", Service = "D365 F&O", Severity = "low", Owner = "Evostel IT", Elapsed = "8h 05m", SlaRemaining = "15h 55m", SlaPercent = 66, Status = "monitoring", OpenedAt = "4 Sep 2026, 06:22 AST", Summary = "Three products reported a delayed stock refresh and reconciled on the next scheduled run." },
        new() { Id = "INC-1037", Title = "Search indexing lag", Service = "Storefront", Severity = "low", Owner = "Kode Kinetics", Elapsed = "1d 2h", SlaRemaining = "21h 10m", SlaPercent = 88, Status = "monitoring", OpenedAt = "3 Sep 2026, 12:17 AST", Summary = "New catalogue items took longer than expected to appear in search results." },
        new() { Id = "INC-1034", Title = "Ticket attachment timeout", Service = "Ticketing", Severity = "low", Owner = "Service Desk", Elapsed = "1d 7h", SlaRemaining = "16h 42m", SlaPercent = 70, Status = "resolved", OpenedAt = "3 Sep 2026, 07:31 AST", Summary = "Large support-ticket attachments intermittently timed out. Upload limits were adjusted and verified." }
    ];

    private readonly List<SupportTicket> _tickets =
    [
        new() { Id = "ST-3321", Subject = "Payment query", Customer = "Al Noor Trading", Level = "L1", Age = "1h 12m", Owner = "Service Desk", Status = "open" },
        new() { Id = "ST-3318", Subject = "Order delivery date", Customer = "Riyadh Industrial", Level = "L2", Age = "5h 04m", Owner = "Sales Operations", Status = "in-progress" },
        new() { Id = "ST-3316", Subject = "Product inventory mismatch", Customer = "Gulf Process Co.", Level = "L2", Age = "8h 47m", Owner = "Evostel IT", Status = "in-progress" },
        new() { Id = "ST-3312", Subject = "Checkout error", Customer = "Eastern Supplies", Level = "L3", Age = "14h 20m", Owner = "Kode Kinetics", Status = "escalated" },
        new() { Id = "ST-3309", Subject = "Account approval status", Customer = "Jubail Automation", Level = "L1", Age = "18h 08m", Owner = "Service Desk", Status = "open" }
    ];

    private readonly List<IntegrationRun> _integrations =
    [
        new() { Id = "INT-01", Name = "Product and inventory sync", System = "D365 F&O", Cadence = "Every 15 min", LastRun = "14:15 AST", Result = "success", Records = "1,284", Owner = "Evostel IT" },
        new() { Id = "INT-02", Name = "Customer registration", System = "D365 AR", Cadence = "On demand", LastRun = "13:52 AST", Result = "success", Records = "3", Owner = "Evostel IT" },
        new() { Id = "INT-03", Name = "Sales order creation", System = "D365 F&O", Cadence = "On checkout", LastRun = "13:41 AST", Result = "success", Records = "12", Owner = "Kode Kinetics" },
        new() { Id = "INT-04", Name = "Advance payment journal", System = "D365 F&O", Cadence = "On payment", LastRun = "12:09 AST", Result = "warning", Records = "8 / 9", Owner = "Finance Operations" },
        new() { Id = "INT-05", Name = "Order notifications", System = "Notification service", Cadence = "Event-driven", LastRun = "14:22 AST", Result = "success", Records = "47", Owner = "Support Lead" }
    ];

    private readonly List<ActivityItem> _activities =
    [
        new() { Time = "14:27", Title = "Commercial API latency", Detail = "Response time exceeded threshold", Tone = "warning" },
        new() { Time = "13:12", Title = "Incident INC-1042 updated", Detail = "Status changed to Investigating", Tone = "info" },
        new() { Time = "11:03", Title = "D365 F&O sync completed", Detail = "All records processed", Tone = "success" },
        new() { Time = "09:44", Title = "New support ticket", Detail = "ST-3321 — Payment query", Tone = "info" },
        new() { Time = "08:17", Title = "Incident INC-1041 resolved", Detail = "Total time 2h 14m", Tone = "success" },
        new() { Time = "06:52", Title = "Weekly report generated", Detail = "Performance and SLA report", Tone = "info" },
        new() { Time = "04:21", Title = "Security scan completed", Detail = "No critical issues found", Tone = "success" }
    ];

    private static readonly List<ReportItem> ReportRows =
    [
        new() { Id = "RPT-091", Name = "Weekly managed services report", Period = "28 Aug – 3 Sep 2026", Generated = "4 Sep 2026, 06:52 AST", Owner = "Operations Lead", Status = "ready" },
        new() { Id = "RPT-090", Name = "API and integration performance", Period = "August 2026", Generated = "1 Sep 2026, 07:00 AST", Owner = "Evostel IT", Status = "ready" },
        new() { Id = "RPT-089", Name = "Support SLA analysis", Period = "August 2026", Generated = "1 Sep 2026, 07:02 AST", Owner = "Support Lead", Status = "ready" },
        new() { Id = "RPT-088", Name = "Compliance control review", Period = "Q3 2026", Generated = "Pending review", Owner = "Security Lead", Status = "draft" }
    ];

    private static readonly List<ComplianceItem> ComplianceRows =
    [
        new() { Control = "Encryption in transit", Framework = "Project Charter / TLS", Owner = "Evostel IT", Status = "verified", Reviewed = "3 Sep 2026" },
        new() { Control = "Encryption at rest", Framework = "Project Charter / AES-256", Owner = "Infrastructure", Status = "evidence-needed", Reviewed = "30 Aug 2026" },
        new() { Control = "Role-based access", Framework = "Project Charter / RBAC", Owner = "Security Lead", Status = "verified", Reviewed = "2 Sep 2026" },
        new() { Control = "MFA for privileged access", Framework = "Project Charter / MFA", Owner = "Security Lead", Status = "in-review", Reviewed = "1 Sep 2026" },
        new() { Control = "Incident escalation", Framework = "Architecture §9.2", Owner = "Operations Lead", Status = "verified", Reviewed = "4 Sep 2026" },
        new() { Control = "Quarterly compliance audit", Framework = "Post-launch support", Owner = "Compliance Owner", Status = "scheduled", Reviewed = "15 Sep 2026" }
    ];

    public DashboardViewModel Snapshot(string activePage)
    {
        lock (_gate)
        {
            return new()
            {
                ActivePage = activePage,
                Services = _services.Select(Clone).ToList(),
                Incidents = _incidents.Select(Clone).ToList(),
                Tickets = _tickets.Select(Clone).ToList(),
                Integrations = _integrations.Select(Clone).ToList(),
                Activities = _activities.ToList(),
                Reports = ReportRows.ToList(),
                Compliance = ComplianceRows.ToList()
            };
        }
    }

    public int OpenIncidentCount
    {
        get { lock (_gate) return _incidents.Count(incident => incident.Status != "resolved"); }
    }

    public Incident? Acknowledge(string incidentId)
    {
        lock (_gate)
        {
            var incident = _incidents.FirstOrDefault(row => row.Id.Equals(incidentId, StringComparison.OrdinalIgnoreCase));
            if (incident is null) return null;
            incident.Status = "acknowledged";
            _activities.Insert(0, new() { Time = DateTime.Now.ToString("HH:mm"), Title = $"Incident {incident.Id} acknowledged", Detail = $"Owner: {incident.Owner}", Tone = "success" });
            return Clone(incident);
        }
    }

    public SupportTicket CreateTicket(CreateTicketRequest request)
    {
        lock (_gate)
        {
            var ticket = new SupportTicket
            {
                Id = $"ST-{_nextTicket++}", Subject = request.Subject.Trim(), Customer = request.Customer.Trim(),
                Level = request.Level, Age = "Just now", Owner = request.Level == "L3" ? "Kode Kinetics" : "Service Desk", Status = "open"
            };
            _tickets.Insert(0, ticket);
            _activities.Insert(0, new() { Time = DateTime.Now.ToString("HH:mm"), Title = "New support ticket", Detail = $"{ticket.Id} — {ticket.Subject}", Tone = "info" });
            return ticket;
        }
    }

    public IntegrationRun? CompleteIntegrationCheck(string integrationId)
    {
        lock (_gate)
        {
            var integration = _integrations.FirstOrDefault(row => row.Id.Equals(integrationId, StringComparison.OrdinalIgnoreCase));
            if (integration is null) return null;
            integration.Result = "success";
            integration.LastRun = $"{DateTime.Now:HH:mm} AST";
            return Clone(integration);
        }
    }

    private static ServiceStatus Clone(ServiceStatus row) => new() { Id = row.Id, Name = row.Name, Status = row.Status, Latency = row.Latency, Availability = row.Availability, Owner = row.Owner, Source = row.Source, Detail = row.Detail };
    private static Incident Clone(Incident row) => new() { Id = row.Id, Title = row.Title, Service = row.Service, Severity = row.Severity, Owner = row.Owner, Elapsed = row.Elapsed, SlaRemaining = row.SlaRemaining, SlaPercent = row.SlaPercent, Status = row.Status, OpenedAt = row.OpenedAt, Summary = row.Summary };
    private static SupportTicket Clone(SupportTicket row) => new() { Id = row.Id, Subject = row.Subject, Customer = row.Customer, Level = row.Level, Age = row.Age, Owner = row.Owner, Status = row.Status };
    private static IntegrationRun Clone(IntegrationRun row) => new() { Id = row.Id, Name = row.Name, System = row.System, Cadence = row.Cadence, LastRun = row.LastRun, Result = row.Result, Records = row.Records, Owner = row.Owner };
}
