// Services/DashboardService.cs

using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Make sure this is included
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AspnetCoreMvcFull.Services
{
  public class DashboardService : IDashboardService
  {
    private readonly AppDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly UserManager<ApplicationUser> _userManager; // Ensure UserManager is injected

    public DashboardService(AppDbContext context, ITenantService tenantService, UserManager<ApplicationUser> userManager) // Add UserManager to constructor
    {
      _context = context;
      _tenantService = tenantService;
      _userManager = userManager; // Assign UserManager
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(ApplicationUser? user)
    {
      var viewModel = new DashboardViewModel();
      var currentTenantId = _tenantService.GetCurrentTenantId();

      // --- Auditor Performance (Specific to the LOGGED-IN User/Auditor) ---
      if (user != null)
      {
        viewModel.AuditorPerformance.CurrentAuditorName = user.UserName;

        var auditorSpecificAuditsQuery = _context.AuditInstances
                                                 .Where(a => a.AuditorName == user.UserName);

        viewModel.AuditorPerformance.TotalAuditsDone = await auditorSpecificAuditsQuery
                                                            .Where(a => a.Status == AuditStatus.Completed)
                                                            .CountAsync();

        viewModel.AuditorPerformance.OverdueAudits = await _context.CorrectiveActions
                                                            .Where(ca => ca.AuditInstance.AuditorName == user.UserName &&
                                                                         ca.Status != CorrectiveActionStatus.Completed &&
                                                                         ca.DueDate < DateTime.Today)
                                                            .CountAsync();

        //viewModel.AuditorPerformance.RejectedAudits = await auditorSpecificAuditsQuery
        //                                                    .Where(a => a.Status == AuditStatus.Rejected)
        //                                                    .CountAsync();
      }


      // --- Compliance and Documents Data (System-wide, filtered by current tenant) ---
      // Apply tenant filter to ComplianceFolders
      var complianceFoldersQuery = _context.ComplianceFolders.Where(f => f.TenantId == currentTenantId);

      viewModel.TotalComplianceFolders = await complianceFoldersQuery.CountAsync();
      viewModel.ActiveComplianceFolders = await complianceFoldersQuery.CountAsync(f => f.Status == FolderStatus.Active);
      viewModel.ArchivedComplianceFolders = await complianceFoldersQuery.CountAsync(f => f.Status == FolderStatus.Archived);

      // Apply tenant filter to Documents
      var documentsQuery = _context.Documents.Where(d => d.ComplianceFolder.TenantId == currentTenantId);
      viewModel.TotalDocuments = await documentsQuery.CountAsync();
      viewModel.PendingReviewDocuments = await documentsQuery.CountAsync(d => d.Status == DocumentStatus.PendingReview);
      viewModel.ApprovedDocuments = await documentsQuery.CountAsync(d => d.Status == DocumentStatus.Approved);

      // Apply tenant filter to RequiredDocuments
      var requiredDocumentsQuery = _context.RequiredDocuments.Where(rd => rd.ComplianceFolder.TenantId == currentTenantId);
      viewModel.TotalRequiredDocuments = await requiredDocumentsQuery.CountAsync();
      viewModel.SubmittedRequiredDocuments = await requiredDocumentsQuery.CountAsync(rd => rd.IsSubmitted);
      viewModel.OutstandingRequiredDocuments = await requiredDocumentsQuery.CountAsync(rd => !rd.IsSubmitted);

      viewModel.ComplianceCategoryCounts = await _context.ComplianceCategories
         .Select(cc => new { cc.Name, Count = cc.ComplianceFolders.Count(cf => cf.TenantId == currentTenantId) }) // Apply tenant filter here
         .ToDictionaryAsync(x => x.Name, x => x.Count);

      // --- Audit and Forms Data (System-wide) ---
      viewModel.TotalAuditInstances = await _context.AuditInstances.CountAsync();
      viewModel.DraftAudits = await _context.AuditInstances.CountAsync(a => a.Status == AuditStatus.Draft);
      viewModel.CompletedAudits = await _context.AuditInstances.CountAsync(a => a.Status == AuditStatus.Completed);
      viewModel.NeedsCorrectiveActionAudits = await _context.AuditInstances.CountAsync(a => a.Status == AuditStatus.NeedsCorrectiveAction);
      viewModel.NeedsFollowUpAudits = await _context.AuditInstances.CountAsync(a => a.Status == AuditStatus.NeedsFollowUp);

      viewModel.AverageAuditScore = await _context.AuditInstances
                          .Where(a => a.Status == AuditStatus.Completed)
                          .AverageAsync(a => (double?)a.PercentageScore) ?? 0.0;

      viewModel.TotalCorrectiveActions = await _context.CorrectiveActions.CountAsync();
      viewModel.PendingCorrectiveActions = await _context.CorrectiveActions.CountAsync(ca => ca.Status == CorrectiveActionStatus.Pending);
      viewModel.InProgressCorrectiveActions = await _context.CorrectiveActions.CountAsync(ca => ca.Status == CorrectiveActionStatus.InProgress);
      viewModel.CompletedCorrectiveActions = await _context.CorrectiveActions.CountAsync(ca => ca.Status == CorrectiveActionStatus.Completed);
      viewModel.OverdueCorrectiveActions = await _context.CorrectiveActions
                          .CountAsync(ca => ca.Status != CorrectiveActionStatus.Completed && ca.DueDate < DateTime.Today);

      viewModel.AuditsByFormType = await _context.AuditInstances
         .Include(ai => ai.JenisForm)
         .GroupBy(ai => ai.JenisForm.Name)
         .Select(g => new { FormTypeName = g.Key, Count = g.Count() })
         .ToDictionaryAsync(x => x.FormTypeName, x => x.Count);

      viewModel.CorrectiveActionsByStatus = await _context.CorrectiveActions
         .GroupBy(ca => ca.Status)
         .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
         .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

      // --- NEW: Non-Compliance Trends Data (System-wide) ---
      viewModel.NonComplianceByQuestion = await _context.CorrectiveActions
          .Include(ca => ca.AuditResponse)
          .ThenInclude(ar => ar.FormItem)
          .Where(ca => ca.AuditResponse != null && ca.AuditResponse.FormItem != null)
          .GroupBy(ca => ca.AuditResponse.FormItem.Question)
          .Select(g => new { Question = g.Key, Count = g.Count() })
          .OrderByDescending(x => x.Count)
          .Take(10)
          .ToDictionaryAsync(x => x.Question, x => x.Count);

      viewModel.NonComplianceByAuditType = await _context.CorrectiveActions
          .Include(ca => ca.AuditInstance)
          .ThenInclude(ai => ai.JenisForm)
          .Where(ca => ca.AuditInstance != null && ca.AuditInstance.JenisForm != null)
          .GroupBy(ca => ca.AuditInstance.JenisForm.Name)
          .Select(g => new { AuditType = g.Key, Count = g.Count() })
          .OrderByDescending(x => x.Count)
          .ToDictionaryAsync(x => x.AuditType, x => x.Count);


      // --- Form Templates Data (System-wide) ---
      viewModel.TotalFormTemplates = await _context.JenisForms.CountAsync();
      viewModel.PublishedFormTemplates = await _context.JenisForms.CountAsync(jf => jf.Status == FormStatus.Published);
      viewModel.DraftFormTemplates = await _context.JenisForms.CountAsync(jf => jf.Status == FormStatus.Draft);

      // --- Tenant Data (System-wide) ---
      viewModel.TotalTenants = await _context.Tenants.IgnoreQueryFilters().CountAsync();
      viewModel.ActiveTenants = await _context.Tenants.IgnoreQueryFilters().CountAsync(t => t.IsActive);

      // --- NEW: User Management Data ---
      viewModel.TotalUsers = await _userManager.Users.CountAsync();
      // Assuming 'ActiveUsers' means all users if no specific 'IsActive' property on ApplicationUser.
      // If you have a custom 'IsActive' property or logic (e.g., last login date), replace this.
      viewModel.ActiveUsers = viewModel.TotalUsers; // Placeholder: all users are considered active
                                                    // If ApplicationUser had a property like 'IsActive', you'd do:
      viewModel.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);


      return viewModel;
    }

    public async Task<DashboardViewModel> GetGlobalSystemStatsAsync()
    {
      var model = new DashboardViewModel();

      // Populate User counts globally
      model.TotalUsers = await _userManager.Users.CountAsync();
      model.ActiveUsers = await _userManager.Users
                              .Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow)
                              .CountAsync();

      // Populate Tenant counts globally
      model.TotalTenants = await _context.Tenants.IgnoreQueryFilters().CountAsync();
      model.ActiveTenants = await _context.Tenants.IgnoreQueryFilters().CountAsync(t => t.IsActive);

      // Populate Compliance, Documents, Audits, Corrective Actions globally
      model.TotalComplianceFolders = await _context.ComplianceFolders.IgnoreQueryFilters().CountAsync();
      model.TotalDocuments = await _context.Documents.IgnoreQueryFilters().CountAsync();
      model.TotalAuditInstances = await _context.AuditInstances.IgnoreQueryFilters().CountAsync();
      model.TotalCorrectiveActions = await _context.CorrectiveActions.IgnoreQueryFilters().CountAsync();

      model.PendingCorrectiveActions = await _context.CorrectiveActions.IgnoreQueryFilters()
                                                  .Where(ca => ca.Status == CorrectiveActionStatus.Pending)
                                                  .CountAsync();

      model.OverdueCorrectiveActions = await _context.CorrectiveActions.IgnoreQueryFilters()
                                                  .Where(ca => ca.DueDate < DateTime.Today && ca.Status != CorrectiveActionStatus.Completed)
                                                  .CountAsync();

      // Populate Form Templates globally
      model.TotalFormTemplates = await _context.JenisForms.IgnoreQueryFilters().CountAsync();
      model.PublishedFormTemplates = await _context.JenisForms.IgnoreQueryFilters().CountAsync(jf => jf.Status == FormStatus.Published);
      model.DraftFormTemplates = await _context.JenisForms.IgnoreQueryFilters().CountAsync(jf => jf.Status == FormStatus.Draft);

      return model;
    }
  }
}
