using Evostel.ManagedServices.Web.Models;

namespace Evostel.ManagedServices.Web.Services;

// The single seam between the dashboard and its data source. Every page and every
// state change goes through this interface, so pointing the dashboard at the Evostel
// operations API means adding one implementation and registering it in Program.cs.
// See docs/OPERATIONS-API-CONTRACT.md for the endpoints and payloads that requires.
public interface IManagedServicesData
{
    Task<DashboardViewModel> SnapshotAsync(string activePage, CancellationToken cancellationToken);

    Task<int> OpenIncidentCountAsync(CancellationToken cancellationToken);

    Task<Incident?> AcknowledgeAsync(string incidentId, CancellationToken cancellationToken);

    Task<SupportTicket> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken);

    Task<IntegrationRun?> CompleteIntegrationCheckAsync(string integrationId, CancellationToken cancellationToken);
}
