// In DashboardsController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Data;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
public class DashboardsController : Controller
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IDashboardService _dashboardService;

  public DashboardsController(UserManager<ApplicationUser> userManager, IDashboardService dashboardService)
  {
    _userManager = userManager;
    _dashboardService = dashboardService;
  }

  public async Task<IActionResult> Index()
  {
    var user = await _userManager.GetUserAsync(User);

    var dashboardViewModel = await _dashboardService.GetDashboardDataAsync(user);

    if (user != null)
    {
      var roles = await _userManager.GetRolesAsync(user);
      HttpContext.Session.SetString("Username", user.UserName);

      // Corrected line: Get the first role or a default string
      HttpContext.Session.SetString("Role", roles.FirstOrDefault() ?? "No Role");

      ViewBag.Username = user.UserName;
      // Corrected line: Assign the first role or a default string to ViewBag.Role
      ViewBag.Role = roles.FirstOrDefault() ?? "No Role";
    }

    return View(dashboardViewModel);
  }
}
