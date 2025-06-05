using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using System.Linq;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Controllers
{
  public class FormBuilderController : Controller
  {
    private readonly AppDbContext _context;

    public FormBuilderController(AppDbContext context)
    {
      _context = context;
    }

    public async Task<IActionResult> Index(int? complianceCategoryId, FormStatus? formStatus)
    {
      var jenisFormsQuery = _context.JenisForms
          .Include(f => f.Sections)
              .ThenInclude(s => s.Items)
          .Include(f => f.ComplianceCategory)
          .AsQueryable();

      if (complianceCategoryId.HasValue && complianceCategoryId.Value > 0)
      {
        jenisFormsQuery = jenisFormsQuery.Where(f => f.ComplianceCategoryId == complianceCategoryId.Value);
      }

      if (formStatus.HasValue)
      {
        jenisFormsQuery = jenisFormsQuery.Where(f => f.Status == formStatus.Value);
      }

      var jenisForms = await jenisFormsQuery.ToListAsync();

      // Pass compliance categories for the filter dropdown
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

      return View(jenisForms);
    }

    [HttpGet]
    public async Task<IActionResult> CreateForm()
    {
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
        var jenisForm = new JenisForm
        {
          Name = model.Name,
          Description = model.Description,
          ComplianceCategoryId = model.ComplianceCategoryId,
          CreatedAt = DateTime.Now,
          Status = FormStatus.Draft // Set default status on creation
        };

        _context.JenisForms.Add(jenisForm);
        await _context.SaveChangesAsync();

        return RedirectToAction("Builder", new { id = jenisForm.FormTypeId });
      }

      model.ComplianceCategories = await _context.ComplianceCategories
          .Select(c => new SelectListItem
          {
            Value = c.Id.ToString(),
            Text = c.Name
          })
          .OrderBy(c => c.Text)
          .ToListAsync();

      return View(model);
    }

    public async Task<IActionResult> Builder(int id)
    {
      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections)
              .ThenInclude(s => s.Items.OrderBy(item => item.Order)) // Order items as well
          .FirstOrDefaultAsync(f => f.FormTypeId == id);

      if (jenisForm == null)
      {
        return NotFound();
      }

      var viewModel = new FormBuilderViewModel
      {
        JenisForm = jenisForm,
        Sections = jenisForm.Sections.OrderBy(s => s.Order).ToList()
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSection(CreateSectionViewModel model)
    {
      if (ModelState.IsValid)
      {
        var maxOrder = await _context.FormSections
            .Where(s => s.FormTypeId == model.FormTypeId)
            .MaxAsync(s => (int?)s.Order) ?? 0;

        var section = new FormSection
        {
          FormTypeId = model.FormTypeId,
          Title = model.Title,
          Description = model.Description,
          Order = maxOrder + 1
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
      var section = await _context.FormSections.FindAsync(sectionId);
      if (section == null)
      {
        return NotFound();
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
      var item = await _context.FormItems
          .Include(i => i.Section)
              .ThenInclude(s => s.JenisForm)
          .FirstOrDefaultAsync(i => i.ItemId == id);

      if (item == null)
      {
        return NotFound();
      }

      if (item.Section == null || item.Section.JenisForm == null)
      {
        return NotFound("Associated section or form not found for this item.");
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
    // Removed 'int id' from the parameter list.
    // The ItemId will now be bound from the form data within the model.
    public async Task<IActionResult> ConfigureItem(ConfigureItemPageViewModel model) // <--- No 'int id' parameter here
    {
      if (!ModelState.IsValid)
      {
        // If validation fails, re-display the view with errors
        // IMPORTANT: You might need to re-populate any dynamic data (like ComplianceCategories if they were on this page)
        // that is not part of the posted model when returning the view.
        return View(model);
      }

      // Now, access the ItemId from the posted model
      var itemToUpdate = await _context.FormItems.FindAsync(model.ItemConfig.ItemId);
      if (itemToUpdate == null)
      {
        // If item not found, return NotFound (or a more user-friendly error)
        return NotFound("Item not found for update.");
      }

      // Update properties from the ItemConfig sub-model
      itemToUpdate.Question = model.ItemConfig.Question;
      itemToUpdate.IsRequired = model.ItemConfig.IsRequired;
      itemToUpdate.MaxScore = model.ItemConfig.MaxScore;
      itemToUpdate.HasLooping = model.ItemConfig.HasLooping;
      itemToUpdate.LoopCount = model.ItemConfig.LoopCount;
      itemToUpdate.LoopLabel = model.ItemConfig.LoopLabel;

      // Handle options for choice types
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

        // Redirect back to the Builder page after successful update
        // You still need FormTypeId to redirect correctly
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
          throw; // Re-throw other concurrency issues
        }
      }
      catch (Exception ex)
      {
        ModelState.AddModelError(string.Empty, $"An error occurred while saving: {ex.Message}");
      }

      // If something went wrong during save, or if ModelState was invalid initially, return the view
      // This will display validation messages if any.
      return View(model);
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
      var item = await _context.FormItems.FindAsync(id);
      if (item == null)
      {
        return NotFound();
      }

      _context.FormItems.Remove(item);
      await _context.SaveChangesAsync();
      return Ok(new { success = true, message = "Item deleted successfully." });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSection(int id)
    {
      var section = await _context.FormSections
                                      .Include(s => s.Items)
                                      .FirstOrDefaultAsync(s => s.SectionId == id);
      if (section == null)
      {
        return NotFound();
      }

      _context.FormItems.RemoveRange(section.Items);
      _context.FormSections.Remove(section);
      await _context.SaveChangesAsync();
      return Ok(new { success = true, message = "Section and its items deleted successfully." });
    }

    // Action for updating Form Status
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFormStatus(int formTypeId, FormStatus newStatus)
    {
      var form = await _context.JenisForms.FindAsync(formTypeId);
      if (form == null)
      {
        return Json(new { success = false, message = "Form not found." });
      }

      form.Status = newStatus;
      try
      {
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Form status updated successfully." });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = $"Error updating status: {ex.Message}" });
      }
    }

    // Modified Delete Action to archive the form instead of deleting
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
      var form = await _context.JenisForms.FindAsync(id);
      if (form == null)
      {
        return NotFound();
      }

      form.Status = FormStatus.Archived; // Set the status to Archived
      try
      {
        await _context.SaveChangesAsync();
        return RedirectToAction("Index"); // Redirect to the index page or wherever appropriate
      }
      catch (Exception)
      {
        // Handle any potential errors during the update
        return StatusCode(500, "An error occurred while archiving the form.");
      }
    }

    public async Task<IActionResult> Preview(int id)
    {
      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections)
              .ThenInclude(s => s.Items)
          .FirstOrDefaultAsync(f => f.FormTypeId == id);

      if (jenisForm == null)
        return NotFound();

      return View(jenisForm);
    }
  }
}
