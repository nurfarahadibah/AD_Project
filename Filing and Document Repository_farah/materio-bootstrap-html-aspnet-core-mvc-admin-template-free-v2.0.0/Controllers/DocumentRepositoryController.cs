using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers
{
  public class DocumentRepositoryController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DocumentRepositoryController(AppDbContext context, IWebHostEnvironment environment)
    {
      _context = context;
      _environment = environment;
    }

    public async Task<IActionResult> Index(string searchQuery = "", string filter = "all")
    {
      var query = _context.ComplianceFolders
          .Include(f => f.Documents)
          .Include(f => f.RequiredDocuments)
          .AsQueryable();

      if (!string.IsNullOrEmpty(searchQuery))
      {
        query = query.Where(f => f.Name.Contains(searchQuery) ||
                                f.Description.Contains(searchQuery) ||
                                f.ComplianceType.Contains(searchQuery));
      }

      if (filter != "all")
      {
        switch (filter.ToLower())
        {
          case "sox":
            query = query.Where(f => f.ComplianceType.Contains("SOX"));
            break;
          case "iso":
            query = query.Where(f => f.ComplianceType.Contains("ISO"));
            break;
          case "gdpr":
            query = query.Where(f => f.ComplianceType.Contains("GDPR"));
            break;
          case "pending":
            query = query.Where(f => f.Status == FolderStatus.InReview);
            break;
        }
      }

      var folders = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

      ViewBag.SearchQuery = searchQuery;
      ViewBag.Filter = filter;
      ViewBag.Stats = await GetDashboardStats();

      return View(folders);
    }

    [HttpGet]
    public IActionResult CreateFolder()
    {
      var viewModel = new CreateFolderViewModel();
      ViewBag.ComplianceTypes = GetComplianceTypes();
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(CreateFolderViewModel model)
    {
      if (ModelState.IsValid)
      {
        string createdByUserName = "System";
        if (User.Identity != null && User.Identity.Name != null)
        {
          createdByUserName = User.Identity.Name;
        }

        var folder = new ComplianceFolder
        {
          Name = model.Name,
          ComplianceType = model.ComplianceType,
          Description = model.Description,
          CreatedBy = createdByUserName // <--- Assign the checked value
        };


        _context.ComplianceFolders.Add(folder);
        await _context.SaveChangesAsync();


        // Add required documents
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

      ViewBag.ComplianceTypes = GetComplianceTypes();
     
      return View(model);
    }

    public async Task<IActionResult> FolderDetails(int id)
    {
      var folder = await _context.ComplianceFolders
          .Include(f => f.Documents)
          .Include(f => f.RequiredDocuments)
              .ThenInclude(rd => rd.Documents)
          .FirstOrDefaultAsync(f => f.Id == id);

      if (folder == null)
      {
        return NotFound();
      }

      return View(folder);
    }

    [HttpGet] // This GET action is likely used for the "Upload General Document" button, not the modal
    public async Task<IActionResult> UploadDocument(int folderId)
    {
      var folder = await _context.ComplianceFolders.FindAsync(folderId);
      if (folder == null)
      {
        return NotFound();
      }

      var viewModel = new DocumentUploadViewModel
      {
        ComplianceFolderId = folderId,
        FolderName = folder.Name
        // If this GET action is used to display a form with a dropdown for RequiredDocuments,
        // you would need to populate AvailableRequiredDocuments here.
        // Otherwise, it's fine for a general upload form.
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(DocumentUploadViewModel model)
    {
      // First, ensure the model is valid and a file was actually uploaded.
      // 'model.File != null' ensures that the IFormFile was successfully bound from the request.
      if (ModelState.IsValid && model.File != null)
      {
        var folder = await _context.ComplianceFolders.FindAsync(model.ComplianceFolderId);
        if (folder == null)
        {
          TempData["Error"] = "Compliance folder not found.";
          return NotFound(); // Or RedirectToAction(nameof(Index))
        }

        // 1. Handle file storage
        // Create directory for the folder's uploads if it doesn't exist
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", folder.Id.ToString());
        Directory.CreateDirectory(uploadsPath); // Creates all directories in the path if they don't exist

        // Generate a unique filename to prevent overwrites and provide a clean file name
        var fileName = Path.GetFileNameWithoutExtension(model.File.FileName);
        var extension = Path.GetExtension(model.File.FileName);
        var uniqueFileName = $"{fileName}_{DateTime.Now.Ticks}{extension}"; // Appends a timestamp to make it unique
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        // Save the file to the server's file system
        try
        {
          using (var stream = new FileStream(filePath, FileMode.Create))
          {
            await model.File.CopyToAsync(stream);
          }
        }
        catch (IOException ex)
        {
          // Log the exception (e.g., using a logger like ILogger)
          Console.WriteLine($"Error saving file: {ex.Message}");
          ModelState.AddModelError("", "Could not save the file to disk. Please try again.");
          TempData["Error"] = "Failed to save the document file. Please check server permissions.";
          // Re-populate model data needed for the view if returning view
          // model.FolderName = folder.Name;
          // model.AvailableRequiredDocuments = ... (if used)
          return View(model); // Return to the upload view with error
        }


        // Determine the 'UploadedBy' user.
        string uploadedByUserName = "System"; // Default to "System" if user is not authenticated
        if (User.Identity != null && User.Identity.IsAuthenticated && !string.IsNullOrEmpty(User.Identity.Name))
        {
          uploadedByUserName = User.Identity.Name;
        }

        // 2. Create the Document record for the database
        var document = new Document
        {
          FileName = model.File.FileName, // Original filename
          FilePath = Path.Combine("uploads", folder.Id.ToString(), uniqueFileName), // Relative path for database storage
          FileType = extension,
          FileSize = model.File.Length,
          Description = model.Description,
          ComplianceFolderId = model.ComplianceFolderId,
          UploadDate = DateTime.Now, // Set the upload date
          UploadedBy = uploadedByUserName
        };

        // 3. IMPORTANT: Link to RequiredDocument and mark as submitted if applicable
        // The modal sends SelectedRequiredDocumentId if a specific required document upload button was clicked
        if (model.SelectedRequiredDocumentId.HasValue)
        {
          document.RequiredDocumentId = model.SelectedRequiredDocumentId.Value; // Assign the ID

          // Retrieve the RequiredDocument from the database to update its status
          var requiredDocument = await _context.RequiredDocuments.FindAsync(model.SelectedRequiredDocumentId.Value);
          if (requiredDocument != null)
          {
            requiredDocument.IsSubmitted = true;
            requiredDocument.SubmissionDate = DateTime.Now; // Mark submission date
            requiredDocument.SubmittedBy = uploadedByUserName; // Mark who submitted it
            _context.RequiredDocuments.Update(requiredDocument); // Tell EF Core to update this entity
          }
        }

        _context.Documents.Add(document); // Add the new Document entity
        await _context.SaveChangesAsync(); // Save all changes (new Document, and updated RequiredDocument)

        TempData["Success"] = "Document uploaded successfully!";
        // Redirect back to the FolderDetails view to show the updated list
        return RedirectToAction(nameof(FolderDetails), new { id = model.ComplianceFolderId });
      }
      else
      {
        // If ModelState is not valid or no file was selected, return to the view
        // You might want to get the FolderName back for the view if it's used there
        var folderForModel = await _context.ComplianceFolders.FindAsync(model.ComplianceFolderId);
        if (folderForModel != null)
        {
          model.FolderName = folderForModel.Name;
        }
        TempData["Error"] = "Please select a file to upload and provide valid details.";
        return View(model); // Returns the view, displaying validation errors
      }
    }

    public async Task<IActionResult> DownloadDocument(int id)
    {
      var document = await _context.Documents.FindAsync(id);
      if (document == null)
      {
        return NotFound();
      }

      var filePath = Path.Combine(_environment.WebRootPath, document.FilePath);
      if (!System.IO.File.Exists(filePath))
      {
        return NotFound();
      }

      var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
      var contentType = GetContentType(document.FileType);

      return File(fileBytes, contentType, document.FileName);
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

    private List<string> GetComplianceTypes()
    {
      return new List<string>
            {
                "SOX (Sarbanes-Oxley)",
                "ISO 27001",
                "GDPR",
                "Financial Audit",
                "Security Compliance",
                "Quality Management",
                "Risk Management",
                "Custom"
            };
    }


    private string GetContentType(string fileExtension)
    {
      return fileExtension.ToLower() switch
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
