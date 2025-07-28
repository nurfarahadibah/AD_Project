using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System; // Required for DateTime.Now

namespace AspnetCoreMvcFull.Controllers
{
  // Ensure only authorized users (e.g., Admins) can manage categories
  [Authorize(Roles = "Admin")] // Adjust role as per your application's authorization
  public class ComplianceCategoriesController : Controller
  {
    private readonly AppDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ComplianceCategoriesController(AppDbContext context, ITenantService tenantService, IHttpContextAccessor httpContextAccessor)
    {
      _context = context;
      _tenantService = tenantService;
      _httpContextAccessor = httpContextAccessor;
    }

    // GET: ComplianceCategories
    // Displays a list of all compliance categories for the current tenant.
    public async Task<IActionResult> Index()
    {
      // The global query filter in AppDbContext ensures only categories for the current tenant are retrieved.
      var complianceCategories = await _context.ComplianceCategories
                                                  .OrderBy(cc => cc.Name)
                                                  .Select(cc => new ComplianceCategoryViewModel
                                                  {
                                                    Id = cc.Id,
                                                    Name = cc.Name,
                                                    Code = cc.Code,
                                                    Description = cc.Description,
                                                    CreatedBy = cc.CreatedBy,
                                                    CreatedDate = cc.CreatedDate,
                                                    LastModifiedBy = cc.LastModifiedBy,
                                                    LastModifiedDate = cc.LastModifiedDate,
                                                    Status = cc.Status // Include Status
                                                  })
                                                  .ToListAsync();
      return View(complianceCategories);
    }

    // GET: ComplianceCategories/Create
    // Displays the form to create a new compliance category.
    public IActionResult Create()
    {
      return View(new CreateComplianceCategoryViewModel());
    }

    // POST: ComplianceCategories/Create
    // Handles the submission of the new compliance category form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateComplianceCategoryViewModel model)
    {
      if (ModelState.IsValid)
      {
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
          TempData["ErrorMessage"] = "Cannot create compliance category: Current tenant not identified.";
          return View(model);
        }

        var createdByUserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        var complianceCategory = new ComplianceCategory
        {
          Name = model.Name,
          Code = model.Code,
          Description = model.Description,
          TenantId = currentTenantId,
          CreatedBy = createdByUserName,
          CreatedDate = DateTime.Now,
          Status = 1 // Default to active when created
        };

        _context.Add(complianceCategory);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Compliance Category '{complianceCategory.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
      }
      // If model state is invalid, return the view with validation errors.
      return View(model);
    }

    // GET: ComplianceCategories/Edit/5
    // Displays the form to edit an existing compliance category.
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      // The global query filter ensures only the current tenant's category is found.
      var complianceCategory = await _context.ComplianceCategories.FindAsync(id);

      if (complianceCategory == null)
      {
        return NotFound();
      }

      var viewModel = new EditComplianceCategoryViewModel
      {
        Id = complianceCategory.Id,
        Name = complianceCategory.Name,
        Code = complianceCategory.Code,
        Description = complianceCategory.Description
      };
      return View(viewModel);
    }

    // POST: ComplianceCategories/Edit/5
    // Handles the submission of the edit compliance category form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditComplianceCategoryViewModel model)
    {
      if (id != model.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        // Retrieve the existing category, global filter handles tenant.
        var complianceCategory = await _context.ComplianceCategories.FindAsync(id);

        if (complianceCategory == null)
        {
          TempData["ErrorMessage"] = "Compliance Category not found or does not belong to your organization.";
          return NotFound();
        }

        try
        {
          complianceCategory.Name = model.Name;
          complianceCategory.Code = model.Code;
          complianceCategory.Description = model.Description;
          complianceCategory.LastModifiedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
          complianceCategory.LastModifiedDate = DateTime.Now;

          _context.Update(complianceCategory);
          await _context.SaveChangesAsync();
          TempData["SuccessMessage"] = $"Compliance Category '{complianceCategory.Name}' updated successfully.";
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ComplianceCategoryExists(complianceCategory.Id))
          {
            return NotFound();
          }
          else
          {
            throw; // Re-throw if it's not a "not found" concurrency issue.
          }
        }
        return RedirectToAction(nameof(Index));
      }
      return View(model);
    }

    // --- NEW: Archive Compliance Category ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
      var complianceCategory = await _context.ComplianceCategories
                                              .Include(cc => cc.ComplianceFolders)
                                              .Include(cc => cc.JenisForms)
                                              .FirstOrDefaultAsync(m => m.Id == id);

      if (complianceCategory == null)
      {
        TempData["ErrorMessage"] = "Compliance Category not found or does not belong to your organization.";
        return NotFound();
      }

      // Check for dependent records before archiving (same logic as delete)
      if (complianceCategory.ComplianceFolders.Any())
      {
        TempData["ErrorMessage"] = $"Cannot archive Compliance Category '{complianceCategory.Name}' because there are associated Compliance Folders. Please reassign or delete the folders first.";
        return RedirectToAction(nameof(Index));
      }

      if (complianceCategory.JenisForms.Any())
      {
        TempData["ErrorMessage"] = $"Cannot archive Compliance Category '{complianceCategory.Name}' because there are associated Form Types. Please reassign or delete the form types first.";
        return RedirectToAction(nameof(Index));
      }

      complianceCategory.Status = 0; // Set status to Archived
      complianceCategory.LastModifiedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
      complianceCategory.LastModifiedDate = DateTime.Now;

      _context.Update(complianceCategory);
      await _context.SaveChangesAsync();
      TempData["SuccessMessage"] = $"Compliance Category '{complianceCategory.Name}' archived successfully.";
      return RedirectToAction(nameof(Index));
    }

    // --- NEW: Unarchive Compliance Category ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unarchive(int id)
    {
      var complianceCategory = await _context.ComplianceCategories.FindAsync(id);

      if (complianceCategory == null)
      {
        TempData["ErrorMessage"] = "Compliance Category not found or does not belong to your organization.";
        return NotFound();
      }

      complianceCategory.Status = 1; // Set status to Active
      complianceCategory.LastModifiedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
      complianceCategory.LastModifiedDate = DateTime.Now;

      _context.Update(complianceCategory);
      await _context.SaveChangesAsync();
      TempData["SuccessMessage"] = $"Compliance Category '{complianceCategory.Name}' unarchived successfully.";
      return RedirectToAction(nameof(Index));
    }

    // Helper method to check if a compliance category exists for the current tenant.
    private bool ComplianceCategoryExists(int id)
    {
      return _context.ComplianceCategories.Any(e => e.Id == id);
    }
  }
}
