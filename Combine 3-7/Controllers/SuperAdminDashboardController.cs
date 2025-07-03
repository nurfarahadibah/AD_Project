// Controllers/SuperAdminDashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models.ViewModels;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Models; // Ensure ApplicationUser is accessible
using AspnetCoreMvcFull.Services; // Ensure IDashboardService is accessible
using Microsoft.AspNetCore.Identity; // Ensure UserManager is accessible

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "SuperAdmin")]
  public class SuperAdminDashboardController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager; // Still useful for general user info if needed
    private readonly IDashboardService _dashboardService;

    public SuperAdminDashboardController(
        UserManager<ApplicationUser> userManager,
        IDashboardService dashboardService)
    {
      _userManager = userManager;
      _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
      // Fetch ALL global stats relevant to SuperAdmin from the service
      var viewModel = await _dashboardService.GetGlobalSystemStatsAsync();

      // Set ViewBag data for the welcome message
      ViewBag.Username = User.Identity.Name;
      ViewBag.Role = "SuperAdmin"; // Explicitly set for this dashboard

      return View(viewModel);
    }
  }
}
