
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;

namespace AspnetCoreMvcFull.Controllers
{
  public class FormBuilderController : Controller
  {
    private readonly AppDbContext _context;

    public FormBuilderController(AppDbContext context)
    {
      _context = context;
    }

    public async Task<IActionResult> Index()
    {
      var jenisForms = await _context.JenisForms
          .Include(f => f.Sections)
          .ThenInclude(s => s.Items)
          .ToListAsync();

      return View(jenisForms);
    }

    [HttpGet]
    public IActionResult CreateForm()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateForm(CreateJenisFormViewModel model)
    {
      if (ModelState.IsValid)
      {
        var jenisForm = new JenisForm
        {
          Name = model.Name,
          Description = model.Description,
          CreatedAt = DateTime.Now
        };

        _context.JenisForms.Add(jenisForm);
        await _context.SaveChangesAsync();

        return RedirectToAction("Builder", new { id = jenisForm.FormTypeId });
      }

      return View(model);
    }

    public async Task<IActionResult> Builder(int id)
    {
      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections)
          .ThenInclude(s => s.Items)
          .FirstOrDefaultAsync(f => f.FormTypeId == id);

      if (jenisForm == null)
        return NotFound();

      var viewModel = new FormBuilderViewModel
      {
        JenisForm = jenisForm,
        Sections = jenisForm.Sections.OrderBy(s => s.Order).ToList()
      };

      return View(viewModel);
    }

    [HttpPost]
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
      }

      return RedirectToAction("Builder", new { id = model.FormTypeId });
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(int sectionId, ItemType itemType)
    {
      var section = await _context.FormSections.FindAsync(sectionId);
      if (section == null)
        return NotFound();

      var maxOrder = await _context.FormItems
          .Where(i => i.SectionId == sectionId)
          .MaxAsync(i => (int?)i.Order) ?? 0;

      var item = new FormItem
      {
        SectionId = sectionId,
        Question = $"New {itemType} Question",
        ItemType = itemType,
        IsRequired = false,
        Order = maxOrder + 1
      };

      // Set default options for choice-based items
      if (itemType == ItemType.Radio || itemType == ItemType.Checkbox || itemType == ItemType.Dropdown)
      {
        var defaultOptions = new List<string> { "Option 1", "Option 2" };
        item.OptionsJson = JsonConvert.SerializeObject(defaultOptions);
      }

      _context.FormItems.Add(item);
      await _context.SaveChangesAsync();

      return RedirectToAction("Builder", new { id = section.FormTypeId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItem(ItemConfigViewModel model)
    {
      var item = await _context.FormItems.FindAsync(model.ItemId);
      if (item == null)
        return NotFound();

      item.Question = model.Question;
      item.IsRequired = model.IsRequired;
      item.MaxScore = model.MaxScore;
      item.HasLooping = model.HasLooping;
      item.LoopCount = model.HasLooping ? model.LoopCount : null;
      item.LoopLabel = model.HasLooping ? model.LoopLabel : null;

      // Update options for choice-based items
      if (item.ItemType == ItemType.Radio || item.ItemType == ItemType.Checkbox || item.ItemType == ItemType.Dropdown)
      {
        item.OptionsJson = JsonConvert.SerializeObject(model.Options);
      }

      await _context.SaveChangesAsync();
      return Json(new { success = true });
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
