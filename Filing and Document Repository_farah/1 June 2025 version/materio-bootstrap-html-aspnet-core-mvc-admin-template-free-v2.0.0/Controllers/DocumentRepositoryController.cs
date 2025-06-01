using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Linq; // Add this using directive for .ToList() on IEnumerable

namespace AspnetCoreMvcFull.Controllers
{
  public class DocumentRepositoryController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentRepositoryController(AppDbContext context, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
      _context = context;
      _environment = environment;
      _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Index(string searchQuery = "", string filter = "all", string statusFilter = "all")
    {
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

      var complianceCategories = await _context.ComplianceCategories.OrderBy(c => c.Name).ToListAsync();
      ViewBag.ComplianceCategories = complianceCategories;

      if (filter != "all")
      {
        if (int.TryParse(filter, out int categoryId))
        {
          query = query.Where(f => f.ComplianceCategoryId == categoryId);
        }
        else
        {
          
        }
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
      ViewBag.Stats = await GetDashboardStats();

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

      var foldersToUpdate = await _context.ComplianceFolders
                                          .Where(f => model.FolderIds.Contains(f.Id))
                                          .ToListAsync();

      if (!foldersToUpdate.Any())
      {
        TempData["ErrorMessage"] = "Selected folders could not be found.";
        return RedirectToAction(nameof(Index));
      }

      string lastModifiedByUserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
      var newStatusEnum = (FolderStatus)parsedNewStatus;
      int updatedCount = 0;

      foreach (var folder in foldersToUpdate)
      {
        // Optional: Add logic to prevent status changes if needed (e.g., cannot change from Archived to Active directly)
        // For simplicity, we'll allow any change for now, but you might want specific rules.
        if (folder.Status != newStatusEnum) // Only update if the status is actually changing
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
      var folder = await _context.ComplianceFolders.FindAsync(id);

      if (folder == null)
      {
        TempData["ErrorMessage"] = "Folder not found.";
        return NotFound();
      }

      if (folder.Status == FolderStatus.Archived)
      {
        folder.Status = FolderStatus.Active;
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveFolder(int id)
    {
      var folder = await _context.ComplianceFolders.FindAsync(id);

      if (folder == null)
      {
        TempData["ErrorMessage"] = "Folder not found.";
        return NotFound();
      }

      if (folder.Status == FolderStatus.Active || folder.Status == FolderStatus.Completed)
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
    // ... (rest of your existing controller actions: CreateFolder, FolderDetails, EditFolder, UploadDocument, DownloadDocument, GetDashboardStats, PopulateComplianceCategories, GetContentType) ...
    [HttpGet]
    public async Task<IActionResult> CreateFolder()
    {
      var viewModel = new CreateFolderViewModel();
      await PopulateComplianceCategories(viewModel); // Populate the SelectList from DB
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(CreateFolderViewModel model)
    {
      if (ModelState.IsValid)
      {
        string createdByUserName = "System"; // Default value
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(_httpContextAccessor.HttpContext.User.Identity.Name))
        {
          createdByUserName = _httpContextAccessor.HttpContext.User.Identity.Name;
        }

        var folder = new ComplianceFolder
        {
          Name = model.Name,
          ComplianceCategoryId = model.ComplianceCategoryId,
          Description = model.Description,
          CreatedBy = createdByUserName
        };

        _context.ComplianceFolders.Add(folder);
        await _context.SaveChangesAsync();

        foreach (var reqDoc in model.RequiredDocuments)
        {
          var requiredDocument = new RequiredDocument
          {
            ComplianceFolderId = folder.Id,
            DocumentName = reqDoc.DocumentName,
            Description = reqDoc.Description,
            IsRequired = reqDoc.IsRequired
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
        var folderToUpdate = await _context.ComplianceFolders
            .Include(f => f.RequiredDocuments)
            .FirstOrDefaultAsync(f => f.Id == model.Id);

        if (folderToUpdate == null)
        {
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
      var folder = await _context.ComplianceFolders.FindAsync(folderId);
      if (folder == null)
      {
        TempData["ErrorMessage"] = "Compliance Folder not found.";
        return NotFound();
      }

      var viewModel = new UploadDocumentViewModel
      {
        ComplianceFolderId = folderId,
        ComplianceFolderName = folder.Name
      };

      if (requiredDocId.HasValue)
      {
        var requiredDoc = await _context.RequiredDocuments.FindAsync(requiredDocId.Value);
        if (requiredDoc == null)
        {
          TempData["ErrorMessage"] = "Required Document not found.";
          return NotFound();
        }
        viewModel.RequiredDocumentId = requiredDocId.Value;
        viewModel.RequiredDocumentName = requiredDoc.DocumentName;
        // Pre-fill description if the required document has one
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
        var folder = await _context.ComplianceFolders
                                   .Include(f => f.RequiredDocuments) // Include RequiredDocuments for potential updates
                                   .FirstOrDefaultAsync(f => f.Id == model.ComplianceFolderId);

        if (folder == null)
        {
          TempData["ErrorMessage"] = "Compliance Folder not found.";
          return NotFound();
        }

        if (model.File == null || model.File.Length == 0)
        {
          ModelState.AddModelError("File", "Please select a file to upload.");
          await PopulateUploadDocumentViewModel(model, folder); // Repopulate if returning to view
          return View(model);
        }

        // Define the path to save the file
        // Ensure the "uploads" folder exists in your wwwroot directory
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
          Directory.CreateDirectory(uploadsFolder);
        }

        // Create a unique file name to prevent conflicts
        var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await model.File.CopyToAsync(stream);
        }

        string uploadedByUserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        var document = new Document
        {
          FileName = model.File.FileName,
          FilePath = "/uploads/" + uniqueFileName, // Store relative path
          FileType = Path.GetExtension(model.File.FileName),
          FileSize = model.File.Length,
          Description = model.Description,
          UploadDate = DateTime.Now,
          UploadedBy = uploadedByUserName,
          ComplianceFolderId = model.ComplianceFolderId,
          RequiredDocumentId = model.RequiredDocumentId // Link to RequiredDocument if applicable
        };

        _context.Documents.Add(document);

        // If this is an upload for a Required Document, update its status
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

      // If ModelState is not valid, re-populate and return view
      var currentFolder = await _context.ComplianceFolders.FindAsync(model.ComplianceFolderId);
      await PopulateUploadDocumentViewModel(model, currentFolder);
      return View(model);
    }

    // Helper method for populating view model in case of validation errors
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

    // Add a DownloadDocument action (if you don't have it already)
    public async Task<IActionResult> DownloadDocument(int id)
    {
      var document = await _context.Documents.FindAsync(id);
      if (document == null)
      {
        return NotFound();
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
