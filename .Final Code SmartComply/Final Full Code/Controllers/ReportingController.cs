// Controllers/ReportingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic; // Add this for List<T>

namespace AspnetCoreMvcFull.Controllers
{
  // You might apply a general authorization policy here, or per action.
  [Authorize] // Ensure only authenticated users can access
  public class ReportingController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReportingService _reportingService;

    public ReportingController(UserManager<ApplicationUser> userManager, IReportingService reportingService)
    {
      _userManager = userManager;
      _reportingService = reportingService;
    }

    public async Task<IActionResult> Index()
    {
      var user = await _userManager.GetUserAsync(User);
      if (user == null)
      {
        // Handle case where user is not found (shouldn't happen with [Authorize])
        return Forbid();
      }

      ReportingViewModel viewModel;

      if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
      {
        viewModel = await _reportingService.GetGlobalReportsAsync();
      }
      else
      {
        // For Admin, Manager, User roles, fetch tenant-specific data
        viewModel = await _reportingService.GetTenantReportsAsync(user);
      }

      return View(viewModel);
    }

    // Add action methods for exporting data
    // These will take the data from the view model and return a file.
    [HttpGet]
    public async Task<IActionResult> ExportDocumentsToCsv()
    {
      var user = await _userManager.GetUserAsync(User);
      ReportingViewModel viewModel;

      if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
      {
        viewModel = await _reportingService.GetGlobalReportsAsync();
      }
      else
      {
        viewModel = await _reportingService.GetTenantReportsAsync(user);
      }

      // --- Logic to convert viewModel.Documents to CSV and return file ---
      var csvContent = GenerateCsvForDocuments(viewModel.Documents);
      return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", "DocumentsReport.csv");
    }

    // Helper method to generate CSV (You'll implement this)
    private string GenerateCsvForDocuments(List<Document> documents)
    {
      var sb = new System.Text.StringBuilder();
      // Add CSV header
      sb.AppendLine("Id,Title,Category,Status,UploadDate"); // Example headers
      foreach (var doc in documents)
      {
        sb.AppendLine($"{doc.Id},{EscapeCsv(doc.FileName)},{EscapeCsv(doc.Description)},{EscapeCsv(doc.Status.ToString())},{doc.UploadDate.ToShortDateString()}"); // Example data
      }
      return sb.ToString();
    }

    // *** NEW METHOD FOR EXPORTING AUDITS TO CSV ***
    [HttpGet]
    public async Task<IActionResult> ExportAuditsToCsv()
    {
      var user = await _userManager.GetUserAsync(User);
      ReportingViewModel viewModel;

      if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
      {
        viewModel = await _reportingService.GetGlobalReportsAsync();
      }
      else
      {
        viewModel = await _reportingService.GetTenantReportsAsync(user);
      }

      var csvContent = GenerateCsvForAuditInstances(viewModel.AuditInstances);
      return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", "AuditInstancesReport.csv");
    }

    // Helper method to generate CSV for Audit Instances
    private string GenerateCsvForAuditInstances(List<AuditInstance> auditInstances)
    {
      var sb = new System.Text.StringBuilder();
      // Add CSV header
      sb.AppendLine("ID,Type,Status,Score,AuditDate");
      foreach (var audit in auditInstances)
      {
        // Ensure JenisForm?.Name is handled for null
        var jenisFormName = audit.JenisForm?.Name ?? "N/A";
        sb.AppendLine($"{audit.AuditInstanceId},{EscapeCsv(jenisFormName)},{EscapeCsv(audit.Status.ToString())},{audit.PercentageScore}%,{audit.AuditDate.ToShortDateString()}");
      }
      return sb.ToString();
    }


    private object EscapeCsv(object category) // This method seems problematic in your original code if it's meant to escape an object. It should really take a string.
    {
      // This needs to be implemented correctly. Assuming it's meant to call the string version.
      if (category == null) return "";
      return EscapeCsv(category.ToString());
    }

    private string EscapeCsv(string value)
    {
      if (string.IsNullOrEmpty(value)) return "";
      // Handle commas and quotes within values
      if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r")) // Added \r for robustness
      {
        return $"\"{value.Replace("\"", "\"\"")}\"";
      }
      return value;
    }

    // You'll add similar Export methods for CorrectiveActions etc.
    // e.g., ExportCorrectiveActionsToExcel()
  }
}
