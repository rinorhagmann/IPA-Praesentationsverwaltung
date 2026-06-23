using System.Security.Claims;
using IPA_Praesentationsverwaltung.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace IPA_Praesentationsverwaltung.Tests.TestSupport;

/// <summary>Wires MVC controllers with a fake authenticated principal and TempData for unit tests.</summary>
public static class MvcTestHarness
{
    public static ClaimsPrincipal Admin(int id = 1, string name = "Admin User") =>
        Principal(id, RoleNames.Admin, name);

    public static ClaimsPrincipal Student(int id = 1, string name = "Anna Muster") =>
        Principal(id, RoleNames.Student, name);

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public static ClaimsPrincipal Principal(int id, string role, string name)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Attaches an <see cref="HttpContext"/>, principal and TempData to the controller.</summary>
    public static TController WithContext<TController>(
        this TController controller,
        ClaimsPrincipal? user = null,
        IServiceProvider? requestServices = null)
        where TController : Controller
    {
        var httpContext = new DefaultHttpContext { User = user ?? Anonymous() };
        if (requestServices is not null)
        {
            httpContext.RequestServices = requestServices;
        }

        // A complete ActionContext (with RouteData + ActionDescriptor) keeps the
        // controller's Url helper constructible, e.g. for RedirectToAction.
        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
        controller.ControllerContext = new ControllerContext(actionContext);
        controller.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        return controller;
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}
