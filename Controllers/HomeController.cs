using Evostel.ManagedServices.Web.Models;
using Evostel.ManagedServices.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Evostel.ManagedServices.Web.Controllers;

public sealed class HomeController(IManagedServicesData data) : Controller
{
    [HttpGet("/")]
    public Task<IActionResult> Overview(CancellationToken cancellationToken) => Dashboard("overview", cancellationToken);

    [HttpGet("/services")]
    public Task<IActionResult> Services(CancellationToken cancellationToken) => Dashboard("services", cancellationToken);

    [HttpGet("/integrations")]
    public Task<IActionResult> Integrations(CancellationToken cancellationToken) => Dashboard("integrations", cancellationToken);

    [HttpGet("/incidents")]
    public Task<IActionResult> Incidents(CancellationToken cancellationToken) => Dashboard("incidents", cancellationToken);

    [HttpGet("/support")]
    public Task<IActionResult> Support(CancellationToken cancellationToken) => Dashboard("support", cancellationToken);

    [HttpGet("/reports")]
    public Task<IActionResult> Reports(CancellationToken cancellationToken) => Dashboard("reports", cancellationToken);

    [HttpGet("/compliance")]
    public Task<IActionResult> Compliance(CancellationToken cancellationToken) => Dashboard("compliance", cancellationToken);

    [HttpGet("/settings")]
    public Task<IActionResult> Settings(CancellationToken cancellationToken) => Dashboard("settings", cancellationToken);

    [HttpGet("/activity")]
    public Task<IActionResult> Activity(CancellationToken cancellationToken) => Dashboard("activity", cancellationToken);

    [HttpGet("/forbidden")]
    public Task<IActionResult> ForbiddenPage(CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Dashboard("forbidden", cancellationToken);
    }

    // UseExceptionHandler routes here, so this action must not depend on the data
    // source succeeding. A live source that is failing is exactly why we arrived.
    [HttpGet("/error")]
    public async Task<IActionResult> Error(CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        try
        {
            return await Dashboard("error", cancellationToken);
        }
        catch (Exception)
        {
            return View("Dashboard", DashboardViewModel.Empty("error"));
        }
    }

    [HttpGet("/{*path}", Order = 999)]
    public Task<IActionResult> NotFoundPage(CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return Dashboard("not-found", cancellationToken);
    }

    private async Task<IActionResult> Dashboard(string page, CancellationToken cancellationToken) =>
        View("Dashboard", await data.SnapshotAsync(page, cancellationToken));
}
