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
              .ThenInclude(rd => rd.Document)
          .FirstOrDefaultAsync(f => f.Id == id);

      if (folder == null)
      {
        return NotFound();
      }

      return View(folder);
    }

    [HttpGet]
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
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(DocumentUploadViewModel model)
    {
      if (ModelState.IsValid && model.File != null)
      {
        var folder = await _context.ComplianceFolders.FindAsync(model.ComplianceFolderId);
        if (folder == null)
        {
          return NotFound();
        }

        // Create uploads directory if it doesn't exist
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", folder.Id.ToString());
        Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var fileName = Path.GetFileNameWithoutExtension(model.File.FileName);
        var extension = Path.GetExtension(model.File.FileName);
        var uniqueFileName = $"{fileName}_{DateTime.Now.Ticks}{extension}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await model.File.CopyToAsync(stream);
        }
        string createdByUserName = "System";
        if (User.Identity != null && User.Identity.Name != null)
        {
          createdByUserName = User.Identity.Name;
        }

        // Save document record
        var document = new Document
        {
          FileName = model.File.FileName,
          FilePath = Path.Combine("uploads", folder.Id.ToString(), uniqueFileName),
          FileType = extension,
          FileSize = model.File.Length,
          Description = model.Description,
          ComplianceFolderId = model.ComplianceFolderId,
          UploadedBy = createdByUserName
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document uploaded successfully!";
        return RedirectToAction(nameof(FolderDetails), new { id = model.ComplianceFolderId });
      }

      return View(model);
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
