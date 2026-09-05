using System.Text;
using Evostel.ManagedServices.Web.Models;
using Evostel.ManagedServices.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Evostel.ManagedServices.Web.Controllers;

[ApiController]
public sealed class OperationsController(
    IManagedServicesData data,
    CommercialApiProbeService probeService) : ControllerBase
{
    [HttpGet("/api/operations/health")]
    public async Task<ActionResult<HealthProbeResult>> Health(CancellationToken cancellationToken) =>
        Ok(await probeService.ProbeAsync(cancellationToken));

    [HttpPost("/api/operations/incidents/{incidentId}/acknowledge")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(string incidentId, CancellationToken cancellationToken)
    {
        var incident = await data.AcknowledgeAsync(incidentId, cancellationToken);
        return incident is null ? NotFound(new { message = "Incident was not found." }) : Ok(incident);
    }

    [HttpPost("/api/operations/tickets")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request, CancellationToken cancellationToken) =>
        Ok(await data.CreateTicketAsync(request, cancellationToken));

    [HttpPost("/api/operations/integrations/{integrationId}/check")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIntegration(string integrationId, CancellationToken cancellationToken)
    {
        await Task.Delay(650, cancellationToken);
        var integration = await data.CompleteIntegrationCheckAsync(integrationId, cancellationToken);
        return integration is null ? NotFound(new { message = "Integration was not found." }) : Ok(integration);
    }

    [HttpGet("/reports/weekly.txt")]
    public async Task<IActionResult> WeeklyText(CancellationToken cancellationToken)
    {
        var openIncidents = await data.OpenIncidentCountAsync(cancellationToken);
        var report = $"""
            Evostel Managed Services — Weekly Performance Report
            Period: 28 August – 3 September 2026
            Availability: 99.94%
            API success rate: 99.72%
            Open incidents: {openIncidents}
            SLA compliance: 96.8%

            Generated from the representative dashboard dataset. Validate live figures before client distribution.
            """;
        return File(Encoding.UTF8.GetBytes(report), "text/plain; charset=utf-8", "evostel-weekly-managed-services-report.txt");
    }

    [HttpGet("/reports/weekly.csv")]
    public async Task<IActionResult> WeeklyCsv(CancellationToken cancellationToken)
    {
        var openIncidents = await data.OpenIncidentCountAsync(cancellationToken);
        var report = $"Metric,Value,Period\nAvailability,99.94%,28 Aug - 3 Sep 2026\nAPI success rate,99.72%,28 Aug - 3 Sep 2026\nOpen incidents,{openIncidents},Current\nSLA compliance,96.8%,28 Aug - 3 Sep 2026\n";
        return File(Encoding.UTF8.GetBytes(report), "text/csv; charset=utf-8", "evostel-weekly-managed-services-report.csv");
    }
}
