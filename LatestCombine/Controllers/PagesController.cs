using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers;

public class PagesController : Controller
{
  public IActionResult AccountSettings() => View();
  public IActionResult AccountSettingsConnections() => View();
  public IActionResult AccountSettingsNotifications() => View();
  public IActionResult MiscError() => View();
  public IActionResult MiscUnderMaintenance() => View();

  public IActionResult UserList() => View();

  // New actions to redirect to the dedicated Roles and Permissions controllers
  public IActionResult Roles()
  {
    // Redirects to the Index action of the RolesController
    return RedirectToAction("Index", "Roles");
  }

  public IActionResult Permissions()
  {
    // Redirects to the Index action of the PermissionsController
    return RedirectToAction("Index", "Permissions");
  }
}


//using System.Diagnostics;
//using Microsoft.AspNetCore.Mvc;
//using AspnetCoreMvcFull.Models;

//namespace AspnetCoreMvcFull.Controllers;

//public class PagesController : Controller
//{
//  public IActionResult AccountSettings() => View();
//  public IActionResult AccountSettingsConnections() => View();
//  public IActionResult AccountSettingsNotifications() => View();
//  public IActionResult MiscError() => View();
//  public IActionResult MiscUnderMaintenance() => View();
//}
