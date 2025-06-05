using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
//public class DashboardsController : Controller
//{
//  public IActionResult Index() => View();
//}

public class DashboardsController : Controller
{
  private readonly UserManager<IdentityUser> _userManager;

  public DashboardsController(UserManager<IdentityUser> userManager)
  {
    _userManager = userManager;
  }

  public async Task<IActionResult> Index()
  {
    var user = await _userManager.GetUserAsync(User);
    if (user != null)
    {
      var roles = await _userManager.GetRolesAsync(user);
      HttpContext.Session.SetString("Username", user.UserName);
      HttpContext.Session.SetString("Role", roles.FirstOrDefault() ?? "No Role");

      ViewBag.Username = user.UserName;
      ViewBag.Role = roles;
    }

    return View();
  }
}
