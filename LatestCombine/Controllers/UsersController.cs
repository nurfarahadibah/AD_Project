// File: Controllers/UsersController.cs
// This controller manages user accounts, including listing, creating, editing, and deleting users.
// Now includes filtering by role, searching by username, and filtering/displaying by Tenant.
// ADDED: Functionality for creating new Tenants (without initial admin user).
// UPDATED: Added IsActive status toggling and filtering for users.

using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectListItem
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data; // Required for AppDbContext
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  // Authorize only users with the "SuperAdmin" role to access this controller.
  [Authorize(Roles = "SuperAdmin")]
  public class UsersController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context; // Inject AppDbContext

    // Constructor for dependency injection
    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
    {
      _userManager = userManager;
      _roleManager = roleManager;
      _context = context; // Initialize AppDbContext
    }

    // GET: Users
    // Displays a list of all users along with their assigned roles and tenant.
    // Now accepts optional roleFilter, searchQuery, tenantFilter, and statusFilter parameters for filtering.
    public async Task<IActionResult> Index(
        string roleFilter,
        string searchQuery,
        string tenantFilter,
        string statusFilter = "all") // NEW: statusFilter parameter, defaults to "all"
    {
      // Retrieve all users from the database.
      IQueryable<ApplicationUser> usersQuery = _userManager.Users;

      // Apply Status Filter (NEW)
      if (statusFilter == "active")
      {
        usersQuery = usersQuery.Where(u => u.IsActive);
      }
      else if (statusFilter == "inactive")
      {
        usersQuery = usersQuery.Where(u => !u.IsActive);
      }
      // If statusFilter is "all" or anything else, no status filter is applied, showing all users.


      // Apply search filter if a searchQuery is provided
      if (!string.IsNullOrWhiteSpace(searchQuery))
      {
        usersQuery = usersQuery.Where(u => u.UserName != null && u.UserName.Contains(searchQuery));
      }

      // Apply tenant filter if a tenantFilter is provided (and is not null/empty)
      if (!string.IsNullOrEmpty(tenantFilter))
      {
        usersQuery = usersQuery.Where(u => u.TenantId == tenantFilter);
      }

      var users = await usersQuery.ToListAsync();
      var userViewModels = new List<UserViewModels>();

      // Prepare a list of all roles for the filter dropdown
      var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
      var rolesForDropdown = new List<SelectListItem> { new SelectListItem { Text = "All Roles", Value = "" } };
      rolesForDropdown.AddRange(allRoles.Select(r => new SelectListItem { Text = r, Value = r, Selected = (r == roleFilter) }));

      // Prepare a list of all tenants for the filter dropdown
      var allTenants = await _context.Tenants
                                    .OrderBy(t => t.Name)
                                    .Select(t => new SelectListItem { Value = t.Id, Text = t.Name })
                                    .ToListAsync();
      var tenantsForDropdown = new List<SelectListItem> { new SelectListItem { Text = "All Tenants", Value = "", Selected = (string.IsNullOrEmpty(tenantFilter)) } };
      tenantsForDropdown.AddRange(allTenants.Select(t => new SelectListItem { Text = t.Text, Value = t.Value, Selected = (t.Value == tenantFilter) }));


      // Iterate through each user to get their roles and tenant info
      foreach (var user in users)
      {
        var roles = await _userManager.GetRolesAsync(user);

        // Fetch tenant name (TenantId is string)
        var tenantName = "N/A";
        if (!string.IsNullOrEmpty(user.TenantId))
        {
          var tenant = await _context.Tenants.FindAsync(user.TenantId);
          tenantName = tenant?.Name ?? "Unknown Tenant";
        }

        // Apply role filter: only add user if they have the selected role (or if no roleFilter is applied)
        if (string.IsNullOrWhiteSpace(roleFilter) || roles.Contains(roleFilter))
        {
          userViewModels.Add(new UserViewModels
          {
            Id = user.Id,
            UserName = user.UserName ?? "N/A",
            Email = user.Email ?? "N/A",
            Roles = string.Join(", ", roles),
            TenantId = user.TenantId,
            TenantName = tenantName,
            IsActive = user.IsActive // Map IsActive property
          });
        }
      }

      // Sort users by username for consistent display
      userViewModels = userViewModels.OrderBy(u => u.UserName).ToList();

      // Pass the list of roles and current filter/search values to the view via ViewBag
      ViewBag.RolesForFilter = rolesForDropdown;
      ViewBag.CurrentRoleFilter = roleFilter;
      ViewBag.CurrentSearchQuery = searchQuery;
      ViewBag.TenantsForFilter = tenantsForDropdown;
      ViewBag.CurrentTenantFilter = tenantFilter;
      ViewBag.CurrentStatusFilter = statusFilter; // NEW: Pass current status filter

      // Pass any success/error messages from TempData
      if (TempData["SuccessMessage"] != null)
      {
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
      }
      if (TempData["ErrorMessage"] != null)
      {
        ViewBag.ErrorMessage = TempData["ErrorMessage"];
      }

      return View("~/Views/Users/Index.cshtml", userViewModels);
    }

    // NEW: Action to toggle user status
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Consider adding authorization, e.g., [Authorize(Roles = "SuperAdmin, Admin")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        TempData["ErrorMessage"] = "User ID is required.";
        return RedirectToAction(nameof(Index));
      }

      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        TempData["ErrorMessage"] = "User not found.";
        return RedirectToAction(nameof(Index));
      }

      // Optional safety: Prevent a user from deactivating their own account
      // Make sure to import System.Security.Claims if using ClaimTypes
      // if (User.Identity.IsAuthenticated && User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id)
      // {
      //     TempData["ErrorMessage"] = "You cannot change your own status.";
      //     return RedirectToAction(nameof(Index));
      // }

      user.IsActive = !user.IsActive; // Toggle the status
      var result = await _userManager.UpdateAsync(user);

      if (result.Succeeded)
      {
        TempData["SuccessMessage"] = $"User '{user.UserName}' status changed to {(user.IsActive ? "Active" : "Inactive")}.";
      }
      else
      {
        TempData["ErrorMessage"] = "Failed to update user status: " + string.Join(", ", result.Errors.Select(e => e.Description));
      }

      return RedirectToAction(nameof(Index));
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
      if (ModelState.IsValid)
      {
        var user = new ApplicationUser { UserName = model.UserName, Email = model.Email, IsActive = true }; // Default new user to active
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
          TempData["SuccessMessage"] = $"User '{user.UserName}' created successfully!";
          return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }
      return View("~/Views/Users/Create.cshtml", model);
    }

    // GET: Users/Edit/5
    // Displays the form for editing an existing user.
    public async Task<IActionResult> Edit(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        return NotFound();
      }

      var userRoles = await _userManager.GetRolesAsync(user);

      // Fetch all available tenants for the dropdown (Tenant.Id is string)
      var availableTenants = await _context.Tenants
                                           .OrderBy(t => t.Name)
                                           .Select(t => new SelectListItem
                                           {
                                             Value = t.Id, // Tenant.Id is string
                                             Text = t.Name
                                           }).ToListAsync();
      // Optional: Add a default "Select Tenant" option if TenantId can be null/empty
      availableTenants.Insert(0, new SelectListItem { Text = "-- Select Tenant --", Value = "" });


      // Populate the ViewModel for editing. (TenantId is string)
      var model = new UserEditViewModel
      {
        Id = user.Id,
        UserName = user.UserName ?? "N/A",
        Email = user.Email ?? "N/A",
        CurrentRoles = userRoles.ToList(),
        TenantId = user.TenantId ?? "", // Populate current TenantId for the user (now string)
        AvailableTenants = availableTenants, // Populate dropdown options
        IsActive = user.IsActive // Pass IsActive to the edit model
      };

      return View("~/Views/Users/Edit.cshtml", model);
    }

    // POST: Users/Edit/5
    // Handles the submission of the user edit form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
      // IMPORTANT: If you allow editing IsActive directly on the form,
      // ensure model.IsActive is mapped correctly here.
      // For now, it's assumed IsActive is only changed via ToggleUserStatus.

      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
          return NotFound();
        }

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.TenantId = model.TenantId; // Save the selected TenantId (now string)
                                        // user.IsActive = model.IsActive; // Uncomment if IsActive is editable on the form

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
          TempData["SuccessMessage"] = $"User '{user.UserName}' updated successfully!";
          return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }

      // If ModelState is not valid, re-populate AvailableTenants before returning the view
      model.AvailableTenants = await _context.Tenants
                                               .OrderBy(t => t.Name)
                                               .Select(t => new SelectListItem
                                               {
                                                 Value = t.Id, // Tenant.Id is string
                                                 Text = t.Name
                                               }).ToListAsync();
      model.AvailableTenants.Insert(0, new SelectListItem { Text = "-- Select Tenant --", Value = "" });


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

      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        return NotFound();
      }

      var userRoles = await _userManager.GetRolesAsync(user);

      // Fetch tenant name for display (TenantId is string)
      var tenantName = "N/A";
      if (!string.IsNullOrEmpty(user.TenantId))
      {
        var tenant = await _context.Tenants.FindAsync(user.TenantId);
        tenantName = tenant?.Name ?? "Unknown Tenant";
      }

      var model = new UserViewModels
      {
        Id = user.Id,
        UserName = user.UserName ?? "N/A",
        Email = user.Email ?? "N/A",
        Roles = string.Join(", ", userRoles),
        TenantId = user.TenantId,
        TenantName = tenantName,
        IsActive = user.IsActive // Pass IsActive to the delete model
      };

      return View("~/Views/Users/Delete.cshtml", model);
    }

    // POST: Users/Delete/5
    // Handles the actual deletion of the user.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
      var user = await _userManager.FindByIdAsync(id);
      if (user == null)
      {
        TempData["ErrorMessage"] = "User not found or already deleted.";
        return RedirectToAction(nameof(Index));
      }

      // Optional safety: Prevent a user from deleting their own account
      if (User.Identity.IsAuthenticated && User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id)
      {
        TempData["ErrorMessage"] = "You cannot delete your own account.";
        return RedirectToAction(nameof(Index));
      }

      var result = await _userManager.DeleteAsync(user);

      if (result.Succeeded)
      {
        TempData["SuccessMessage"] = $"User '{user.UserName}' deleted successfully!";
        return RedirectToAction(nameof(Index));
      }

      foreach (var error in result.Errors)
      {
        ModelState.AddModelError(string.Empty, error.Description);
      }
      var userRoles = await _userManager.GetRolesAsync(user);

      var tenantName = "N/A";
      if (!string.IsNullOrEmpty(user.TenantId))
      {
        var tenant = await _context.Tenants.FindAsync(user.TenantId);
        tenantName = tenant?.Name ?? "Unknown Tenant";
      }

      var model = new UserViewModels
      {
        Id = user.Id,
        UserName = user.UserName ?? "N/A",
        Email = user.Email ?? "N/A",
        Roles = string.Join(", ", userRoles),
        TenantId = user.TenantId,
        TenantName = tenantName,
        IsActive = user.IsActive
      };
      return View("~/Views/Users/Delete.cshtml", model);
    }

    // --- TENANT CREATION FUNCTIONALITY ---

    // GET: Users/CreateTenant
    // Displays the form for creating a new tenant.
    public IActionResult CreateTenant()
    {
      return View("~/Views/Users/CreateTenant.cshtml");
    }

    // POST: Users/CreateTenant
    // Handles the submission of the new tenant creation form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(TenantCreateViewModel model)
    {
      if (ModelState.IsValid)
      {
        var newTenant = new Tenant
        {
          Id = Guid.NewGuid().ToString(),
          Name = model.Name,
          Description = model.Description,
          CreatedDate = DateTime.Now,
          IsActive = true // Default new tenant to active
        };

        try
        {
          _context.Tenants.Add(newTenant);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = $"Tenant '{newTenant.Name}' created successfully with ID: {newTenant.Id}!";
          return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
          ModelState.AddModelError(string.Empty, "An error occurred while saving the tenant. Please try again. " + ex.Message);
        }
        catch (Exception ex)
        {
          ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
        }
      }
      return View("~/Views/Users/CreateTenant.cshtml", model);
    }
  }
}
