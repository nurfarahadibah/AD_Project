// Controllers/ReportingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;

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

    private object EscapeCsv(object category)
    {
      throw new NotImplementedException();
    }

    private string EscapeCsv(string value)
    {
      if (string.IsNullOrEmpty(value)) return "";
      // Handle commas and quotes within values
      if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
      {
        return $"\"{value.Replace("\"", "\"\"")}\"";
      }
      return value;
    }

    // You'll add similar Export methods for AuditInstances, CorrectiveActions etc.
    // e.g., ExportAuditsToPdf(), ExportCorrectiveActionsToExcel()
  }
}
