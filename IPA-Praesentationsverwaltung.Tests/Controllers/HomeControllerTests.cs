using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Models;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Index_redirects_anonymous_to_login()
    {
        HomeController sut = new HomeController().WithContext(MvcTestHarness.Anonymous());

        var redirect = Assert.IsType<RedirectToActionResult>(sut.Index());
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Auth", redirect.ControllerName);
    }

    [Fact]
    public void Index_redirects_admin_to_dashboard()
    {
        HomeController sut = new HomeController().WithContext(MvcTestHarness.Admin());

        var redirect = Assert.IsType<RedirectToActionResult>(sut.Index());
        Assert.Equal("Dashboard", redirect.ActionName);
        Assert.Equal("Admin", redirect.ControllerName);
    }

    [Fact]
    public void Index_redirects_student_to_presentations()
    {
        HomeController sut = new HomeController().WithContext(MvcTestHarness.Student());

        var redirect = Assert.IsType<RedirectToActionResult>(sut.Index());
        Assert.Equal("Presentations", redirect.ActionName);
        Assert.Equal("Student", redirect.ControllerName);
    }

    [Fact]
    public void Error_returns_view_with_request_id()
    {
        HomeController sut = new HomeController().WithContext(MvcTestHarness.Anonymous());

        var view = Assert.IsType<ViewResult>(sut.Error());
        Assert.IsType<ErrorViewModel>(view.Model);
    }
}
