using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models; // This using now provides ItemType, ItemOption, AuditResponse, etc.
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Models.ViewModels; // For ViewModels
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class AuditController : Controller
  {
    private readonly AppDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditController(AppDbContext context, ITenantService tenantService, UserManager<ApplicationUser> userManager)
    {
      _context = context;
      _tenantService = tenantService;
      _userManager = userManager;
    }

    public async Task<IActionResult> FollowUpAudit(int auditInstanceId)
    {
      var auditInstance = await _context.AuditInstances
          .Include(ai => ai.JenisForm)
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.FormItem) // No .ThenInclude(fi => fi.ItemType)
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.CorrectiveActions.Where(ca => ca.Status != CorrectiveActionStatus.Completed))
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId);

      if (auditInstance == null)
      {
        return NotFound();
      }

      var itemsNeedingFollowUp = auditInstance.AuditResponses
          .Where(ar => ar.ScoredValue.HasValue && ar.MaxPossibleScore.HasValue && ar.ScoredValue.Value < ar.MaxPossibleScore.Value)
          .ToList();

      var followUpViewModel = new FollowUpAuditViewModel
      {
        OriginalAuditInstanceId = auditInstance.AuditInstanceId,
        FormTypeId = auditInstance.FormTypeId,
        FormName = auditInstance.JenisForm.Name,
        FormDescription = auditInstance.JenisForm.Description,
        ItemsForFollowUp = itemsNeedingFollowUp.Select(ar => new FollowUpAuditItemDto
        {
          AuditResponseId = ar.AuditResponseId,
          FormItemId = ar.FormItemId, // AuditResponse.FormItemId is the FK to FormItem.ItemId
          FormItemQuestion = ar.FormItem.Question,
          CorrectiveActionNotes = ar.CorrectiveActions.OrderByDescending(ca => ca.CreatedDate).FirstOrDefault()?.CorrectiveActionNotes,
          MaxScore = ar.FormItem.MaxScore,
          ExistingScoredValue = ar.ScoredValue,
          ExistingResponseValue = ar.ResponseValue,
          HasLooping = ar.FormItem.HasLooping,
          LoopCount = ar.FormItem.LoopCount, // LoopCount is int?, assigning to int? is fine
          LoopLabel = ar.FormItem.LoopLabel,
          LoopIndex = ar.LoopIndex,
          FormItem = new FormItemViewModel
          {
            ItemId = ar.FormItem.ItemId, // FormItem's actual PK
            Question = ar.FormItem.Question,
            ItemType = ar.FormItem.ItemType, // Assigning enum directly
            ItemTypeName = ar.FormItem.ItemType.ToString(),
            MaxScore = ar.FormItem.MaxScore,
            OptionsJson = ar.FormItem.OptionsJson, // Corrected to OptionsJson
            HasLooping = ar.FormItem.HasLooping,
            LoopCount = ar.FormItem.LoopCount,
            LoopLabel = ar.FormItem.LoopLabel,
            IsRequired = ar.FormItem.IsRequired
          }
        }).ToList()
      };

      return View(followUpViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFollowUpAudit(FollowUpAuditSubmitDto submitDto)
    {
      if (!ModelState.IsValid)
      {
        var auditInstance = await _context.AuditInstances
            .Include(ai => ai.JenisForm)
            .Include(ai => ai.AuditResponses)
                .ThenInclude(ar => ar.FormItem)
            .Include(ai => ai.AuditResponses)
                .ThenInclude(ar => ar.CorrectiveActions.Where(ca => ca.Status != CorrectiveActionStatus.Completed))
            .FirstOrDefaultAsync(ai => ai.AuditInstanceId == submitDto.OriginalAuditInstanceId);

        if (auditInstance == null)
        {
          return NotFound("Original audit instance not found for validation error display.");
        }

        var itemsNeedingFollowUp = auditInstance.AuditResponses
            .Where(ar => ar.ScoredValue.HasValue && ar.MaxPossibleScore.HasValue && ar.ScoredValue.Value < ar.MaxPossibleScore.Value)
            .ToList();

        var followUpViewModel = new FollowUpAuditViewModel
        {
          OriginalAuditInstanceId = auditInstance.AuditInstanceId,
          FormTypeId = auditInstance.FormTypeId,
          FormName = auditInstance.JenisForm.Name,
          FormDescription = auditInstance.JenisForm.Description,
          ItemsForFollowUp = itemsNeedingFollowUp.Select(ar => new FollowUpAuditItemDto
          {
            AuditResponseId = ar.AuditResponseId,
            FormItemId = ar.FormItemId, // AuditResponse.FormItemId is the FK
            FormItemQuestion = ar.FormItem.Question,
            CorrectiveActionNotes = ar.CorrectiveActions.OrderByDescending(ca => ca.CreatedDate).FirstOrDefault()?.CorrectiveActionNotes,
            MaxScore = ar.FormItem.MaxScore,
            ExistingScoredValue = ar.ScoredValue,
            ExistingResponseValue = ar.ResponseValue,
            HasLooping = ar.FormItem.HasLooping,
            LoopCount = ar.FormItem.LoopCount,
            LoopLabel = ar.FormItem.LoopLabel,
            LoopIndex = ar.LoopIndex,
            FormItem = new FormItemViewModel
            {
              ItemId = ar.FormItem.ItemId, // FormItem's actual PK
              Question = ar.FormItem.Question,
              ItemType = ar.FormItem.ItemType,
              ItemTypeName = ar.FormItem.ItemType.ToString(),
              MaxScore = ar.FormItem.MaxScore,
              OptionsJson = ar.FormItem.OptionsJson, // Corrected
              HasLooping = ar.FormItem.HasLooping,
              LoopCount = ar.FormItem.LoopCount,
              LoopLabel = ar.FormItem.LoopLabel,
              IsRequired = ar.FormItem.IsRequired
            }
          }).ToList()
        };

        TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting.";
        return View("FollowUpAudit", followUpViewModel);
      }

      var newAuditInstance = new AuditInstance
      {
        FormTypeId = submitDto.FormTypeId,
        AuditDate = DateTime.Now,
        //AuditorId = _userManager.GetUserId(User),
        Status = AuditStatus.Completed,
        TenantId = _tenantService.GetCurrentTenantId(),
        FormName = (await _context.JenisForms.FindAsync(submitDto.FormTypeId))?.Name
      };
      _context.AuditInstances.Add(newAuditInstance);
      await _context.SaveChangesAsync();

      foreach (var submittedItem in submitDto.Items)
      {
        var formItem = await _context.FormItems
            .FirstOrDefaultAsync(fi => fi.ItemId == submittedItem.ItemId); // Using FormItem.ItemId

        if (formItem == null)
        {
          continue;
        }

        int? scoredValue = CalculateItemScore(formItem, submittedItem.ResponseValue);

        var newAuditResponse = new AuditResponse
        {
          AuditInstanceId = newAuditInstance.AuditInstanceId,
          FormItemId = submittedItem.ItemId, // Storing FormItem.ItemId as FK in AuditResponse
          ResponseValue = submittedItem.ResponseValue,
          ScoredValue = scoredValue, // Now nullable int?
          MaxPossibleScore = formItem.MaxScore,
          LoopIndex = submittedItem.LoopIndex,
          OriginalAuditResponseId = submittedItem.OriginalAuditResponseId
        };
        _context.AuditResponses.Add(newAuditResponse);

        var activeCorrectiveActions = await _context.CorrectiveActions
            .Where(ca => ca.AuditResponseId == submittedItem.OriginalAuditResponseId && ca.Status != CorrectiveActionStatus.Completed)
            .ToListAsync();

        foreach (var originalCorrectiveAction in activeCorrectiveActions)
        {
          if (scoredValue.HasValue && formItem.MaxScore.HasValue && scoredValue.Value >= formItem.MaxScore.Value)
          {
            originalCorrectiveAction.Status = CorrectiveActionStatus.Completed;
            originalCorrectiveAction.CompletionDate = DateTime.Now;
            originalCorrectiveAction.LastModifiedBy = User.Identity.Name;
            originalCorrectiveAction.LastModifiedDate = DateTime.Now;
            _context.CorrectiveActions.Update(originalCorrectiveAction);
          }
        }
      }

      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "Follow-up Audit submitted successfully!";
      return RedirectToAction("Details", "Audit", new { id = newAuditInstance.AuditInstanceId });
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
        //AuditorId = _userManager.GetUserId(User),
        AuditorName = User.Identity.Name ?? "Unknown Auditor",
        TenantId = currentTenantId,
        BranchName = branchName,
        Status = AuditStatus.Completed,
        AuditResponses = new List<AuditResponse>()
      };

      int totalScore = 0; // This is a non-nullable int
      int totalMaxScore = 0; // This is a non-nullable int

      foreach (var section in jenisForm.Sections.OrderBy(s => s.Order))
      {
        foreach (var item in section.Items.OrderBy(i => i.Order))
        {
          bool isScoreableItemType =
              item.ItemType.ToString() == "Number" ||
              item.ItemType.ToString() == "Checkbox" ||
              item.ItemType.ToString() == "Radio" || // Corrected to "Radio"
              item.ItemType.ToString() == "Dropdown";

          if (item.HasLooping && item.LoopCount.HasValue)
          {
            for (int i = 0; i < item.LoopCount.Value; i++)
            {
              var fieldName = $"{item.ItemId}_loop_{i}"; // Using item.ItemId
              var submittedValue = Request.Form[fieldName].ToString();

              int? itemScore = CalculateItemScore(item, submittedValue);
              totalScore += itemScore ?? 0; // Correctly handles int? to int conversion

              if (isScoreableItemType)
              {
                totalMaxScore += item.MaxScore ?? 0; // Correctly handles int? to int conversion
              }

              auditInstance.AuditResponses.Add(new AuditResponse
              {
                FormItemId = item.ItemId, // Correctly assigning FormItem.ItemId to FK
                FormItemQuestion = item.Question,
                ResponseValue = submittedValue,
                ScoredValue = itemScore, // Now nullable int?
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
            var fieldName = item.ItemId.ToString(); // Using item.ItemId
            var submittedValue = Request.Form[fieldName].ToString();

            int? itemScore = CalculateItemScore(item, submittedValue);
            totalScore += itemScore ?? 0; // Correctly handles int? to int conversion

            if (isScoreableItemType)
            {
              totalMaxScore += item.MaxScore ?? 0; // Correctly handles int? to int conversion
            }

            auditInstance.AuditResponses.Add(new AuditResponse
            {
              FormItemId = item.ItemId, // Correctly assigning FormItem.ItemId to FK
              FormItemQuestion = item.Question,
              ResponseValue = submittedValue,
              ScoredValue = itemScore, // Now nullable int?
              MaxPossibleScore = item.MaxScore,
            });

            if (item.IsRequired && isScoreableItemType)
            {
              if (string.IsNullOrWhiteSpace(submittedValue))
              {
                if (item.ItemType.ToString() == "Checkbox" || item.ItemType.ToString() == "Radio") // Corrected to "Radio"
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

    public int? CalculateItemScore(FormItem item, string? submittedValue) // submittedValue can be null or empty
    {
      int? score = null;

      if (item.ItemType == ItemType.Text || // Direct enum comparison is cleaner than ToString()
          item.ItemType == ItemType.Textarea || // Changed to Textarea
          item.ItemType == ItemType.File ||
          item.ItemType == ItemType.Signature)
      {
        return null;
      }

      if (!item.MaxScore.HasValue || item.MaxScore.Value <= 0)
      {
        return null;
      }

      switch (item.ItemType) // Direct enum switch
      {
        case ItemType.Number:
          if (int.TryParse(submittedValue, out int numValue))
          {
            score = Math.Min(numValue, item.MaxScore.Value);
            score = Math.Max(score.Value, 0);
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

        case ItemType.Radio: // Corrected to Radio
        case ItemType.Dropdown:
          if (!string.IsNullOrWhiteSpace(submittedValue) && !string.IsNullOrEmpty(item.OptionsJson))
          {
            try
            {
              var options = JsonSerializer.Deserialize<List<ItemOption>>(item.OptionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
              if (options != null)
              {
                var selectedOption = options.FirstOrDefault(o => o.Value != null && o.Value.Equals(submittedValue, StringComparison.OrdinalIgnoreCase));
                if (selectedOption != null && selectedOption.Score.HasValue)
                {
                  score = Math.Min(selectedOption.Score.Value, item.MaxScore.Value);
                  score = Math.Max(score.Value, 0);
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
          .Include(ai => ai.JenisForm)
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

    private CorrectiveActionStatus AuditStatusToCorrectiveActionStatus(AuditStatus auditStatus)
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
