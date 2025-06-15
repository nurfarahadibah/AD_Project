using AspnetCoreMvcFull.Models; // For ApplicationUser
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq; // For .FirstOrDefault() if you want to use it for roles

namespace AspnetCoreMvcFull.Services
{
  public class DashboardService : IDashboardService
  {
    private readonly UserManager<ApplicationUser> _userManager;
    // In a real application, you would inject your DbContext or other data access services here
    // private readonly ApplicationDbContext _dbContext;

    public DashboardService(UserManager<ApplicationUser> userManager /*, ApplicationDbContext dbContext */)
    {
      _userManager = userManager;
      // _dbContext = dbContext;
    }

    public async Task<DashboardViewModels> GetDashboardDataAsync(ApplicationUser user)
    {
      var dashboardViewModel = new DashboardViewModels();

      if (user != null)
      {
        // Retrieve user roles (still needed here if dashboard content varies by role)
        var roles = await _userManager.GetRolesAsync(user);

        // Populate Auditor Performance Data (Dummy Data - replace with real data from DB)
        dashboardViewModel.AuditorPerformance.CurrentAuditorName = user.UserName;
        // Example: Fetch from a real data source
        // var audits = await _dbContext.Audits.Where(a => a.AuditorId == user.Id).ToListAsync();
        // dashboardViewModel.AuditorPerformance.TotalAuditsDone = audits.Count;
        // dashboardViewModel.AuditorPerformance.OverdueAudits = audits.Count(a => a.IsOverdue);
        // dashboardViewModel.AuditorPerformance.RejectedAudits = audits.Count(a => a.IsRejected);
        dashboardViewModel.AuditorPerformance.TotalAuditsDone = 125;
        dashboardViewModel.AuditorPerformance.OverdueAudits = 8;
        dashboardViewModel.AuditorPerformance.RejectedAudits = 3;

        // Populate Compliance Summary Data (Dummy Data - replace with real data from DB)
        // Example: Calculate from real data
        // var totalAudits = await _dbContext.Audits.CountAsync();
        // var completedAudits = await _dbContext.Audits.CountAsync(a => a.IsCompleted);
        // dashboardViewModel.ComplianceSummary.PercentageOfCompletedAudits = totalAudits > 0 ? (double)completedAudits / totalAudits * 100 : 0;
        // dashboardViewModel.ComplianceSummary.PendingCorrectiveActions = await _dbContext.CorrectiveActions.CountAsync(ca => ca.Status == "Pending");
        dashboardViewModel.ComplianceSummary.PercentageOfCompletedAudits = 85.5;
        dashboardViewModel.ComplianceSummary.PendingCorrectiveActions = 15;
      }

      return dashboardViewModel;
    }
  }
}
