// Controllers/DocumentRepositoryController.cs
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Services; // For ITenantService
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace AspnetCoreMvcFull.Controllers
{
  public class DocumentRepositoryController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantService _tenantService; // --- NEW: Inject ITenantService ---

    public DocumentRepositoryController(AppDbContext context, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, ITenantService tenantService) // --- NEW: Inject ITenantService ---
    {
      _context = context;
      _environment = environment;
      _httpContextAccessor = httpContextAccessor;
      _tenantService = tenantService; // --- NEW: Assign ITenantService ---
    }

    public async Task<IActionResult> Index(string searchQuery = "", string filter = "all", string statusFilter = "all")
    {
      // Global Query Filter in AppDbContext automatically handles TenantId filtering here.
      // No need for .Where(f => f.TenantId == _tenantService.GetCurrentTenantId())
      var query = _context.ComplianceFolders
          .Include(f => f.Documents)
          .Include(f => f.RequiredDocuments)
          .Include(f => f.ComplianceCategory)
          .AsQueryable();

      if (!string.IsNullOrEmpty(searchQuery))
      {
        query = query.Where(f => f.Name.Contains(searchQuery) ||
                                 f.Description.Contains(searchQuery) ||
                                 f.RequiredDocuments.Any(rd => rd.DocumentName.Contains(searchQuery) || rd.Description.Contains(searchQuery)) ||
                                 (f.ComplianceCategory != null && f.ComplianceCategory.Name.Contains(searchQuery)));
      }

      // ComplianceCategories will also be filtered by the global query filter.
      var complianceCategories = await _context.ComplianceCategories.OrderBy(c => c.Name).ToListAsync();
      ViewBag.ComplianceCategories = complianceCategories;

      if (filter != "all")
      {
        if (int.TryParse(filter, out int categoryId))
        {
          query = query.Where(f => f.ComplianceCategoryId == categoryId);
        }
        // else condition can be removed if not needed, or add specific logic
      }

      if (statusFilter != "all")
      {
        if (Enum.TryParse(typeof(FolderStatus), statusFilter, true, out var parsedStatus))
        {
          query = query.Where(f => f.Status == (FolderStatus)parsedStatus);
        }
      }

      var folders = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

      ViewBag.SearchQuery = searchQuery;
      ViewBag.Filter = filter;
      ViewBag.StatusFilter = statusFilter;
      ViewBag.Stats = await GetDashboardStats(); // This will also be tenant-filtered
                                                 // --- ADD THIS LINE ---
      ViewBag.CurrentTenantId = _tenantService.GetCurrentTenantId();
      return View(folders);
    }

    // --- NEW ACTION FOR BULK STATUS UPDATE ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateStatus(BulkUpdateStatusViewModel model)
    {
      if (model.FolderIds == null || !model.FolderIds.Any())
      {
        TempData["ErrorMessage"] = "No folders were selected for the bulk action.";
        return RedirectToAction(nameof(Index));
      }

      if (string.IsNullOrEmpty(model.NewStatus))
      {
        TempData["ErrorMessage"] = "No new status was selected for the bulk action.";
        return RedirectToAction(nameof(Index));
      }

      if (!Enum.TryParse(typeof(FolderStatus), model.NewStatus, true, out var parsedNewStatus))
      {
        TempData["ErrorMessage"] = $"Invalid status selected: {model.NewStatus}.";
        return RedirectToAction(nameof(Index));
      }

      // Global Query Filter ensures only current tenant's folders are fetched
      var foldersToUpdate = await _context.ComplianceFolders
                                           .Where(f => model.FolderIds.Contains(f.Id))
                                           .ToListAsync();

      if (!foldersToUpdate.Any())
      {
        TempData["ErrorMessage"] = "Selected folders could not be found or do not belong to your organization.";
        return RedirectToAction(nameof(Index));
      }

      string lastModifiedByUserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
      var newStatusEnum = (FolderStatus)parsedNewStatus;
      int updatedCount = 0;

      foreach (var folder in foldersToUpdate)
      {
        if (folder.Status != newStatusEnum)
        {
          folder.Status = newStatusEnum;
          folder.LastModifiedDate = DateTime.Now;
          folder.LastModifiedBy = lastModifiedByUserName;
          _context.ComplianceFolders.Update(folder);
          updatedCount++;
        }
      }

      if (updatedCount > 0)
      {
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Successfully updated status for {updatedCount} folder(s) to '{newStatusEnum}'.";
      }
      else
      {
        TempData["InfoMessage"] = "No folders required a status change (already at selected status).";
      }

      return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateFolder(int id)
    {
      // Global Query Filter ensures only current tenant's folder is found
      var folder = await _context.ComplianceFolders.FindAsync(id);

      if (folder == null)
      {
        TempData["ErrorMessage"] = "Folder not found.";
        return NotFound();
      }

      if (folder.Status == FolderStatus.Archived) // Assuming "Active" is the opposite of "Archived"
      {
        folder.Status = FolderStatus.Active; // Change to FolderStatus.Active if that's your target state
        folder.LastModifiedDate = DateTime.Now;
        folder.LastModifiedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        _context.Update(folder);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Folder '{folder.Name}' has been activated successfully."; // Correct message
      }
      else
      {
        TempData["ErrorMessage"] = $"Folder '{folder.Name}' cannot be activated from its current status of '{folder.Status}'.";
      }

      return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveFolder(int id)
    {
      // Global Query Filter ensures only current tenant's folder is found
      var folder = await _context.ComplianceFolders.FindAsync(id);

      if (folder == null)
      {
        TempData["ErrorMessage"] = "Folder not found.";
        return NotFound();
      }

      if (folder.Status == FolderStatus.Active || folder.Status == FolderStatus.Completed  )
      {
        folder.Status = FolderStatus.Archived;
        folder.LastModifiedDate = DateTime.Now;
        folder.LastModifiedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        _context.Update(folder);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Folder '{folder.Name}' has been archived successfully.";
      }
      else
      {
        TempData["ErrorMessage"] = $"Folder '{folder.Name}' cannot be archived from its current status of '{folder.Status}'.";
      }

      return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateFolder()
    {
      var viewModel = new CreateFolderViewModel();
      await PopulateComplianceCategories(viewModel); // Categories are tenant-filtered
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(CreateFolderViewModel model)
    {
      if (ModelState.IsValid)
      {
        string createdByUserName = "System";
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(_httpContextAccessor.HttpContext.User.Identity.Name))
        {
          createdByUserName = _httpContextAccessor.HttpContext.User.Identity.Name;
        }

        // --- NEW: Assign TenantId to the new folder ---
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
          TempData["ErrorMessage"] = "Cannot create folder: Current tenant not identified.";
          await PopulateComplianceCategories(model);
          return View(model);
        }

        var folder = new ComplianceFolder
        {
          Name = model.Name,
          ComplianceCategoryId = model.ComplianceCategoryId,
          Description = model.Description,
          CreatedBy = createdByUserName,
          TenantId = currentTenantId // --- Assign the current tenant's ID ---
        };

        _context.ComplianceFolders.Add(folder);
        await _context.SaveChangesAsync(); // Save folder first to get its ID

        foreach (var reqDoc in model.RequiredDocuments)
        {
          var requiredDocument = new RequiredDocument
          {
            ComplianceFolderId = folder.Id,
            DocumentName = reqDoc.DocumentName,
            Description = reqDoc.Description,
            IsRequired = reqDoc.IsRequired
            // RequiredDocument does not need TenantId directly, as it's linked to ComplianceFolder
          };
          _context.RequiredDocuments.Add(requiredDocument);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Compliance folder created successfully!";
        return RedirectToAction(nameof(Index));
      }

      await PopulateComplianceCategories(model);
      return View(model);
    }

    public async Task<IActionResult> FolderDetails(int id)
    {
      // Global Query Filter ensures only current tenant's folder is found
      var folder = await _context.ComplianceFolders
          .Include(f => f.Documents)
          .Include(f => f.RequiredDocuments)
              .ThenInclude(rd => rd.Documents)
          .Include(f => f.ComplianceCategory)
          .FirstOrDefaultAsync(f => f.Id == id);

      if (folder == null)
      {
        return NotFound();
      }

      return View(folder);
    }

    [HttpGet]
    public async Task<IActionResult> EditFolder(int id)
    {
      // Global Query Filter ensures only current tenant's folder is found
      var folder = await _context.ComplianceFolders
          .Include(f => f.RequiredDocuments)
          .FirstOrDefaultAsync(f => f.Id == id);

      if (folder == null)
      {
        return NotFound();
      }

      var viewModel = new CreateFolderViewModel
      {
        Id = folder.Id,
        Name = folder.Name,
        ComplianceCategoryId = folder.ComplianceCategoryId,
        Description = folder.Description,
        RequiredDocuments = folder.RequiredDocuments.Select(rd => new RequiredDocumentViewModel
        {
          Id = rd.Id,
          DocumentName = rd.DocumentName,
          Description = rd.Description,
          IsRequired = rd.IsRequired
        }).ToList()
      };

      await PopulateComplianceCategories(viewModel);
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFolder(CreateFolderViewModel model)
    {
      if (ModelState.IsValid)
      {
        // Global Query Filter ensures only current tenant's folder is found
        var folderToUpdate = await _context.ComplianceFolders
            .Include(f => f.RequiredDocuments)
            .FirstOrDefaultAsync(f => f.Id == model.Id);

        if (folderToUpdate == null)
        {
          TempData["ErrorMessage"] = "Folder not found or does not belong to your organization.";
          return NotFound();
        }

        string lastModifiedByUserName = "System";
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(_httpContextAccessor.HttpContext.User.Identity.Name))
        {
          lastModifiedByUserName = _httpContextAccessor.HttpContext.User.Identity.Name;
        }

        folderToUpdate.Name = model.Name;
        folderToUpdate.ComplianceCategoryId = model.ComplianceCategoryId;
        folderToUpdate.Description = model.Description;
        folderToUpdate.LastModifiedDate = DateTime.Now;
        folderToUpdate.LastModifiedBy = lastModifiedByUserName;

        // When re-saving RequiredDocuments, you're replacing them, so existing ones
        // implicitly inherit TenantId from the folder.

        var existingRequiredDocs = folderToUpdate.RequiredDocuments.ToList();
        _context.RequiredDocuments.RemoveRange(existingRequiredDocs);

        foreach (var reqDocVm in model.RequiredDocuments)
        {
          var requiredDocument = new RequiredDocument
          {
            ComplianceFolderId = folderToUpdate.Id,
            DocumentName = reqDocVm.DocumentName,
            Description = reqDocVm.Description,
            IsRequired = reqDocVm.IsRequired
          };
          _context.RequiredDocuments.Add(requiredDocument);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Compliance folder updated successfully!";
        return RedirectToAction(nameof(Index));
      }

      await PopulateComplianceCategories(model);
      return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> UploadDocument(int folderId, int? requiredDocId)
    {
      // Global Query Filter ensures only current tenant's folder is found
      var folder = await _context.ComplianceFolders.FindAsync(folderId);
      if (folder == null)
      {
        TempData["ErrorMessage"] = "Compliance Folder not found or does not belong to your organization.";
        return NotFound();
      }

      var viewModel = new UploadDocumentViewModel
      {
        ComplianceFolderId = folderId,
        ComplianceFolderName = folder.Name
      };

      if (requiredDocId.HasValue)
      {
        // RequiredDocument query will also be indirectly filtered through ComplianceFolder.
        // If you fetch a requiredDoc by ID, and its parent folder is not the current tenant's,
        // it won't be found because of the global filter on ComplianceFolder.
        var requiredDoc = await _context.RequiredDocuments.FindAsync(requiredDocId.Value);
        if (requiredDoc == null || requiredDoc.ComplianceFolderId != folderId) // Add extra check for security
        {
          TempData["ErrorMessage"] = "Required Document not found or mismatch.";
          return NotFound();
        }
        viewModel.RequiredDocumentId = requiredDocId.Value;
        viewModel.RequiredDocumentName = requiredDoc.DocumentName;
        viewModel.Description = requiredDoc.Description;
      }

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(UploadDocumentViewModel model)
    {
      if (ModelState.IsValid)
      {
        // Global Query Filter ensures only current tenant's folder is found
        var folder = await _context.ComplianceFolders
                                     .Include(f => f.RequiredDocuments)
                                     .FirstOrDefaultAsync(f => f.Id == model.ComplianceFolderId);

        if (folder == null)
        {
          TempData["ErrorMessage"] = "Compliance Folder not found or does not belong to your organization.";
          return NotFound();
        }

        if (model.File == null || model.File.Length == 0)
        {
          ModelState.AddModelError("File", "Please select a file to upload.");
          await PopulateUploadDocumentViewModel(model, folder);
          return View(model);
        }

        // Define the path to save the file
        // Use the tenant ID in the path to ensure tenant-specific storage
        var tenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
          TempData["ErrorMessage"] = "Cannot upload document: Current tenant not identified.";
          await PopulateUploadDocumentViewModel(model, folder);
          return View(model);
        }

        var tenantUploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", tenantId);
        if (!Directory.Exists(tenantUploadsFolder))
        {
          Directory.CreateDirectory(tenantUploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
        var filePath = Path.Combine(tenantUploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await model.File.CopyToAsync(stream);
        }

        string uploadedByUserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        var document = new Document
        {
          FileName = model.File.FileName,
          // --- NEW: Store tenant-specific path ---
          FilePath = $"/uploads/{tenantId}/{uniqueFileName}",
          FileType = Path.GetExtension(model.File.FileName),
          FileSize = model.File.Length,
          Description = model.Description,
          UploadDate = DateTime.Now,
          UploadedBy = uploadedByUserName,
          ComplianceFolderId = model.ComplianceFolderId,
          RequiredDocumentId = model.RequiredDocumentId
          // Document does not need TenantId directly, as it's linked to ComplianceFolder
        };

        _context.Documents.Add(document);

        if (model.RequiredDocumentId.HasValue)
        {
          var requiredDoc = folder.RequiredDocuments.FirstOrDefault(rd => rd.Id == model.RequiredDocumentId.Value);
          if (requiredDoc != null && !requiredDoc.IsSubmitted)
          {
            requiredDoc.IsSubmitted = true;
            requiredDoc.SubmissionDate = DateTime.Now;
            requiredDoc.SubmittedBy = uploadedByUserName;
            _context.RequiredDocuments.Update(requiredDoc);
          }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Document '{model.File.FileName}' uploaded successfully!";
        return RedirectToAction(nameof(FolderDetails), new { id = model.ComplianceFolderId });
      }

      var currentFolder = await _context.ComplianceFolders.FindAsync(model.ComplianceFolderId);
      await PopulateUploadDocumentViewModel(model, currentFolder);
      return View(model);
    }

    private async Task PopulateUploadDocumentViewModel(UploadDocumentViewModel viewModel, ComplianceFolder? folder)
    {
      if (folder != null)
      {
        viewModel.ComplianceFolderName = folder.Name;
      }

      if (viewModel.RequiredDocumentId.HasValue)
      {
        var requiredDoc = await _context.RequiredDocuments.FindAsync(viewModel.RequiredDocumentId.Value);
        if (requiredDoc != null)
        {
          viewModel.RequiredDocumentName = requiredDoc.DocumentName;
        }
      }
    }

    public async Task<IActionResult> DownloadDocument(int id)
    {
      // Global Query Filter ensures only current tenant's document (via folder) is found
      var document = await _context.Documents
          .Include(d => d.ComplianceFolder) // Include folder to check tenant context
          .FirstOrDefaultAsync(d => d.Id == id);

      if (document == null)
      {
        TempData["ErrorMessage"] = "Document not found or does not belong to your organization.";
        return NotFound();
      }

      // --- SECURITY CHECK: Ensure document's folder belongs to the current tenant ---
      // This is implicitly handled by global filter, but an explicit check can be good for critical actions.
      if (document.ComplianceFolder.TenantId != _tenantService.GetCurrentTenantId())
      {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to download this document.";
        return Unauthorized(); // Or return Forbid(), or RedirectToAction("AccessDenied")
      }


      var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));

      if (!System.IO.File.Exists(filePath))
      {
        TempData["ErrorMessage"] = "The requested file was not found on the server.";
        return RedirectToAction(nameof(FolderDetails), new { id = document.ComplianceFolderId });
      }

      var contentType = GetContentType(Path.GetExtension(document.FileName));
      return PhysicalFile(filePath, contentType, document.FileName);
    }

    private async Task<object> GetDashboardStats()
    {
      // Global Query Filter ensures these counts are tenant-specific
      var totalFolders = await _context.ComplianceFolders.CountAsync();
      var totalDocuments = await _context.Documents.CountAsync();
      var pendingSubmissions = await _context.RequiredDocuments
          .Where(rd => rd.IsRequired && !rd.IsSubmitted)
          .CountAsync();

      var totalRequired = await _context.RequiredDocuments
          .Where(rd => rd.IsRequired)
          .CountAsync();
      var totalSubmitted = await _context.RequiredDocuments
          .Where(rd => rd.IsRequired && rd.IsSubmitted)
          .CountAsync();

      var complianceRate = totalRequired > 0 ? (totalSubmitted * 100) / totalRequired : 100;

      return new
      {
        TotalFolders = totalFolders,
        TotalDocuments = totalDocuments,
        PendingSubmissions = pendingSubmissions,
        ComplianceRate = complianceRate
      };
    }

    private async Task PopulateComplianceCategories(CreateFolderViewModel viewModel)
    {
      // Global Query Filter ensures these categories are tenant-specific
      var categories = await _context.ComplianceCategories
                                      .OrderBy(c => c.Name)
                                      .ToListAsync();
      viewModel.ComplianceCategories = new SelectList(categories, "Id", "Name");
    }

    private string GetContentType(string fileExtension)
    {
      return fileExtension.ToLowerInvariant() switch
      {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".txt" => "text/plain",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
      };
    }
  }
}
