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

        public async Task<IActionResult> Index(int? complianceCategoryId, FormStatus? formStatus, string sortOrder) // Added sortOrder parameter
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
            else
            {
                // Default to showing only non-archived forms if no status filter is applied
                jenisFormsQuery = jenisFormsQuery.Where(f => f.Status != FormStatus.Archived);
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
        public async Task<IActionResult> ConfigureItem(ConfigureItemPageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var itemToUpdate = await _context.FormItems.FindAsync(model.ItemConfig.ItemId);
            if (itemToUpdate == null)
            {
                return NotFound("Item not found for update.");
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

        // Action for updating Form Status (used for archiving/restoring and submitting in Preview)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFormStatus(int formTypeId, FormStatus newStatus)
        {
            var form = await _context.JenisForms.FindAsync(formTypeId);
            if (form == null)
            {
                // If form not found, redirect to Index. You might use TempData for a notification.
                return RedirectToAction("Index");
            }

            form.Status = newStatus;
            try
            {
                await _context.SaveChangesAsync();
                // Redirect back to Index after successful update
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes.
                Console.Error.WriteLine($"Error updating status: {ex.Message}");
                // Redirect back to Index in case of an error.
                // You might use TempData["ErrorMessage"] = "Error updating status.";
                return RedirectToAction("Index");
            }
        }

        // Action to completely delete a form (now handled by direct HTML form submission)
        [HttpPost] // <-- THIS IS THE CORRECTION: Changed from [HttpDelete] to [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFormCompletely(int id)
        {
            var form = await _context.JenisForms
                                    .Include(f => f.Sections)
                                        .ThenInclude(s => s.Items)
                                    .FirstOrDefaultAsync(f => f.FormTypeId == id);

            if (form == null)
            {
                return NotFound();
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
                return RedirectToAction("Index"); // Redirect to the index page after complete deletion
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using a logging framework)
                Console.Error.WriteLine($"Error deleting form completely: {ex.Message}");
                // For an HTML form submission, setting TempData for a message is better
                TempData["ErrorMessage"] = "An error occurred while completely deleting the form.";
                return RedirectToAction("Index"); // Redirect back to Index on error
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