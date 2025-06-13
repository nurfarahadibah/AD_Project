namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class DashboardViewModels
  {
    public AuditorPerformanceViewModel AuditorPerformance { get; set; } = new AuditorPerformanceViewModel();
    public ComplianceSummaryViewModel ComplianceSummary { get; set; } = new ComplianceSummaryViewModel();
    // You might add more specific view models here for chart data,
    // e.g., public List<NonComplianceTrendData> NonComplianceTrends { get; set; }
    // For simplicity, we'll include basic chart data directly in the controller for now.
  }

  public class AuditorPerformanceViewModel
  {
    public int TotalAuditsDone { get; set; }
    public int OverdueAudits { get; set; }
    public int RejectedAudits { get; set; }
    public string CurrentAuditorName { get; set; } // To display the logged-in auditor's name
  }

  public class ComplianceSummaryViewModel
  {
    public double PercentageOfCompletedAudits { get; set; }
    public int PendingCorrectiveActions { get; set; }
  }

  // You could also define a ViewModel for specific chart data if you have complex data structures
  // For example:
  // public class NonComplianceTrendData
  // {
  //     public string Category { get; set; }
  //     public int Count { get; set; }
  // }
}
