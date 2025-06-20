// Services/ReportingService.cs
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AspnetCoreMvcFull.Services
{
  public class ReportingService : IReportingService
  {
    private readonly AppDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportingService(AppDbContext context, ITenantService tenantService, UserManager<ApplicationUser> userManager)
    {
      _context = context;
      _tenantService = tenantService;
      _userManager = userManager;
    }

    public async Task<ReportingViewModel> GetTenantReportsAsync(ApplicationUser user)
    {
      var viewModel = new ReportingViewModel();
      viewModel.UserRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault(); // Get the primary role
      viewModel.CurrentTenantId = _tenantService.GetCurrentTenantId();

      // All queries here should be filtered by viewModel.CurrentTenantId
      // Example: Documents for the current tenant
      //viewModel.Documents = await _context.Documents
      //                                .Where(d => d.TenantId == viewModel.CurrentTenantId)
      //                                .ToListAsync();

      // Example: Audit Instances for the current tenant
      viewModel.AuditInstances = await _context.AuditInstances
                                          .Where(a => a.TenantId == viewModel.CurrentTenantId)
                                          .ToListAsync();

      // Example: Corrective Actions for the current tenant
      //viewModel.CorrectiveActions = await _context.CorrectiveActions
      //                                        .Where(ca => ca.TenantId == viewModel.CurrentTenantId)
      //                                        .ToListAsync();

      // Add other tenant-specific data retrieval logic here based on user role if needed
      // e.g., if Manager needs to see different sets of data than a regular User.

      return viewModel;
    }

    public async Task<ReportingViewModel> GetGlobalReportsAsync()
    {
      var viewModel = new ReportingViewModel();
      viewModel.UserRole = "SuperAdmin"; // Explicitly set for SuperAdmin view

      // All queries here should use .IgnoreQueryFilters() to get global data
      // Example: All Documents across all tenants
      viewModel.Documents = await _context.Documents.IgnoreQueryFilters().ToListAsync();

      // Example: All Audit Instances across all tenants
      viewModel.AuditInstances = await _context.AuditInstances.IgnoreQueryFilters().ToListAsync();

      // Example: All Corrective Actions across all tenants
      viewModel.CorrectiveActions = await _context.CorrectiveActions.IgnoreQueryFilters().ToListAsync();

      // Add other global data retrieval logic here (e.g., all users, all tenants etc.)

      return viewModel;
    }
  }
}
