using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers
{
  public class UserRoleAssignmentController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserRoleAssignmentController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
      _userManager = userManager;
      _roleManager = roleManager;
    }

    // GET: UserRoleAssignment/Index
    // Displays a list of all users and their assigned roles (this was the original RoleManagement/Index)
    // This page is now mainly for viewing and linking to ManageRoles for a specific user.
    // The main "Total users with their roles" table is now part of Roles/Index.cshtml
    // This action can be removed if you only link to ManageRoles from Roles/Index.
    public async Task<IActionResult> Index()
    {
      var users = await _userManager.Users.ToListAsync();
      var userRolesViewModels = new List<UserRolesViewModel>();

      foreach (var user in users)
      {
        var roles = await _userManager.GetRolesAsync(user);
        userRolesViewModels.Add(new UserRolesViewModel
        {
          UserId = user.Id,
          UserName = user.UserName,
          Email = user.Email,
          Roles = roles.ToList()
        });
      }

      // You might want to remove this Index view if you only manage roles from the Roles/Index page.
      // For now, keeping it for compatibility if you have other links to it.
      return View("~/Views/UserRoleAssignment/Index.cshtml", userRolesViewModels);
    }


    // GET: UserRoleAssignment/ManageRoles/userId
    // Displays the form to manage roles for a specific user
    public async Task<IActionResult> ManageRoles(string userId)
    {
      if (userId == null)
      {
        return NotFound();
      }

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null)
      {
        return NotFound();
      }

      var allRoles = await _roleManager.Roles.ToListAsync();
      var userRoles = await _userManager.GetRolesAsync(user);

      var model = new ManageUserRolesViewModel
      {
        UserId = user.Id,
        UserName = user.UserName
      };

      foreach (var role in allRoles)
      {
        model.RoleSelections.Add(new RoleSelectionViewModel
        {
          RoleId = role.Id,
          RoleName = role.Name,
          IsSelected = userRoles.Contains(role.Name)
        });
      }

      return View("~/Views/UserRoleAssignment/ManageRoles.cshtml", model);
    }

    // POST: UserRoleAssignment/ManageRoles
    // Handles the form submission to update a user's roles
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
    {
      var user = await _userManager.FindByIdAsync(model.UserId);
      if (user == null)
      {
        return NotFound();
      }

      var userRoles = await _userManager.GetRolesAsync(user);

      // Loop through the submitted role selections
      foreach (var roleSelection in model.RoleSelections)
      {
        if (roleSelection.IsSelected && !userRoles.Contains(roleSelection.RoleName))
        {
          // Role selected but not currently assigned, add it
          await _userManager.AddToRoleAsync(user, roleSelection.RoleName);
        }
        else if (!roleSelection.IsSelected && userRoles.Contains(roleSelection.RoleName))
        {
          // Role not selected but currently assigned, remove it
          await _userManager.RemoveFromRoleAsync(user, roleSelection.RoleName);
        }
      }

      //TempData["SuccessMessage"] = $"Roles for user '{user.UserName}' updated successfully!";
      return RedirectToAction(nameof(Index), "Roles"); // Redirect to the main Roles list page
    }
  }
}
