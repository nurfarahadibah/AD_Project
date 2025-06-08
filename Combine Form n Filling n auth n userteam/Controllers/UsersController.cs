// File: Controllers/UsersController.cs
// This controller manages user accounts, including listing, creating, editing, and deleting users.
// Now includes filtering by role and searching by username.

using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectListItem
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers
{
  // Authorize only users with the "SuperAdmin" role to access this controller.
  [Authorize(Roles = "SuperAdmin")]
  public class UsersController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    // Constructor for dependency injection
    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
      _userManager = userManager;
      _roleManager = roleManager;
    }

    // GET: Users
    // Displays a list of all users along with their assigned roles.
    // Now accepts optional roleFilter and searchQuery parameters for filtering.
    public async Task<IActionResult> Index(string roleFilter, string searchQuery)
    {
      // Retrieve all users from the database.
      IQueryable<ApplicationUser> usersQuery = _userManager.Users;

      // Apply search filter if a searchQuery is provided
      if (!string.IsNullOrWhiteSpace(searchQuery))
      {
        usersQuery = usersQuery.Where(u => u.UserName != null && u.UserName.Contains(searchQuery));
      }

      var users = await usersQuery.ToListAsync();
      var userViewModels = new List<UserViewModels>();

      // Prepare a list of all roles for the filter dropdown
      var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
      // Add an "All Roles" option to the beginning of the list
      var rolesForDropdown = new List<SelectListItem> { new SelectListItem { Text = "All Roles", Value = "" } };
      rolesForDropdown.AddRange(allRoles.Select(r => new SelectListItem { Text = r, Value = r, Selected = (r == roleFilter) }));

      // Iterate through each user to get their roles and apply role filter
      foreach (var user in users)
      {
        var roles = await _userManager.GetRolesAsync(user);

        // Apply role filter: only add user if they have the selected role (or if no roleFilter is applied)
        if (string.IsNullOrWhiteSpace(roleFilter) || roles.Contains(roleFilter))
        {
          userViewModels.Add(new UserViewModels
          {
            Id = user.Id,
            UserName = user.UserName ?? "N/A", // Handle null UserName
            Email = user.Email ?? "N/A",       // Handle null Email
            Roles = string.Join(", ", roles)   // Join roles into a comma-separated string
          });
        }
      }

      // Sort users by username for consistent display
      userViewModels = userViewModels.OrderBy(u => u.UserName).ToList();

      // Pass the list of roles and current filter/search values to the view via ViewBag
      ViewBag.RolesForFilter = rolesForDropdown;
      ViewBag.CurrentRoleFilter = roleFilter;
      ViewBag.CurrentSearchQuery = searchQuery;

      // Explicitly specify the view path if it's not in the default location (Views/Users/Index.cshtml)
      // If your view is at Views/Users/Index.cshtml, you can simply return View(userViewModels);
      return View("~/Views/Users/Index.cshtml", userViewModels);
    }

    // GET: Users/Create
    // Displays the form for creating a new user.
    public IActionResult Create()
    {
      return View("~/Views/Users/Create.cshtml");
    }

    // POST: Users/Create
    // Handles the submission of the new user creation form.
    [HttpPost]
    [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery attacks.
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
      if (ModelState.IsValid)
      {
        // Create a new ApplicationUser instance.
        var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
        // Attempt to create the user with the provided password.
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
          // Optionally, assign a default role to the new user here, e.g., "User"
          // var defaultRole = "User";
          // if (!await _roleManager.RoleExistsAsync(defaultRole))
          // {
          //     await _roleManager.CreateAsync(new IdentityRole(defaultRole));
          // }
          // await _userManager.AddToRoleAsync(user, defaultRole);

          // Redirect to the user list after successful creation.
          return RedirectToAction(nameof(Index));
        }

        // If creation failed, add errors to ModelState to display in the view.
        foreach (var error in result.Errors)
        {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }
      // If ModelState is not valid, return the view with validation errors.
      return View("~/Views/Users/Create.cshtml", model);
    }

    // GET: Users/Edit/5
    // Displays the form for editing an existing user.
    public async Task<IActionResult> Edit(string id)
    {
      if (id == null)
      {
        return NotFound(); // Return 404 if ID is not provided.
      }

      // Find the user by ID.
      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        return NotFound(); // Return 404 if user is not found.
      }

      // Get the roles currently assigned to the user.
      var userRoles = await _userManager.GetRolesAsync(user);

      // Populate the ViewModel for editing.
      var model = new UserEditViewModel
      {
        Id = user.Id,
        UserName = user.UserName ?? "N/A",
        Email = user.Email ?? "N/A",
        CurrentRoles = userRoles.ToList()
      };

      return View("~/Views/Users/Edit.cshtml", model);
    }

    // POST: Users/Edit/5
    // Handles the submission of the user edit form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
      if (ModelState.IsValid)
      {
        // Find the user by ID.
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
          return NotFound();
        }

        // Update user properties.
        user.UserName = model.UserName;
        user.Email = model.Email;

        // Attempt to update the user.
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
          return RedirectToAction(nameof(Index));
        }

        // If update failed, add errors to ModelState.
        foreach (var error in result.Errors)
        {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }
      return View("~/Views/Users/Edit.cshtml", model);
    }

    // GET: Users/Delete/5
    // Displays a confirmation page before deleting a user.
    public async Task<IActionResult> Delete(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      // Find the user by ID.
      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        return NotFound();
      }

      // Get the roles currently assigned to the user for display.
      var userRoles = await _userManager.GetRolesAsync(user);

      var model = new UserViewModels
      {
        Id = user.Id,
        UserName = user.UserName ?? "N/A",
        Email = user.Email ?? "N/A",
        Roles = string.Join(", ", userRoles)
      };

      return View("~/Views/Users/Delete.cshtml", model);
    }

    // POST: Users/Delete/5
    // Handles the actual deletion of the user.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
      // Find the user by ID.
      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        // User already deleted or not found, redirect to index.
        return RedirectToAction(nameof(Index));
      }

      // Attempt to delete the user.
      var result = await _userManager.DeleteAsync(user);

      if (result.Succeeded)
      {
        return RedirectToAction(nameof(Index));
      }

      // If deletion failed, add errors to ModelState and return to the delete confirmation page.
      foreach (var error in result.Errors)
      {
        ModelState.AddModelError(string.Empty, error.Description);
      }
      // Re-fetch user details to display errors on the delete confirmation page if needed.
      var userRoles = await _userManager.GetRolesAsync(user);
      var model = new UserViewModels
      {
        Id = user.Id,
        UserName = user.UserName ?? "N/A",
        Email = user.Email ?? "N/A",
        Roles = string.Join(", ", userRoles)
      };
      return View("~/Views/Users/Delete.cshtml", model);
    }
  }
}
