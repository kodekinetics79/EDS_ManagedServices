using Evostel.ManagedServices.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Evostel.ManagedServices.Web.Controllers;

public sealed class HomeController(ManagedServicesStore store) : Controller
{
    [HttpGet("/")]
    public IActionResult Overview() => Dashboard("overview");

    [HttpGet("/services")]
    public IActionResult Services() => Dashboard("services");

    [HttpGet("/integrations")]
    public IActionResult Integrations() => Dashboard("integrations");

    [HttpGet("/incidents")]
    public IActionResult Incidents() => Dashboard("incidents");

    [HttpGet("/support")]
    public IActionResult Support() => Dashboard("support");

    [HttpGet("/reports")]
    public IActionResult Reports() => Dashboard("reports");

    [HttpGet("/compliance")]
    public IActionResult Compliance() => Dashboard("compliance");

    [HttpGet("/settings")]
    public IActionResult Settings() => Dashboard("settings");

    [HttpGet("/activity")]
    public IActionResult Activity() => Dashboard("activity");

    [HttpGet("/forbidden")]
    public IActionResult ForbiddenPage()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Dashboard("forbidden");
    }

    [HttpGet("/error")]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return Dashboard("error");
    }

    [HttpGet("/{*path}", Order = 999)]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return Dashboard("not-found");
    }

    private ViewResult Dashboard(string page) => View("Dashboard", store.Snapshot(page));
}
