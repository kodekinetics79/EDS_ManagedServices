using System.Collections.Concurrent;
using System.Diagnostics;

namespace Evostel.ManagedServices.Web.Services;

public sealed record ServiceHealthReading(string Status, int? LatencyMs, DateTimeOffset CheckedAt);

public sealed class ServiceProbeTarget
{
    public string Id { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}

public interface IServiceHealthReadings
{
    ServiceHealthReading? Find(string serviceId);
}

// Probes the real Evostel endpoints on a timer and caches the results, so the service
// rail shows measured latency instead of hardcoded numbers without putting an outbound
// call on the render path of every page.
//
// Any HTTP response means the service answered, including 401 on the endpoints that
// require a customer token — reachability is what is being measured, not authorisation.
// Only transport failures, timeouts, and 5xx count as unavailable.
public sealed class ServiceHealthMonitor(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ServiceHealthMonitor> logger) : BackgroundService, IServiceHealthReadings
{
    private const int DegradedAboveMs = 1500;

    private readonly ConcurrentDictionary<string, ServiceHealthReading> _readings = new();

    public ServiceHealthReading? Find(string serviceId) =>
        _readings.TryGetValue(serviceId, out var reading) ? reading : null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var targets = configuration.GetSection("Evostel:ServiceProbes").Get<List<ServiceProbeTarget>>() ?? [];
        if (targets.Count == 0)
        {
            logger.LogInformation("No Evostel:ServiceProbes configured; the service rail stays representative.");
            return;
        }

        var interval = TimeSpan.FromSeconds(
            configuration.GetValue("Evostel:ServiceProbeIntervalSeconds", 60));

        using var timer = new PeriodicTimer(interval);
        do
        {
            // Sequentially, not in parallel: six concurrent requests to shared hosting
            // inflate every measurement and would report healthy services as degraded.
            foreach (var target in targets)
            {
                await ProbeAsync(target, stoppingToken);
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task ProbeAsync(ServiceProbeTarget target, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(target.Id) || string.IsNullOrWhiteSpace(target.Path)) return;

        var baseUrl = configuration["Evostel:CommercialApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return;

        var client = httpClientFactory.CreateClient("service-probe");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await client.GetAsync(
                $"{baseUrl}{target.Path}", HttpCompletionOption.ResponseHeadersRead, stoppingToken);
            stopwatch.Stop();

            var latency = (int)stopwatch.ElapsedMilliseconds;
            var status = (int)response.StatusCode >= 500
                ? "unavailable"
                : latency > DegradedAboveMs ? "degraded" : "healthy";

            Record(target.Id, status, latency);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down; leave the last good reading in place.
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            logger.LogDebug(exception, "Probe of {ServiceId} failed.", target.Id);
            Record(target.Id, "unavailable", null);
        }
    }

    private void Record(string serviceId, string status, int? latency) =>
        _readings[serviceId] = new ServiceHealthReading(status, latency, DateTimeOffset.UtcNow);
}
