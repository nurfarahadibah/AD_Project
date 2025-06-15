using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Data;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services; // Add this line for the service interface

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
public class DashboardsController : Controller
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IDashboardService _dashboardService; // Declare the service interface

  public DashboardsController(UserManager<ApplicationUser> userManager, IDashboardService dashboardService) // Inject the service
  {
    _userManager = userManager;
    _dashboardService = dashboardService; // Assign the injected service
  }

  public async Task<IActionResult> Index()
  {
    var user = await _userManager.GetUserAsync(User);

    // Use the service to get dashboard data
    var dashboardViewModel = await _dashboardService.GetDashboardDataAsync(user);

    if (user != null)
    {
      var roles = await _userManager.GetRolesAsync(user);
      HttpContext.Session.SetString("Username", user.UserName);
      HttpContext.Session.SetString("Role", roles.FirstOrDefault() ?? "No Role");

      ViewBag.Username = user.UserName;
      ViewBag.Role = roles;
    }

    return View(dashboardViewModel); // Pass the populated ViewModel to the view
  }
}
