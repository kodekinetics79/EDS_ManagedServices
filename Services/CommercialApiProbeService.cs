using System.Diagnostics;
using Evostel.ManagedServices.Web.Models;

namespace Evostel.ManagedServices.Web.Services;

public sealed class CommercialApiProbeService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<HealthProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Evostel:CommercialApiBaseUrl"]?.TrimEnd('/')
            ?? "https://evostel-latest.runasp.net";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await httpClient.GetAsync(
                $"{baseUrl}/api/commercial/ProductCollections",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new("unavailable", null, DateTimeOffset.UtcNow,
                    $"Commercial API returned HTTP {(int)response.StatusCode}.");
            }

            var latency = (int)stopwatch.ElapsedMilliseconds;
            return latency > 1500
                ? new("degraded", latency, DateTimeOffset.UtcNow, "Live API responded slowly.")
                : new("healthy", latency, DateTimeOffset.UtcNow, "Live API responded successfully.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new("unavailable", null, DateTimeOffset.UtcNow, "Live API probe timed out.");
        }
        catch (HttpRequestException)
        {
            return new("unavailable", null, DateTimeOffset.UtcNow, "Live API could not be reached.");
        }
    }
}
