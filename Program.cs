using Microsoft.AspNetCore.HttpOverrides;
using Evostel.ManagedServices.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpClient<CommercialApiProbeService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
// Evostel:OperationsMode selects the data source behind IManagedServicesData.
// "Representative" serves the built-in dataset. A live implementation calling the
// Evostel operations API registers here instead — see docs/OPERATIONS-API-CONTRACT.md.
var operationsMode = builder.Configuration["Evostel:OperationsMode"] ?? "Representative";
if (!string.Equals(operationsMode, "Representative", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"Evostel:OperationsMode is '{operationsMode}' but no live data source is registered. " +
        "Implement IManagedServicesData against the Evostel operations API and register it here, " +
        "or set the mode back to 'Representative'.");
}

builder.Services.AddHttpClient("service-probe", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddSingleton<ServiceHealthMonitor>();
builder.Services.AddSingleton<IServiceHealthReadings>(sp => sp.GetRequiredService<ServiceHealthMonitor>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ServiceHealthMonitor>());

builder.Services.AddSingleton<ManagedServicesStore>();
builder.Services.AddSingleton<IManagedServicesData>(sp => sp.GetRequiredService<ManagedServicesStore>());
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Overview}/{id?}");

app.Run();

public partial class Program { }
