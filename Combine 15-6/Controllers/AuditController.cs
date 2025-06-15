using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models; // Provides ItemType, ItemOption, AuditResponse, JenisForm, FormSection, FormItem etc.
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Models.ViewModels; // For all your ViewModels and DTOs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For Include, FirstOrDefaultAsync, ToListAsync
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ methods like Any(), SelectMany(), OrderBy(), ToList()
using System.Text.Json; // For JsonSerializer
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class AuditController : Controller
  {
    private readonly AppDbContext _context; // Using AppDbContext as per your provided controller
    private readonly ITenantService _tenantService;
    private readonly UserManager<ApplicationUser> _userManager;

    // Static options for JsonSerializer to avoid re-creating it
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true // Ensures matching properties regardless of casing
    };

    public AuditController(AppDbContext context, ITenantService tenantService, UserManager<ApplicationUser> userManager)
    {
      _context = context;
      _tenantService = tenantService;
      _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
      ViewBag.IsUser = User.IsInRole("User");
      var currentTenantId = _tenantService.GetCurrentTenantId();

      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot display audit instances: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstances = await _context.AuditInstances
                                      .Where(ai => ai.TenantId == currentTenantId)
                                      .OrderByDescending(ai => ai.AuditDate)
                                      .ToListAsync();

      var availableJenisForms = await _context.JenisForms
                                              .Where(jf => jf.TenantId == currentTenantId)
                                              .OrderBy(jf => jf.Name)
                                              .ToListAsync();

      ViewData["AvailableForms"] = availableJenisForms;

      return View(auditInstances);
    }

    public async Task<IActionResult> ListFormTemplates()
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot display form templates: Current tenant not identified.";
        return Unauthorized();
      }

      var availableJenisForms = await _context.JenisForms
                                              .Where(jf => jf.TenantId == currentTenantId)
                                              .OrderBy(jf => jf.Name)
                                              .ToListAsync();

      return View(availableJenisForms);
    }

    public async Task<IActionResult> DisplayForm(int id)
    {
      bool isUser = User.IsInRole("User");

      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot display form: Current tenant not identified.";
        return Unauthorized();
      }

      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections.OrderBy(s => s.Order))
              .ThenInclude(s => s.Items.OrderBy(item => item.Order))
          .FirstOrDefaultAsync(f => f.FormTypeId == id && f.TenantId == currentTenantId);

      if (jenisForm == null)
      {
        TempData["ErrorMessage"] = "Audit Form not found or does not belong to your organization.";
        return NotFound();
      }

      ViewBag.IsUser = isUser;
      return View("~/Views/Audit/AuditForm.cshtml", jenisForm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAudit(int formTypeId)
    {
      bool isUser = User.IsInRole("User");
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot submit audit: Current tenant not identified.";
        return Unauthorized();
      }

      var jenisForm = await _context.JenisForms
          .Include(f => f.Sections)
              .ThenInclude(s => s.Items)
          .FirstOrDefaultAsync(f => f.FormTypeId == formTypeId && f.TenantId == currentTenantId);

      if (jenisForm == null)
      {
        TempData["ErrorMessage"] = "Submitted form template not found or does not belong to your organization.";
        return NotFound();
      }

      var branchName = Request.Form["BranchName"].ToString();

      var auditInstance = new AuditInstance
      {
        FormTypeId = jenisForm.FormTypeId,
        FormName = jenisForm.Name,
        AuditDate = DateTime.Now,
        AuditorName = User.Identity.Name ?? "Unknown Auditor",
        TenantId = currentTenantId,
        BranchName = branchName,
        Status = AuditStatus.Completed, // Default status for initial submission
        AuditResponses = new List<AuditResponse>()
      };

      int totalScore = 0;
      int totalMaxScore = 0;

      foreach (var section in jenisForm.Sections.OrderBy(s => s.Order))
      {
        foreach (var item in section.Items.OrderBy(i => i.Order))
        {
          bool isScoreableItemType =
              item.ItemType == ItemType.Number ||
              item.ItemType == ItemType.Checkbox ||
              item.ItemType == ItemType.Radio ||
              item.ItemType == ItemType.Dropdown;

          if (item.HasLooping && item.LoopCount.HasValue)
          {
            for (int i = 0; i < item.LoopCount.Value; i++)
            {
              var fieldName = $"{item.ItemId}_loop_{i}";
              var submittedValue = Request.Form[fieldName].ToString();

              int? itemScore = CalculateItemScore(item, submittedValue);
              totalScore += itemScore ?? 0;

              if (isScoreableItemType)
              {
                totalMaxScore += item.MaxScore ?? 0;
              }

              auditInstance.AuditResponses.Add(new AuditResponse
              {
                FormItemId = item.ItemId,
                FormItemQuestion = item.Question,
                ResponseValue = submittedValue,
                ScoredValue = itemScore,
                MaxPossibleScore = item.MaxScore,
                LoopIndex = i,
              });

              if (item.IsRequired && isScoreableItemType)
              {
                if (string.IsNullOrWhiteSpace(submittedValue))
                {
                  ModelState.AddModelError(fieldName, $"The '{item.Question} - {item.LoopLabel} {i + 1}' field is required.");
                }
              }
            }
          }
          else
          {
            var fieldName = item.ItemId.ToString();
            var submittedValue = Request.Form[fieldName].ToString();

            int? itemScore = CalculateItemScore(item, submittedValue);
            totalScore += itemScore ?? 0;

            if (isScoreableItemType)
            {
              totalMaxScore += item.MaxScore ?? 0;
            }

            auditInstance.AuditResponses.Add(new AuditResponse
            {
              FormItemId = item.ItemId,
              FormItemQuestion = item.Question,
              ResponseValue = submittedValue,
              ScoredValue = itemScore,
              MaxPossibleScore = item.MaxScore,
            });

            if (item.IsRequired && isScoreableItemType)
            {
              if (string.IsNullOrWhiteSpace(submittedValue))
              {
                if (item.ItemType == ItemType.Checkbox || item.ItemType == ItemType.Radio)
                {
                  var selectedOptions = Request.Form[fieldName];
                  if (selectedOptions.Count == 0 || string.IsNullOrEmpty(selectedOptions.ToString()))
                  {
                    ModelState.AddModelError(fieldName, $"At least one option for '{item.Question}' is required.");
                  }
                }
                else
                {
                  ModelState.AddModelError(fieldName, $"The '{item.Question}' field is required.");
                }
              }
            }
          }
        }
      }

      if (!ModelState.IsValid)
      {
        TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting.";
        ViewBag.IsUser = isUser;
        var reloadedJenisForm = await _context.JenisForms
            .Include(f => f.Sections)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(f => f.FormTypeId == formTypeId && f.TenantId == currentTenantId);
        return View("~/Views/Audit/AuditForm.cshtml", reloadedJenisForm);
      }

      auditInstance.TotalScore = totalScore;
      auditInstance.TotalMaxScore = totalMaxScore;
      auditInstance.PercentageScore = totalMaxScore > 0 ? (double)totalScore / totalMaxScore * 100 : 0;

      _context.AuditInstances.Add(auditInstance);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "Audit Form submitted successfully!";
      return RedirectToAction("AuditConfirmation", new { auditId = auditInstance.AuditInstanceId });
    }

    // GET: Audit/FollowUpAudit/{auditInstanceId}
    public async Task<IActionResult> FollowUpAudit(int auditInstanceId)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot access follow-up audit: Current tenant not identified.";
        return Unauthorized();
      }

      // 1. Fetch the previous AuditInstance with its related data
      var previousAuditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses) // Include responses from the previous audit
              .ThenInclude(ar => ar.FormItem) // Include the FormItem (template) for each response
                  .ThenInclude(fi => fi.Section) // Include the Section for each FormItem
          .Include(ai => ai.JenisForm) // Include the overall JenisForm details (for FormName, Description)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId && ai.TenantId == currentTenantId);

      if (previousAuditInstance == null)
      {
        TempData["ErrorMessage"] = "Previous audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      // 2. Initialize the FollowUpAuditViewModel
      var viewModel = new FollowUpAuditViewModel
      {
        OriginalAuditInstanceId = previousAuditInstance.AuditInstanceId,
        FormTypeId = previousAuditInstance.FormTypeId,
        FormName = previousAuditInstance.JenisForm?.Name ?? previousAuditInstance.FormName,
        FormDescription = previousAuditInstance.JenisForm?.Description
      };

      // 3. Populate Sections and Items for Follow-up
      //    Get all sections from the original form template, ordered by their 'Order'.
      var originalSections = await _context.FormSections
          .Where(fs => fs.FormTypeId == previousAuditInstance.FormTypeId)
          .OrderBy(fs => fs.Order)
          .Include(fs => fs.Items) // Include original FormItems (templates)
          .ToListAsync();

      foreach (var originalSection in originalSections)
      {
        var followUpSectionDto = new FollowUpAuditSectionDto
        {
          SectionId = originalSection.SectionId,
          Title = originalSection.Title,
          Description = originalSection.Description,
          Order = originalSection.Order
        };

        // Iterate through the original FormItems within this section
        foreach (var originalFormItem in originalSection.Items.OrderBy(fi => fi.Order))
        {
          // Find the corresponding AuditResponse(s) from the last audit instance for this specific FormItem
          // This handles both non-looping and looping items correctly
          var previousResponsesForThisItem = previousAuditInstance.AuditResponses
              .Where(ar => ar.FormItemId == originalFormItem.ItemId)
              .ToList();

          foreach (var previousResponse in previousResponsesForThisItem)
          {
            // Only add to the follow-up list if the previous score was NOT full
            if (previousResponse.ScoredValue < previousResponse.MaxPossibleScore)
            {
              var followUpItemDto = new FollowUpAuditItemDto
              {
                AuditResponseId = previousResponse.AuditResponseId,
                FormItemId = previousResponse.FormItemId,
                FormItemQuestion = previousResponse.FormItemQuestion,
                MaxScore = previousResponse.MaxPossibleScore, // Max score for this *specific response* instance
                ExistingScoredValue = previousResponse.ScoredValue,
                ExistingResponseValue = previousResponse.ResponseValue,
                HasLooping = originalFormItem.HasLooping, // Get looping info from original template
                LoopCount = originalFormItem.LoopCount,
                LoopLabel = originalFormItem.LoopLabel,
                LoopIndex = previousResponse.LoopIndex, // Preserve original loop index if it was a looped item response
                CorrectiveActionNotes = null, // Or fetch existing corrective action notes if applicable

                // Map the original FormItem template properties to FormItemViewModel
                FormItem = new FormItemViewModel
                {
                  ItemId = originalFormItem.ItemId,
                  Question = originalFormItem.Question,
                  ItemType = originalFormItem.ItemType,
                  ItemTypeName = originalFormItem.ItemType.ToString(), // Convert enum to string
                  MaxScore = originalFormItem.MaxScore, // Max score from the template
                  OptionsJson = originalFormItem.OptionsJson,
                  HasLooping = originalFormItem.HasLooping,
                  LoopCount = originalFormItem.LoopCount,
                  LoopLabel = originalFormItem.LoopLabel,
                  IsRequired = originalFormItem.IsRequired,
                  SectionId = originalFormItem.SectionId, // Populate the SectionId
                  Order = originalFormItem.Order // Populate the Order property
                }
              };
              followUpSectionDto.Items.Add(followUpItemDto);
            }
          }
        }
        // Only add the section to the ViewModel if it contains items that need follow-up
        if (followUpSectionDto.Items.Count > 0)
        {
          viewModel.Sections.Add(followUpSectionDto);
        }
      }

      // If no items need follow-up (all were 100%), you might redirect or show a specific message in the view
      if (viewModel.Sections.Count == 0 || viewModel.Sections.All(s => s.Items.All(item => (item.ExistingScoredValue ?? 0) >= (item.MaxScore ?? 0))))
      {
        TempData["SuccessMessage"] = "This audit instance has no items requiring follow-up. All previous items were fully scored!";
        // You could redirect to the audit details page or index if you prefer
        return RedirectToAction(nameof(Details), new { id = auditInstanceId });
      }

      return View(viewModel);
    }

    // POST: Audit/SubmitFollowUpAudit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFollowUpAudit(FollowUpAuditSubmitDto submitDto)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot submit follow-up audit: Current tenant not identified.";
        return Unauthorized();
      }

      // 1. Fetch the original AuditInstance and its responses for update
      var originalAudit = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == submitDto.OriginalAuditInstanceId && ai.TenantId == currentTenantId);

      if (originalAudit == null)
      {
        return NotFound();
      }

      // Ensure ModelState is valid BEFORE processing, otherwise repopulate and return view
      if (!ModelState.IsValid)
      {
        TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting the follow-up.";
        // Re-populate the ViewModel for the view to display existing data and validation errors
        return await RebuildFollowUpAuditViewModelForView(submitDto.OriginalAuditInstanceId, currentTenantId);
      }

      // 2. Iterate through the submitted items from the DTO and update corresponding AuditResponses
      foreach (var submittedItem in submitDto.Items)
      {
        var auditResponseToUpdate = originalAudit.AuditResponses
            .FirstOrDefault(ar => ar.AuditResponseId == submittedItem.OriginalAuditResponseId);

        if (auditResponseToUpdate != null)
        {
          // Update the response value with the new submission
          auditResponseToUpdate.ResponseValue = submittedItem.ResponseValue;

          // Recalculate score for this item based on the new response value and its FormItem template
          var formItemTemplate = await _context.FormItems.FindAsync(submittedItem.ItemId);
          if (formItemTemplate != null)
          {
            auditResponseToUpdate.ScoredValue = CalculateItemScore(formItemTemplate, submittedItem.ResponseValue);
          }

          // Update metadata for the audit response itself (optional)
          // auditResponseToUpdate.LastModifiedBy = User.Identity.Name;
          // auditResponseToUpdate.LastModifiedDate = DateTime.UtcNow;
        }
      }

      // 3. Recalculate overall audit score for the instance based on ALL responses (updated and unchanged)
      originalAudit.TotalScore = originalAudit.AuditResponses.Sum(ar => ar.ScoredValue ?? 0);
      originalAudit.TotalMaxScore = originalAudit.AuditResponses.Sum(ar => ar.MaxPossibleScore ?? 0); // MaxPossibleScore from responses

      // If you need MaxScore from FormItems, you'd fetch them here and sum
      // originalAudit.TotalMaxScore = originalAudit.AuditResponses.Sum(ar => ar.FormItem?.MaxScore ?? 0); // If MaxScore is from FormItem

      originalAudit.PercentageScore = originalAudit.TotalMaxScore > 0
          ? (double)originalAudit.TotalScore / originalAudit.TotalMaxScore * 100
          : 0;

      // 4. Update the AuditInstance status
      // Check if *all* audit responses now have full score compared to their MaxPossibleScore
      bool allItemsFullyScored = originalAudit.AuditResponses.All(ar => ar.ScoredValue.HasValue && ar.ScoredValue.Value >= ar.MaxPossibleScore);

      if (allItemsFullyScored)
      {
        originalAudit.Status = AuditStatus.Completed; // All items in the original audit are now fully scored
      }
      else
      {
        // If some items still not fully scored, perhaps it goes back to NeedsFollowUp
        originalAudit.Status = AuditStatus.NeedsFollowUp;
      }

      // 5. Save changes to the database
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "Follow Up Audit submitted successfully and audit instance updated!";
      // 6. Redirect to Audit Instances list or details page
      return RedirectToAction(nameof(Details), new { id = originalAudit.AuditInstanceId });
    }


    // Helper method to reconstruct the ViewModel for validation errors or initial GET
    private async Task<IActionResult> RebuildFollowUpAuditViewModelForView(int auditInstanceId, string currentTenantId)
    {
      var previousAuditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.FormItem)
                  .ThenInclude(fi => fi.Section)
          .Include(ai => ai.JenisForm)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId && ai.TenantId == currentTenantId);

      if (previousAuditInstance == null)
      {
        return NotFound();
      }

      var viewModel = new FollowUpAuditViewModel
      {
        OriginalAuditInstanceId = previousAuditInstance.AuditInstanceId,
        FormTypeId = previousAuditInstance.FormTypeId,
        FormName = previousAuditInstance.JenisForm?.Name ?? previousAuditInstance.FormName,
        FormDescription = previousAuditInstance.JenisForm?.Description
      };

      var originalSections = await _context.FormSections
          .Where(fs => fs.FormTypeId == previousAuditInstance.FormTypeId)
          .OrderBy(fs => fs.Order)
          .Include(fs => fs.Items)
          .ToListAsync();

      foreach (var originalSection in originalSections)
      {
        var followUpSectionDto = new FollowUpAuditSectionDto
        {
          SectionId = originalSection.SectionId,
          Title = originalSection.Title,
          Description = originalSection.Description,
          Order = originalSection.Order
        };

        foreach (var originalFormItem in originalSection.Items.OrderBy(fi => fi.Order))
        {
          var previousResponsesForThisItem = previousAuditInstance.AuditResponses
              .Where(ar => ar.FormItemId == originalFormItem.ItemId)
              .ToList();

          foreach (var previousResponse in previousResponsesForThisItem)
          {
            if (previousResponse.ScoredValue < previousResponse.MaxPossibleScore)
            {
              var followUpItemDto = new FollowUpAuditItemDto
              {
                AuditResponseId = previousResponse.AuditResponseId,
                FormItemId = previousResponse.FormItemId,
                FormItemQuestion = previousResponse.FormItemQuestion,
                MaxScore = previousResponse.MaxPossibleScore,
                ExistingScoredValue = previousResponse.ScoredValue,
                ExistingResponseValue = previousResponse.ResponseValue,
                HasLooping = originalFormItem.HasLooping,
                LoopCount = originalFormItem.LoopCount,
                LoopLabel = originalFormItem.LoopLabel,
                LoopIndex = previousResponse.LoopIndex,
                CorrectiveActionNotes = null, // Or fetch existing corrective action notes if applicable
                FormItem = new FormItemViewModel
                {
                  ItemId = originalFormItem.ItemId,
                  Question = originalFormItem.Question,
                  ItemType = originalFormItem.ItemType,
                  ItemTypeName = originalFormItem.ItemType.ToString(),
                  MaxScore = originalFormItem.MaxScore,
                  OptionsJson = originalFormItem.OptionsJson,
                  HasLooping = originalFormItem.HasLooping,
                  LoopCount = originalFormItem.LoopCount,
                  LoopLabel = originalFormItem.LoopLabel,
                  IsRequired = originalFormItem.IsRequired,
                  SectionId = originalFormItem.SectionId,
                  Order = originalFormItem.Order
                }
              };
              followUpSectionDto.Items.Add(followUpItemDto);
            }
          }
        }
        if (followUpSectionDto.Items.Count > 0)
        {
          viewModel.Sections.Add(followUpSectionDto);
        }
      }
      return View("~/Views/Audit/FollowUpAudit.cshtml", viewModel);
    }


    // Helper method to calculate score for an individual item (reused from initial audit logic)
    public int? CalculateItemScore(FormItem item, string? submittedValue) // submittedValue can be null or empty
    {
      int? score = null;

      if (item.ItemType == ItemType.Text ||
          item.ItemType == ItemType.Textarea ||
          item.ItemType == ItemType.File ||
          item.ItemType == ItemType.Signature)
      {
        // For non-scoreable types, return null score
        // Or if you want to assign MaxScore if value is present:
        // return string.IsNullOrWhiteSpace(submittedValue) ? 0 : item.MaxScore;
        return null;
      }

      if (!item.MaxScore.HasValue || item.MaxScore.Value <= 0)
      {
        return null; // Item is not configured for scoring
      }

      switch (item.ItemType)
      {
        case ItemType.Number:
          if (int.TryParse(submittedValue, out int numValue))
          {
            score = Math.Min(numValue, item.MaxScore.Value);
            score = Math.Max(score.Value, 0); // Ensure score is not negative
          }
          else
          {
            score = 0; // No valid number, score is 0
          }
          break;

        case ItemType.Checkbox:
          if (submittedValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
              submittedValue?.Equals("on", StringComparison.OrdinalIgnoreCase) == true)
          {
            score = item.MaxScore.Value;
          }
          else
          {
            score = 0; // Checkbox not checked, score is 0
          }
          break;

        case ItemType.Radio:
        case ItemType.Dropdown:
          if (!string.IsNullOrWhiteSpace(submittedValue) && !string.IsNullOrEmpty(item.OptionsJson))
          {
            try
            {
              var options = JsonSerializer.Deserialize<List<ItemOption>>(item.OptionsJson, _jsonSerializerOptions); // Use static options
              if (options != null)
              {
                var selectedOption = options.FirstOrDefault(o => o.Value != null && o.Value.Equals(submittedValue, StringComparison.OrdinalIgnoreCase));
                if (selectedOption != null && selectedOption.Score.HasValue)
                {
                  score = Math.Min(selectedOption.Score.Value, item.MaxScore.Value);
                  score = Math.Max(score.Value, 0); // Ensure score is not negative
                }
                else
                {
                  score = 0; // Selected option has no score or doesn't match
                }
              }
              else
              {
                score = 0; // No options deserialized
              }
            }
            catch (JsonException ex)
            {
              Console.WriteLine($"Error deserializing OptionsJson for item {item.ItemId}: {ex.Message}");
              score = 0; // Error in JSON, score is 0
            }
          }
          else
          {
            score = 0; // No value submitted or no options defined, score is 0
          }
          break;

        default:
          score = null; // Default case for non-scoreable types
          break;
      }

      return score;
    }

    public IActionResult AuditConfirmation(int auditId)
    {
      ViewBag.AuditId = auditId;
      return View();
    }

    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot view details: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.FormItem)
          // Include the full form structure to group responses by section
          .Include(ai => ai.JenisForm)
              .ThenInclude(jf => jf.Sections.OrderBy(s => s.Order))
                  .ThenInclude(s => s.Items.OrderBy(item => item.Order)) // Include items within sections
          .FirstOrDefaultAsync(m => m.AuditInstanceId == id && m.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        TempData["ErrorMessage"] = "Audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      return View(auditInstance);
    }

    public async Task<IActionResult> AddCorrectiveActions(int auditInstanceId)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot access corrective actions: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.FormItem)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId && ai.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        return NotFound();
      }

      var itemsNeedingCorrection = auditInstance.AuditResponses
          .Where(r => r.MaxPossibleScore.HasValue && r.ScoredValue.HasValue && r.ScoredValue.Value < r.MaxPossibleScore.Value)
          .OrderBy(r => r.FormItemId)
          .ThenBy(r => r.LoopIndex)
          .ToList();

      ViewBag.ItemsNeedingCorrection = itemsNeedingCorrection;
      ViewBag.AuditInstanceId = auditInstanceId;
      ViewBag.FormName = auditInstance.FormName;

      var existingCorrectiveActions = await _context.CorrectiveActions
          .Where(ca => ca.AuditInstanceId == auditInstanceId)
          .ToListAsync();
      ViewBag.ExistingCorrectiveActions = existingCorrectiveActions;

      return View(auditInstance);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCorrectiveActions(int auditInstanceId, [FromForm] string jsonData)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot save corrective actions: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId && ai.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        return NotFound();
      }

      var caData = JsonSerializer.Deserialize<List<CorrectiveActionSubmitDto>>(jsonData);

      if (caData != null)
      {
        foreach (var item in caData)
        {
          if (!string.IsNullOrWhiteSpace(item.CorrectiveActionNotes))
          {
            var existingCa = await _context.CorrectiveActions
                .FirstOrDefaultAsync(ca => ca.AuditResponseId == item.AuditResponseId && ca.AuditInstanceId == auditInstanceId);

            if (existingCa == null)
            {
              var newCa = new CorrectiveAction
              {
                AuditInstanceId = auditInstanceId,
                AuditResponseId = item.AuditResponseId,
                CorrectiveActionNotes = item.CorrectiveActionNotes,
                DueDate = item.DueDate,
                Status = CorrectiveActionStatus.Pending,
                CreatedBy = User.Identity.Name,
                CreatedDate = DateTime.Now
              };
              _context.CorrectiveActions.Add(newCa);
            }
            else
            {
              existingCa.CorrectiveActionNotes = item.CorrectiveActionNotes;
              existingCa.DueDate = item.DueDate;
              existingCa.LastModifiedBy = User.Identity.Name;
              existingCa.LastModifiedDate = DateTime.Now;
              _context.CorrectiveActions.Update(existingCa);
            }
          }
        }
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Corrective actions saved successfully!";
      }
      else
      {
        TempData["ErrorMessage"] = "No corrective actions data received.";
      }

      return RedirectToAction("Details", new { id = auditInstanceId });
    }

    private static CorrectiveActionStatus AuditStatusToCorrectiveActionStatus(AuditStatus auditStatus)
    {
      if (auditStatus == AuditStatus.NeedsCorrectiveAction) return CorrectiveActionStatus.Pending;
      return CorrectiveActionStatus.Pending;
    }

    public class CorrectiveActionSubmitDto
    {
      public int AuditResponseId { get; set; }
      public string? CorrectiveActionNotes { get; set; }
      public DateTime DueDate { get; set; }
    }
  }
}
