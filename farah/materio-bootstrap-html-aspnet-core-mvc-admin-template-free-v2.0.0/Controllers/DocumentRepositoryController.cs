using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Mvc;

namespace AspnetCoreMvcFull.Controllers
{
  public class DocumentRepositoryController : Controller
  {
    private readonly AppDbContext _context;

    public DocumentRepositoryController(AppDbContext context)
    {
      _context = context;
    }

    public IActionResult Index(string searchQuery = "", string filter = "all")
    {
      var folders = _context.ComplianceFolders
          .Include(f => f.Documents)
          .AsQueryable();

      if (!string.IsNullOrEmpty(searchQuery))
      {
        folders = folders.Where(f => f.Name.Contains(searchQuery) ||
                                   f.Description.Contains(searchQuery));
      }

      if (filter != "all")
      {
        folders = folders.Where(f => f.ComplianceType == filter);
      }

      var viewModel = new DocumentRepositoryViewModel
      {
        Folders = folders.ToList(),
        SearchQuery = searchQuery,
        SelectedFilter = filter,
        Stats = new RepositoryStats
        {
          TotalFolders = _context.ComplianceFolders.Count(),
          TotalDocuments = _context.Documents.Count(),
          PendingSubmissions = 8, // Implement your logic
          ComplianceRate = 92 // Implement your logic
        }
      };

      return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder(CreateFolderViewModel model)
    {
      if (ModelState.IsValid)
      {
        var folder = new ComplianceFolder
        {
          Name = model.FolderName,
          ComplianceType = model.ComplianceType,
          Description = model.Description,
          AssignedUsers = model.AssignedUsers
        };

        _context.ComplianceFolders.Add(folder);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Folder created successfully!";
        return RedirectToAction("Index");
      }

      return View("Index", model);
    }

    [HttpPost]
    public async Task<IActionResult> UploadDocument(IFormFile file, int folderId)
    {
      if (file != null && file.Length > 0)
      {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await file.CopyToAsync(stream);
        }

        var document = new Document
        {
          FileName = file.FileName,
          FilePath = fileName,
          FileSize = file.Length,
          ContentType = file.ContentType,
          ComplianceFolderId = folderId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document uploaded successfully!";
      }

      return RedirectToAction("Index");
    }
  }
}
