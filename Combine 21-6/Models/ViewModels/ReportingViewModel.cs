// Models/ViewModels/ReportingViewModel.cs
using System.Collections.Generic;
using AspnetCoreMvcFull.Models; // Assuming your models like Document, AuditInstance, CorrectiveAction are here

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class ReportingViewModel
  {
    public string? UserRole { get; set; }
    public string? CurrentTenantId { get; set; }

    // Lists of items that can be exported
    public List<Document> Documents { get; set; } = new List<Document>();
    public List<AuditInstance> AuditInstances { get; set; } = new List<AuditInstance>();
    public List<CorrectiveAction> CorrectiveActions { get; set; } = new List<CorrectiveAction>();
    // Add other lists as needed, e.g., List<ComplianceFolder>, List<User> etc.

    // Properties for filtering or display (optional, but good for interactive reports)
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DocumentStatusFilter { get; set; }
    public string? AuditStatusFilter { get; set; }
    // ... any other filters
  }
}
