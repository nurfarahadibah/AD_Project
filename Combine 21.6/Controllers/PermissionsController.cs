using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "SuperAdmin")]
  public class PermissionsController : Controller
  {
    // Updated static list of all possible permissions based on the provided Module Overview diagram.
    private static readonly List<string> AllPermissions = new List<string>
        {
            // General / System related (derived from the diagram's implications and common admin tasks)
            "Admin - Manage System Settings", // Implied from previous diagram's "System Settings"
            "Admin - Manage Users & Roles",   // Implied from previous diagram's "Manage User and Roles"
            "Admin - View User Activity Logs", // From "User activity logs"

            // From Compliance Framework Setup Module (Admin Only)
            "Admin - Manage Compliance Categories", // From "Add/edit compliance categories"
            "Admin - Manage Form Templates", // From "Create/Edit Form with customizable fields", "Add Forms"

            // From Audit Module (User & Manager)
            "User - Create Audit", // From "Create Audit based on Compliance"
            "Manager - Verify Audit", // From "Verification"
            "Manager - Add Corrective Action", // From "Add Corrective Action"
            "User & Manager - Manage Follow Up Audits", // From "Add/Edit Follow Up Audit" (User creates, Manager manages/reviews)

            // From Dashboard & Reporting Module
            "User & Manager - View Auditor Performance", // From "Auditor performance"
            "User & Manager - View Compliance Trends", // From "Heatmaps or bar charts"
            "User & Manager - Generate Exportable Reports", // From "Exportable reports"
            "User & Manager - View Compliance Summary", // From "Compliance summary"

            // From Filing & Document Repository Module
            "User - Upload Documents", // From "Upload scanned documents or certifications"
            "User & Manager - Search & Filter Documents", // From "Filter/search by compliance type, etc."
            "User & Manager - Organize Documents" // From "Folder/tag system for easy organization"
        };


    // GET: Permissions/Index
    // Displays a list of all defined permissions.
    public IActionResult Index()
    {
      var permissionsList = AllPermissions.Select(p =>
      {
        var viewModel = new PermissionListViewModel
        {
          Name = p,
          CreatedDate = DateTime.Now.AddDays(-new Random().Next(1, 365)) // Placeholder
        };

        // Assign roles based on the permission name and the provided diagram.
        switch (p)
        {
          // Admin-only permissions
          case "Admin - Manage System Settings":
          case "Admin - Manage Users & Roles":
          case "Admin - View User Activity Logs":
          case "Admin - Manage Compliance Categories":
          case "Admin - Manage Form Templates":
            viewModel.AssignedToRoles.Add("Admin");
            break;

          // User-only permissions
          case "User - Create Audit":
          case "User - Upload Documents":
            viewModel.AssignedToRoles.Add("User");
            break;

          // Manager-only permissions
          case "Manager - Verify Audit":
          case "Manager - Add Corrective Action":
            viewModel.AssignedToRoles.Add("Manager");
            break;

          // Permissions assigned to both User and Manager
          case "User & Manager - Manage Follow Up Audits":
          case "User & Manager - View Auditor Performance":
          case "User & Manager - View Compliance Trends":
          case "User & Manager - Generate Exportable Reports":
          case "User & Manager - View Compliance Summary":
          case "User & Manager - Search & Filter Documents":
          case "User & Manager - Organize Documents":
            viewModel.AssignedToRoles.Add("User");
            viewModel.AssignedToRoles.Add("Manager");
            break;
        }
        return viewModel;
      }).ToList();

      return View("~/Views/Permissions/Index.cshtml", permissionsList);
    }
  }
}
