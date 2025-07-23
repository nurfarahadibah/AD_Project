// Services/ReportingService.cs
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.EntityFrameworkCore; // Make sure this is included for .Include()
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

      // Documents for the current tenant
      // Documents are linked via ComplianceFolder, which should have TenantId
      viewModel.Documents = await _context.Documents
                                          .Include(d => d.ComplianceFolder) // Eagerly load ComplianceFolder
                                          .Where(d => d.ComplianceFolder != null && d.ComplianceFolder.TenantId == viewModel.CurrentTenantId)
                                          .ToListAsync();

      // Audit Instances for the current tenant
      viewModel.AuditInstances = await _context.AuditInstances
                                              .Where(a => a.TenantId == viewModel.CurrentTenantId)
                                              .Include(a => a.JenisForm) // Already added this from previous fix
                                              .ToListAsync();

      // Corrective Actions for the current tenant
      // CorrectiveActions are linked via AuditInstance, which has TenantId
      viewModel.CorrectiveActions = await _context.CorrectiveActions
                                                  .Include(ca => ca.AuditInstance) // Eagerly load AuditInstance
                                                  .Where(ca => ca.AuditInstance != null && ca.AuditInstance.TenantId == viewModel.CurrentTenantId)
                                                  .ToListAsync();

      // Add other tenant-specific data retrieval logic here based on user role if needed
      // e.g., if Manager needs to see different sets of data than a regular User.

      return viewModel;
    }

    public async Task<ReportingViewModel> GetGlobalReportsAsync()
    {
      var viewModel = new ReportingViewModel();
      viewModel.UserRole = "SuperAdmin"; // Explicitly set for SuperAdmin view

      // All queries here should use .IgnoreQueryFilters() to get global data
      // For global reports, you generally don't need tenant-specific filtering.
      // .Include() is still needed if you display related data in the view.

      // All Documents across all tenants
      // No TenantId filter needed for global view, but include ComplianceFolder if its properties are used in the view/reports.
      viewModel.Documents = await _context.Documents
                                          .IgnoreQueryFilters()
                                          .Include(d => d.ComplianceFolder) // Include if needed for other properties/reports
                                          .ToListAsync();

      // All Audit Instances across all tenants
      viewModel.AuditInstances = await _context.AuditInstances
                                              .IgnoreQueryFilters()
                                              .Include(a => a.JenisForm) // Already added this from previous fix
                                              .ToListAsync();

      // All Corrective Actions across all tenants
      // Include AuditInstance if its properties are used in the view/reports.
      viewModel.CorrectiveActions = await _context.CorrectiveActions
                                                  .IgnoreQueryFilters()
                                                  .Include(ca => ca.AuditInstance) // Include if needed for other properties/reports
                                                  .ToListAsync();

      // Add other global data retrieval logic here (e.g., all users, all tenants etc.)

      return viewModel;
    }
  }
}
