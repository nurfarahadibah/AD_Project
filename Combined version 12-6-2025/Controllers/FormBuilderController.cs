using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Services; // For ITenantService
using System.Linq;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Controllers
{
  public class FormBuilderController : Controller
  {
    private readonly AppDbContext _context;
    private readonly ITenantService _tenantService; // --- NEW: Inject ITenantService ---

    public FormBuilderController(AppDbContext context, ITenantService tenantService) // --- NEW: Inject ITenantService ---
    {
      _context = context;
      _tenantService = tenantService; // --- NEW: Assign ITenantService ---
    }

    public async Task<IActionResult> Index(int? complianceCategoryId, FormStatus? formStatus, string sortOrder) // Added sortOrder parameter
    {
      // Global Query Filter in AppDbContext should automatically handle TenantId filtering here.
      var jenisFormsQuery = _context.JenisForms
          .Include(f => f.Sections)
              .ThenInclude(s => s.Items)
          .Include(f => f.ComplianceCategory)
          .AsQueryable();

      if (complianceCategoryId.HasValue && complianceCategoryId.Value > 0)
      {
        jenisFormsQuery = jenisFormsQuery.Where(f => f.ComplianceCategoryId == complianceCategoryId.Value);
      }

      // Only apply the status filter if a specific status has been provided.
      if (formStatus.HasValue)
      {
        jenisFormsQuery = jenisFormsQuery.Where(f => f.Status == formStatus.Value);
      }

      // --- Apply Sorting ---
      switch (sortOrder)
      {
        case "latest":
          jenisFormsQuery = jenisFormsQuery.OrderByDescending(f => f.CreatedAt);
          break;
        default:
          // Default sorting if no specific sortOrder is provided or it's unknown
          jenisFormsQuery = jenisFormsQuery.OrderBy(f => f.Name); // Example default: sort by Name ascending
          break;
      }
      // --- End Apply Sorting ---

      var jenisForms = await jenisFormsQuery.ToListAsync();

      // Pass compliance categories for the filter dropdown
      // Global Query Filter ensures these categories are tenant-specific
      ViewBag.ComplianceCategories = await _context.ComplianceCategories
          .Select(c => new SelectListItem
          {
            Value = c.Id.ToString(),
            Text = c.Name
          })
          .OrderBy(c => c.Text)
          .ToListAsync();

      ViewBag.SelectedComplianceCategoryId = complianceCategoryId;
      ViewBag.SelectedFormStatus = formStatus;
      ViewBag.CurrentSortOrder = sortOrder; // Pass the current sort order to the view

      ViewBag.CurrentTenantId = _tenantService.GetCurrentTenantId(); // Get current tenant ID for display/debug

      return View(jenisForms);
    }

    [HttpGet]
    public async Task<IActionResult> CreateForm()
    {
      // Global Query Filter ensures these categories are tenant-specific
      var complianceCategories = await _context.ComplianceCategories
          .Select(c => new SelectListItem
          {
            Value = c.Id.ToString(),
            Text = c.Name
          })
          .OrderBy(c => c.Text) // Order categories alphabetically
          .ToListAsync();

      var viewModel = new CreateJenisFormViewModel
      {
        ComplianceCategories = complianceCategories
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForm(CreateJenisFormViewModel model)
    {
      if (ModelState.IsValid)
      {
        // --- NEW: Assign TenantId to the new form ---
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
          TempData["ErrorMessage"] = "Cannot create form: Current tenant not identified.";
          await PopulateComplianceCategoriesForCreateModel(model); // Helper to repopulate dropdown
          return View(model);
        }

        var jenisForm = new JenisForm
        {
          Name = model.Name,
          Description = model.Description,
          ComplianceCategoryId = model.ComplianceCategoryId,
          CreatedAt = DateTime.Now,
          Status = FormStatus.Draft, // Set default status on creation
          TenantId = currentTenantId // --- Assign the current tenant's ID ---
        };

        _context.JenisForms.Add(jenisForm);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Form created successfully!";
        return RedirectToAction("Builder", new { id = jenisForm.FormTypeId });
      }

      await PopulateComplianceCategoriesForCreateModel(model);
      return View(model);
    }

    public async Task<IActionResult> Builder(int id)
    {
      // Global Query Filter ensures only current tenant's form is found
      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections.OrderBy(s => s.Order)) // Order sections
              .ThenInclude(s => s.Items.OrderBy(item => item.Order)) // Order items as well
          .FirstOrDefaultAsync(f => f.FormTypeId == id);

      if (jenisForm == null)
      {
        return NotFound();
      }

      var viewModel = new FormBuilderViewModel
      {
        JenisForm = jenisForm,
        Sections = jenisForm.Sections.ToList() // Already ordered by Include
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSection(CreateSectionViewModel model)
    {
      if (ModelState.IsValid)
      {
        // Check if the FormTypeId belongs to the current tenant
        var form = await _context.JenisForms.FindAsync(model.FormTypeId);
        if (form == null || form.TenantId != _tenantService.GetCurrentTenantId())
        {
          return Json(new { success = false, message = "Access Denied: Form not found or does not belong to your organization." });
        }

        var maxOrder = await _context.FormSections
            .Where(s => s.FormTypeId == model.FormTypeId)
            .MaxAsync(s => (int?)s.Order) ?? 0;

        var section = new FormSection
        {
          FormTypeId = model.FormTypeId,
          Title = model.Title,
          Description = model.Description,
          Order = maxOrder + 1
          // FormSection does not need TenantId directly, as it's linked to JenisForm which has it.
        };

        _context.FormSections.Add(section);
        await _context.SaveChangesAsync();
        return Json(new { success = true, sectionId = section.SectionId, formTypeId = section.FormTypeId });
      }
      return Json(new { success = false, message = "Invalid model state for adding section.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int sectionId, ItemType itemType)
    {
      // Eagerly load the JenisForm to check tenant ID
      var section = await _context.FormSections
                                  .Include(s => s.JenisForm)
                                  .FirstOrDefaultAsync(s => s.SectionId == sectionId);

      if (section == null)
      {
        return NotFound("Section not found.");
      }

      // --- SECURITY CHECK: Ensure section's form belongs to the current tenant ---
      if (section.JenisForm == null || section.JenisForm.TenantId != _tenantService.GetCurrentTenantId())
      {
        return Json(new { success = false, message = "Access Denied: Section or its associated form not found or does not belong to your organization." });
      }

      var maxOrder = await _context.FormItems
          .Where(i => i.SectionId == sectionId)
          .MaxAsync(i => (int?)i.Order) ?? 0;

      var item = new FormItem
      {
        SectionId = sectionId,
        Question = $"New {itemType} Question",
        ItemType = itemType,
        IsRequired = false,
        Order = maxOrder + 1,
        MaxScore = null
        // FormItem does not need TenantId directly, as it's linked to FormSection which is linked to JenisForm.
      };

      if (itemType == ItemType.Radio || itemType == ItemType.Checkbox || itemType == ItemType.Dropdown)
      {
        var defaultOptions = new List<string> { "Option 1", "Option 2" };
        item.OptionsJson = JsonConvert.SerializeObject(defaultOptions);
        item.MaxScore = 1; // Default score for choice types
      }

      _context.FormItems.Add(item);
      await _context.SaveChangesAsync();

      return Json(new { success = true, itemId = item.ItemId, sectionId = item.SectionId, formTypeId = section.FormTypeId });
    }

    // Combined ConfigureItem for GET and POST
    [HttpGet]
    public async Task<IActionResult> ConfigureItem(int id)
    {
      // Global Query Filter ensures only current tenant's item (via form) is found
      var item = await _context.FormItems
          .Include(i => i.Section)
              .ThenInclude(s => s.JenisForm)
          .FirstOrDefaultAsync(i => i.ItemId == id);

      if (item == null)
      {
        return NotFound("Item not found.");
      }

      // Explicit tenant check for critical actions, though global filter should handle it
      if (item.Section?.JenisForm?.TenantId != _tenantService.GetCurrentTenantId())
      {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to configure this item.";
        return Unauthorized();
      }

      var itemConfigViewModel = new ItemConfigViewModel
      {
        ItemId = item.ItemId,
        ItemType = item.ItemType,
        Question = item.Question,
        IsRequired = item.IsRequired,
        MaxScore = item.MaxScore,
        HasLooping = item.HasLooping,
        LoopCount = item.LoopCount,
        LoopLabel = item.LoopLabel
      };

      if (!string.IsNullOrEmpty(item.OptionsJson))
      {
        try
        {
          itemConfigViewModel.Options = JsonConvert.DeserializeObject<List<string>>(item.OptionsJson) ?? new List<string>();
        }
        catch (JsonException ex)
        {
          Console.WriteLine($"Error deserializing OptionsJson for ItemId {item.ItemId}: {ex.Message}");
          itemConfigViewModel.Options = new List<string> { "Error: Invalid JSON options format." };
        }
      }

      var viewModel = new ConfigureItemPageViewModel
      {
        FormTypeId = item.Section.FormTypeId,
        FormName = item.Section.JenisForm.Name,
        FormDescription = item.Section.JenisForm.Description,
        ItemConfig = itemConfigViewModel
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureItem(ConfigureItemPageViewModel model)
    {
      if (!ModelState.IsValid)
      {
        // Re-fetch data if needed to re-render the view correctly
        var item = await _context.FormItems
                                 .Include(i => i.Section)
                                     .ThenInclude(s => s.JenisForm)
                                 .FirstOrDefaultAsync(i => i.ItemId == model.ItemConfig.ItemId);
        if (item != null)
        {
          model.FormTypeId = item.Section.FormTypeId;
          model.FormName = item.Section.JenisForm.Name;
          model.FormDescription = item.Section.JenisForm.Description;
        }
        return View(model);
      }

      // Ensure the item belongs to the current tenant before updating
      var itemToUpdate = await _context.FormItems
                                       .Include(i => i.Section)
                                           .ThenInclude(s => s.JenisForm)
                                       .FirstOrDefaultAsync(i => i.ItemId == model.ItemConfig.ItemId);

      if (itemToUpdate == null)
      {
        return NotFound("Item not found for update.");
      }

      if (itemToUpdate.Section?.JenisForm?.TenantId != _tenantService.GetCurrentTenantId())
      {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to modify this item.";
        return Unauthorized();
      }

      itemToUpdate.Question = model.ItemConfig.Question;
      itemToUpdate.IsRequired = model.ItemConfig.IsRequired;
      itemToUpdate.MaxScore = model.ItemConfig.MaxScore;
      itemToUpdate.HasLooping = model.ItemConfig.HasLooping;
      itemToUpdate.LoopCount = model.ItemConfig.LoopCount;
      itemToUpdate.LoopLabel = model.ItemConfig.LoopLabel;

      if (model.ItemConfig.ItemType == ItemType.Radio || model.ItemConfig.ItemType == ItemType.Checkbox || model.ItemConfig.ItemType == ItemType.Dropdown)
      {
        itemToUpdate.OptionsJson = JsonConvert.SerializeObject(model.ItemConfig.Options);
      }
      else
      {
        itemToUpdate.OptionsJson = null;
      }

      try
      {
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Item configuration updated successfully!";
        return RedirectToAction("Builder", new { id = model.FormTypeId });
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!_context.FormItems.Any(e => e.ItemId == model.ItemConfig.ItemId))
        {
          ModelState.AddModelError(string.Empty, "The item was deleted by another user.");
        }
        else
        {
          throw;
        }
      }
      catch (Exception ex)
      {
        ModelState.AddModelError(string.Empty, $"An error occurred while saving: {ex.Message}");
      }

      // Re-fetch data for the view model if save fails and we need to re-render
      var currentItem = await _context.FormItems
                                      .Include(i => i.Section)
                                          .ThenInclude(s => s.JenisForm)
                                      .FirstOrDefaultAsync(i => i.ItemId == model.ItemConfig.ItemId);
      if (currentItem != null)
      {
        model.FormTypeId = currentItem.Section.FormTypeId;
        model.FormName = currentItem.Section.JenisForm.Name;
        model.FormDescription = currentItem.Section.JenisForm.Description;
      }
      return View(model);
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
      // Global Query Filter ensures only current tenant's item (via form) is found
      var item = await _context.FormItems
                               .Include(i => i.Section)
                                   .ThenInclude(s => s.JenisForm)
                               .FirstOrDefaultAsync(i => i.ItemId == id);

      if (item == null)
      {
        return NotFound();
      }

      // Explicit tenant check
      if (item.Section?.JenisForm?.TenantId != _tenantService.GetCurrentTenantId())
      {
        return Unauthorized("Access Denied: You do not have permission to delete this item.");
      }

      _context.FormItems.Remove(item);
      await _context.SaveChangesAsync();
      return Ok(new { success = true, message = "Item deleted successfully." });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSection(int id)
    {
      // Global Query Filter ensures only current tenant's section (via form) is found
      var section = await _context.FormSections
                                  .Include(s => s.Items)
                                  .Include(s => s.JenisForm) // Include JenisForm for tenant check
                                  .FirstOrDefaultAsync(s => s.SectionId == id);
      if (section == null)
      {
        return NotFound();
      }

      // Explicit tenant check
      if (section.JenisForm?.TenantId != _tenantService.GetCurrentTenantId())
      {
        return Unauthorized("Access Denied: You do not have permission to delete this section.");
      }

      _context.FormItems.RemoveRange(section.Items);
      _context.FormSections.Remove(section);
      await _context.SaveChangesAsync();
      return Ok(new { success = true, message = "Section and its items deleted successfully." });
    }

    // Action for updating Form Status (used for archiving/restoring and submitting in Preview)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFormStatus(int formTypeId, FormStatus newStatus)
    {
      // Global Query Filter ensures only current tenant's form is found
      var form = await _context.JenisForms.FindAsync(formTypeId);
      if (form == null)
      {
        TempData["ErrorMessage"] = "Form not found or does not belong to your organization.";
        return RedirectToAction("Index");
      }

      // Explicit tenant check for good measure, though global filter should prevent finding it
      if (form.TenantId != _tenantService.GetCurrentTenantId())
      {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to update this form's status.";
        return Unauthorized();
      }

      form.Status = newStatus;
      try
      {
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Form '{form.Name}' status updated to '{newStatus}' successfully.";
        return RedirectToAction("Index");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error updating status for form ID {formTypeId}: {ex.Message}");
        TempData["ErrorMessage"] = "An error occurred while updating the form status.";
        return RedirectToAction("Index");
      }
    }

    // Action to completely delete a form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFormCompletely(int id)
    {
      // Global Query Filter ensures only current tenant's form is found
      var form = await _context.JenisForms
                               .Include(f => f.Sections)
                                   .ThenInclude(s => s.Items)
                               .FirstOrDefaultAsync(f => f.FormTypeId == id);

      if (form == null)
      {
        TempData["ErrorMessage"] = "Form not found or does not belong to your organization.";
        return NotFound();
      }

      // Explicit tenant check for critical actions
      if (form.TenantId != _tenantService.GetCurrentTenantId())
      {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to delete this form.";
        return Unauthorized();
      }

      // Remove all associated items
      foreach (var section in form.Sections)
      {
        _context.FormItems.RemoveRange(section.Items);
      }
      // Remove all associated sections
      _context.FormSections.RemoveRange(form.Sections);
      // Finally, remove the form itself
      _context.JenisForms.Remove(form);

      try
      {
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Form '{form.Name}' and all its associated data deleted successfully.";
        return RedirectToAction("Index");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error deleting form completely (ID: {id}): {ex.Message}");
        TempData["ErrorMessage"] = "An error occurred while completely deleting the form.";
        return RedirectToAction("Index");
      }
    }

    public async Task<IActionResult> Preview(int id)
    {
      // Global Query Filter ensures only current tenant's form is found
      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections)
              .ThenInclude(s => s.Items)
          .FirstOrDefaultAsync(f => f.FormTypeId == id);

      if (jenisForm == null)
        return NotFound();

      // Explicit tenant check
      if (jenisForm.TenantId != _tenantService.GetCurrentTenantId())
      {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to preview this form.";
        return Unauthorized();
      }

      return View(jenisForm);
    }

    // Helper method to repopulate compliance categories for CreateFormViewModel
    private async Task PopulateComplianceCategoriesForCreateModel(CreateJenisFormViewModel model)
    {
      // Global Query Filter ensures these categories are tenant-specific
      model.ComplianceCategories = await _context.ComplianceCategories
          .Select(c => new SelectListItem
          {
            Value = c.Id.ToString(),
            Text = c.Name
          })
          .OrderBy(c => c.Text)
          .ToListAsync();
    }
  }
}
