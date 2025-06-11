using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Sockets;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "SuperAdmin")]
  public class RolesController : Controller
  {
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
      _roleManager = roleManager;
      _userManager = userManager;
    }

    // Static list of all possible permissions as per your Materio template's modal.
    // In a real application, these would typically come from a database.
    private static readonly List<string> AllPermissions = new List<string>
        {
            "Administrator Access",
            "User Management Read", "User Management Write", "User Management Create",
            "Content Management Read", "Content Management Write", "Content Management Create",
            "Disputes Management Read", "Disputes Management Write", "Disputes Management Create",
            "Database Management Read", "Database Management Write", "Database Management Create",
            "Financial Management Read", "Financial Management Write", "Financial Management Create",
            "Reporting Read", "Reporting Write", "Reporting Create",
            "API Control Read", "API Control Write", "API Control Create",
            "Repository Management Read", "Repository Management Write", "Repository Management Create",
            "Payroll Read", "Payroll Write", "Payroll Create"
        };


    // GET: Roles/Index
    // Displays role cards and a table of users with their roles.
    public async Task<IActionResult> Index(int showEntries = 10, string searchUser = "", string selectRole = "", int page = 1)
    {
      var viewModel = new RolesIndexViewModel(); // Create an instance of the composite ViewModel

      // Set current filter values in the ViewModel for view persistence
      viewModel.ShowEntries = showEntries;
      viewModel.SearchUser = searchUser;
      viewModel.SelectedRole = selectRole;
      viewModel.CurrentPage = page;

      // 1. Prepare data for Role Cards
      var roles = await _roleManager.Roles.ToListAsync();
      foreach (var role in roles)
      {
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
        viewModel.RoleCards.Add(new RoleCardViewModel
        {
          Id = role.Id,
          Name = role.Name,
          UserCount = usersInRole.Count
        });
      }

      // 2. Prepare data for "Total users with their roles" table
      var allUsersQuery = _userManager.Users.AsQueryable();

      // Apply search filter
      if (!string.IsNullOrWhiteSpace(searchUser))
      {
        allUsersQuery = allUsersQuery.Where(u =>
            u.UserName.Contains(searchUser) ||
            u.Email.Contains(searchUser));
      }

      var filteredUsersList = new List<ApplicationUser>();
      foreach (var user in await allUsersQuery.ToListAsync())
      {
        var userRoles = await _userManager.GetRolesAsync(user);
        // Apply role filter
        if (string.IsNullOrWhiteSpace(selectRole) || userRoles.Contains(selectRole))
        {
          filteredUsersList.Add(user);
        }
      }

      // Calculate total users after filtering but before pagination
      viewModel.TotalUsers = filteredUsersList.Count;

      // Apply pagination
      var skip = (page - 1) * showEntries;
      var paginatedUsers = filteredUsersList.OrderBy(u => u.UserName) // Always order for consistent pagination
                                            .Skip(skip)
                                            .Take(showEntries)
                                            .ToList();

      foreach (var user in paginatedUsers)
      {
        var userRoles = await _userManager.GetRolesAsync(user);
        viewModel.UserRolesList.Add(new UserRolesListViewModel
        {
          UserId = user.Id,
          UserName = user.UserName,
          Email = user.Email,
          Roles = userRoles.ToList()
        });
      }

      // 3. Prepare model for the Add/Edit Role Modal (for initial "Add New Role" load)
      viewModel.AddEditRoleForm.Permissions = AllPermissions.Select(p => new PermissionItemViewModel { Name = p, IsSelected = false }).ToList();

      // 4. Populate AvailableRoles for the dropdown filter
      viewModel.AvailableRoles.Add(new SelectListItem { Text = "Select Role", Value = "" }); // Default option
      foreach (var role in roles.OrderBy(r => r.Name)) // Order roles alphabetically
      {
        viewModel.AvailableRoles.Add(new SelectListItem { Text = role.Name, Value = role.Name });
      }

      return View("~/Views/Roles/Index.cshtml", viewModel); // Pass the composite ViewModel
    }

    // GET: Roles/GetRoleForEditModal/roleId
    // AJAX endpoint to fetch role details and permissions for the edit modal
    [HttpGet]
    public async Task<IActionResult> GetRoleForEditModal(string roleId)
    {
      if (string.IsNullOrEmpty(roleId))
      {
        return BadRequest("Role ID is required.");
      }

      var role = await _roleManager.FindByIdAsync(roleId);
      if (role == null)
      {
        return NotFound("Role not found.");
      }

      var rolePermissions = new List<string>();
      // For demo purposes, assume Admin role has all permissions.
      // In a real app, you'd fetch actual permissions associated with this role from a database.
      if (role.Name == "Admin")
      {
        rolePermissions.AddRange(AllPermissions);
      }
      // For other roles, you would load their specific permissions from your data store.

      var model = new AddEditRoleViewModel
      {
        Id = role.Id,
        Name = role.Name,
        Permissions = AllPermissions.Select(p => new PermissionItemViewModel
        {
          Name = p,
          IsSelected = rolePermissions.Contains(p) // Check if role has this permission
        }).ToList()
      };

      return Json(model); // Return as JSON for AJAX
    }


    // POST: Roles/AddEditRole
    // Handles adding a new role or editing an existing one
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEditRole(AddEditRoleViewModel model)
    {
      if (!ModelState.IsValid)
      {
        // If model state is invalid, return a BadRequest with errors
        // For AJAX forms, you'd typically return JSON errors.
        // For a full page post, you'd re-render the view with the model.
        // Since this is a modal, we'll redirect and show an error message.
        TempData["ErrorMessage"] = "Failed to save role: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return RedirectToAction(nameof(Index));
      }

      IdentityResult result;
      IdentityRole role;

      if (string.IsNullOrEmpty(model.Id)) // Add new role
      {
        role = new IdentityRole(model.Name);
        result = await _roleManager.CreateAsync(role);
      }
      else // Edit existing role
      {
        role = await _roleManager.FindByIdAsync(model.Id);
        if (role == null)
        {
          TempData["ErrorMessage"] = "Role not found for editing.";
          return RedirectToAction(nameof(Index));
        }
        role.Name = model.Name;
        result = await _roleManager.UpdateAsync(role);
      }

      if (result.Succeeded)
      {
        // Handle permissions for the role
        // In a real application, you'd save the selected permissions to a separate table
        // that links roles to permissions (e.g., RolePermissions table).
        // For this example, we'll just demonstrate the concept.

        // Example: If you had a custom permission service:
        // await _permissionService.UpdateRolePermissionsAsync(role.Id, model.Permissions.Where(p => p.IsSelected).Select(p => p.Name).ToList());

        TempData["SuccessMessage"] = $"Role '{model.Name}' saved successfully!";
        return RedirectToAction(nameof(Index));
      }

      foreach (var error in result.Errors)
      {
        ModelState.AddModelError(string.Empty, error.Description);
      }

      TempData["ErrorMessage"] = "Failed to save role: " + string.Join(", ", result.Errors.Select(e => e.Description));
      return RedirectToAction(nameof(Index));
    }

    // POST: Roles/DeleteRole/roleId
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
      if (string.IsNullOrEmpty(roleId))
      {
        return BadRequest("Role ID is required.");
      }

      var role = await _roleManager.FindByIdAsync(roleId);
      if (role == null)
      {
        return NotFound("Role not found.");
      }

      // Prevent deleting critical roles like "Admin" for safety
      if (role.Name == "Admin" || role.Name == "User") // Add other critical roles
      {
        TempData["ErrorMessage"] = $"Cannot delete critical role '{role.Name}'.";
        return RedirectToAction(nameof(Index));
      }

      // Check if there are users in this role
      var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
      if (usersInRole.Any())
      {
        TempData["ErrorMessage"] = $"Cannot delete role '{role.Name}' because it has {usersInRole.Count} assigned users. Please reassign users first.";
        return RedirectToAction(nameof(Index));
      }


      var result = await _roleManager.DeleteAsync(role);

      if (result.Succeeded)
      {
        TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully!";
      }
      else
      {
        TempData["ErrorMessage"] = "Failed to delete role: " + string.Join(", ", result.Errors.Select(e => e.Description));
      }

      return RedirectToAction(nameof(Index));
    }
  }
}
