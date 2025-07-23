// Models/ViewModels/DashboardViewModel.cs

using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class DashboardViewModel // Singular name, comprehensive content
  {
    // Compliance and Documents
    public int TotalComplianceFolders { get; set; }
    public int ActiveComplianceFolders { get; set; }
    public int ArchivedComplianceFolders { get; set; }
    public int TotalDocuments { get; set; }
    public int PendingReviewDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int TotalRequiredDocuments { get; set; }
    public int SubmittedRequiredDocuments { get; set; }
    public int OutstandingRequiredDocuments { get; set; }
    public Dictionary<string, int>? ComplianceCategoryCounts { get; set; } // For chart

    // Audit and Forms
    public int TotalAuditInstances { get; set; }
    public int DraftAudits { get; set; }
    public int CompletedAudits { get; set; }
    public int NeedsCorrectiveActionAudits { get; set; }
    public int NeedsFollowUpAudits { get; set; }
    public double AverageAuditScore { get; set; }
    public int TotalCorrectiveActions { get; set; }
    public int PendingCorrectiveActions { get; set; }
    public int InProgressCorrectiveActions { get; set; }
    public int CompletedCorrectiveActions { get; set; }
    public int OverdueCorrectiveActions { get; set; }
    public Dictionary<string, int>? AuditsByFormType { get; set; } // For chart
    public Dictionary<string, int>? CorrectiveActionsByStatus { get; set; } // For chart

    // Form Templates
    public int TotalFormTemplates { get; set; }
    public int PublishedFormTemplates { get; set; }
    public int DraftFormTemplates { get; set; }

    // Reporting (NEW SECTION)
    public int TotalReportsGenerated { get; set; }
    public int ScheduledReports { get; set; }
    public int AdHocReports { get; set; }
    public Dictionary<string, int>? ReportsByType { get; set; } // For chart

    // Other potentially useful metrics (for super admin)
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }

    public List<UserViewModels> RecentUsers { get; set; } = new List<UserViewModels>();

    // --- NEW: Auditor Performance (Specific to a user/auditor) ---
    public AuditorPerformanceViewModel AuditorPerformance { get; set; } = new AuditorPerformanceViewModel();

    // --- NEW: Non-Compliance Trends Data ---
    public Dictionary<string, int> NonComplianceByQuestion { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> NonComplianceByAuditType { get; set; } = new Dictionary<string, int>();
  }

  // Keep your existing AuditorPerformanceViewModel here
  public class AuditorPerformanceViewModel
  {
    public int TotalAuditsDone { get; set; } // Completed audits by this auditor
    public int OverdueAudits { get; set; }   // Overdue corrective actions related to this auditor's audits
    public int RejectedAudits { get; set; }  // Audits by this auditor marked as Rejected
    public string CurrentAuditorName { get; set; } // Name of the auditor for these stats
  }

  // You can remove ComplianceSummaryViewModel from here if you prefer to use the main DashboardViewModel properties directly for it
  // Or keep it if you want to encapsulate those three specific values.
  // For now, I'll keep the direct properties on DashboardViewModel for ComplianceSummary for simplicity
  public class ComplianceSummaryViewModel
  {
    public double PercentageOfCompletedAudits { get; set; }
    public int PendingCorrectiveActions { get; set; }
  }
}
