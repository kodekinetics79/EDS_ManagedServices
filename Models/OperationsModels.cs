using System.ComponentModel.DataAnnotations;

namespace Evostel.ManagedServices.Web.Models;

public sealed class ServiceStatus
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Status { get; set; }
    public int Latency { get; set; }
    public decimal Availability { get; init; }
    public required string Owner { get; init; }
    public required string Source { get; init; }
    public required string Detail { get; init; }
}

public sealed class Incident
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Service { get; init; }
    public required string Severity { get; init; }
    public required string Owner { get; init; }
    public required string Elapsed { get; init; }
    public required string SlaRemaining { get; init; }
    public int SlaPercent { get; init; }
    public required string Status { get; set; }
    public required string OpenedAt { get; init; }
    public required string Summary { get; init; }
}

public sealed class SupportTicket
{
    public required string Id { get; init; }
    public required string Subject { get; init; }
    public required string Customer { get; init; }
    public required string Level { get; init; }
    public required string Age { get; init; }
    public required string Owner { get; init; }
    public required string Status { get; init; }

    private int AgeHours => int.TryParse(Age.Split('h')[0], out var hours) ? hours : 0;
    public bool IsNew => !Age.Contains('d') && AgeHours < 4;
    public bool IsAging => Age.Contains('d') || AgeHours > 12;
}

public sealed class IntegrationRun
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string System { get; init; }
    public required string Cadence { get; init; }
    public required string LastRun { get; set; }
    public required string Result { get; set; }
    public required string Records { get; init; }
    public required string Owner { get; init; }
}

public sealed class ActivityItem
{
    public required string Time { get; init; }
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public required string Tone { get; init; }
}

public sealed class ReportItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Period { get; init; }
    public required string Generated { get; init; }
    public required string Owner { get; init; }
    public required string Status { get; init; }
}

public sealed class ComplianceItem
{
    public required string Control { get; init; }
    public required string Framework { get; init; }
    public required string Owner { get; init; }
    public required string Status { get; init; }
    public required string Reviewed { get; init; }
}

public sealed class DashboardViewModel
{
    public required string ActivePage { get; init; }
    public required IReadOnlyList<ServiceStatus> Services { get; init; }
    public required IReadOnlyList<Incident> Incidents { get; init; }
    public required IReadOnlyList<SupportTicket> Tickets { get; init; }
    public required IReadOnlyList<IntegrationRun> Integrations { get; init; }
    public required IReadOnlyList<ActivityItem> Activities { get; init; }
    public required IReadOnlyList<ReportItem> Reports { get; init; }
    public required IReadOnlyList<ComplianceItem> Compliance { get; init; }
}

public sealed class IncidentTableViewModel
{
    public required IReadOnlyList<Incident> Incidents { get; init; }
    public bool Embedded { get; init; }
}

public sealed class CreateTicketRequest
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string Subject { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Customer { get; init; } = string.Empty;

    [Required, RegularExpression("L1|L2|L3")]
    public string Level { get; init; } = "L1";

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;
}

public sealed record HealthProbeResult(string State, int? Latency, DateTimeOffset CheckedAt, string Message);
