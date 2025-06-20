using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives; // Required for StringValues

namespace AspnetCoreMvcFull.Controllers
{
  public class AuditController : Controller
  {
    private readonly AppDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
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
                                      .Where(ai => ai.TenantId == currentTenantId && !ai.IsArchived)
                                      .OrderByDescending(ai => ai.AuditDate)
                                      .ToListAsync();

      var availableJenisForms = await _context.JenisForms
                                              .Where(jf => jf.TenantId == currentTenantId)
                                              .OrderBy(jf => jf.Name)
                                              .ToListAsync();

      ViewData["AvailableForms"] = availableJenisForms;

      return View(auditInstances);
    }

    public async Task<IActionResult> Archived()
    {
      ViewBag.IsUser = User.IsInRole("User");
      var currentTenantId = _tenantService.GetCurrentTenantId();

      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot display archived audit instances: Current tenant not identified.";
        return Unauthorized();
      }

      var archivedAuditInstances = await _context.AuditInstances
                                      .Where(ai => ai.TenantId == currentTenantId && ai.IsArchived)
                                      .OrderByDescending(ai => ai.AuditDate)
                                      .ToListAsync();

      return View(archivedAuditInstances);
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
                                              .Where(jf => jf.TenantId == currentTenantId && (jf.Status == FormStatus.Published || jf.Status == FormStatus.Revised))
                                              .OrderBy(jf => jf.Name)
                                              .ToListAsync();

      return View(availableJenisForms);
    }

    public async Task<IActionResult> DisplayForm(int id, int? auditInstanceId)
    {
      bool isUser = User.IsInRole("User");

      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot display form: Current tenant not identified.";
        return Unauthorized();
      }

      JenisForm jenisForm;
      AuditInstance existingAuditInstance = null;

      if (auditInstanceId.HasValue)
      {
        existingAuditInstance = await _context.AuditInstances
            .Include(ai => ai.AuditResponses)
            .Include(ai => ai.JenisForm)
                .ThenInclude(jf => jf.Sections.OrderBy(s => s.Order))
                    .ThenInclude(s => s.Items.OrderBy(item => item.Order))
            .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId.Value && ai.TenantId == currentTenantId);

        if (existingAuditInstance != null)
        {
          jenisForm = existingAuditInstance.JenisForm;
          ViewBag.ExistingAuditResponses = existingAuditInstance.AuditResponses.ToDictionary(r => $"{r.FormItemId}{(r.LoopIndex.HasValue ? "_loop_" + r.LoopIndex.Value : "")}", r => r.ResponseValue);
          ViewBag.IsDraft = (existingAuditInstance.Status == AuditStatus.Draft);
          ViewBag.IsEditingExistingAudit = true;
        }
        else
        {
          TempData["ErrorMessage"] = "Audit instance not found. Starting a new form.";
          jenisForm = await _context.JenisForms
              .Include(f => f.Sections.OrderBy(s => s.Order))
                  .ThenInclude(s => s.Items.OrderBy(item => item.Order))
              .FirstOrDefaultAsync(f => f.FormTypeId == id && f.TenantId == currentTenantId);
        }
      }
      else
      {
        jenisForm = await _context.JenisForms
            .Include(f => f.Sections.OrderBy(s => s.Order))
                .ThenInclude(s => s.Items.OrderBy(item => item.Order))
            .FirstOrDefaultAsync(f => f.FormTypeId == id && f.TenantId == currentTenantId);
      }

      if (jenisForm == null)
      {
        TempData["ErrorMessage"] = "Audit Form not found or does not belong to your organization.";
        return NotFound();
      }

      ViewBag.AuditInstanceId = existingAuditInstance?.AuditInstanceId;

      ViewBag.IsUser = isUser;
      return View("~/Views/Audit/AuditForm.cshtml", jenisForm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAudit(int formTypeId, string status, int? auditInstanceId)
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

      AuditInstance auditInstance;
      bool isNewAuditCreation = !auditInstanceId.HasValue;
      AuditStatus originalStatus = AuditStatus.Draft; // This is the status of the audit being submitted/updated

      if (auditInstanceId.HasValue)
      {
        auditInstance = await _context.AuditInstances
            .Include(ai => ai.AuditResponses)
            .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId.Value && ai.TenantId == currentTenantId);

        if (auditInstance == null)
        {
          TempData["ErrorMessage"] = "Cannot update audit: instance not found.";
          return NotFound();
        }
        originalStatus = auditInstance.Status;

        // Clear existing responses for update scenario
        _context.AuditResponses.RemoveRange(auditInstance.AuditResponses);
        auditInstance.AuditResponses.Clear();
      }
      else
      {
        // Create a new audit instance for initial submission
        auditInstance = new AuditInstance
        {
          FormTypeId = jenisForm.FormTypeId,
          FormName = jenisForm.Name,
          AuditorName = User.Identity.Name ?? "Unknown Auditor",
          TenantId = currentTenantId,
          AuditResponses = new List<AuditResponse>()
        };
        _context.AuditInstances.Add(auditInstance);
      }

      auditInstance.AuditDate = DateTime.Now;

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
              StringValues submittedValues = Request.Form[fieldName]; // Corrected
              var submittedValueCombined = string.Join(",", submittedValues.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)));

              int? itemScore = CalculateItemScore(item, submittedValueCombined);
              totalScore += itemScore ?? 0;

              if (isScoreableItemType)
              {
                totalMaxScore += item.MaxScore ?? 0;
              }

              var newAuditResponse = new AuditResponse
              {
                FormItemId = item.ItemId,
                FormItemQuestion = item.Question,
                ResponseValue = submittedValueCombined,
                ScoredValue = itemScore,
                MaxPossibleScore = item.MaxScore,
                LoopIndex = i,
              };
              auditInstance.AuditResponses.Add(newAuditResponse);

              if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && item.IsRequired && string.IsNullOrWhiteSpace(submittedValueCombined))
              {
                ModelState.AddModelError(fieldName, $"The '{item.Question} - {item.LoopLabel} {i + 1}' field is required.");
              }
              else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && isScoreableItemType && (itemScore < item.MaxScore || !itemScore.HasValue))
              {
                // Create CorrectiveAction if item is scoreable and not fully scored
                _context.CorrectiveActions.Add(new CorrectiveAction
                {
                  AuditInstance = auditInstance,
                  AuditResponse = newAuditResponse,
                  //FormItemId = item.ItemId, // Required: FormItemId
                  CorrectiveActionNotes = null,
                  Status = CorrectiveActionStatus.Pending, // Corrected Enum usage
                  CreatedBy = User.Identity.Name,
                  CreatedDate = DateTime.Now,
                  DueDate = DateTime.Now.AddDays(7)
                });
              }
            }
          }
          else
          {
            var fieldName = item.ItemId.ToString();
            StringValues submittedValues = Request.Form[fieldName]; // Corrected
            var submittedValueCombined = string.Join(",", submittedValues.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)));

            int? itemScore = CalculateItemScore(item, submittedValueCombined);
            totalScore += itemScore ?? 0;

            if (isScoreableItemType)
            {
              totalMaxScore += item.MaxScore ?? 0;
            }

            var newAuditResponse = new AuditResponse
            {
              FormItemId = item.ItemId,
              FormItemQuestion = item.Question,
              ResponseValue = submittedValueCombined,
              ScoredValue = itemScore,
              MaxPossibleScore = item.MaxScore,
            };
            auditInstance.AuditResponses.Add(newAuditResponse);

            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && item.IsRequired && string.IsNullOrWhiteSpace(submittedValueCombined))
            {
              ModelState.AddModelError(fieldName, $"The '{item.Question}' field is required.");
            }
            else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && isScoreableItemType && (itemScore < item.MaxScore || !itemScore.HasValue))
            {
              // Create CorrectiveAction if item is scoreable and not fully scored
              _context.CorrectiveActions.Add(new CorrectiveAction
              {
                AuditInstance = auditInstance,
                AuditResponse = newAuditResponse,
                //FormItemId = item.ItemId, // Required: FormItemId
                CorrectiveActionNotes = null,
                Status = CorrectiveActionStatus.Pending, // Corrected Enum usage
                CreatedBy = User.Identity.Name,
                CreatedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7)
              });
            }
          }
        }
      }

      if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !ModelState.IsValid)
      {
        TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting.";
        ViewBag.IsUser = isUser;
        ViewBag.AuditInstanceId = auditInstance.AuditInstanceId;
        ViewBag.ExistingAuditResponses = auditInstance.AuditResponses.ToDictionary(r => $"{r.FormItemId}{(r.LoopIndex.HasValue ? "_loop_" + r.LoopIndex.Value : "")}", r => r.ResponseValue);

        var reloadedJenisForm = await _context.JenisForms
            .Include(f => f.Sections)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(f => f.FormTypeId == formTypeId && f.TenantId == currentTenantId);
        return View("~/Views/Audit/AuditForm.cshtml", reloadedJenisForm);
      }

      auditInstance.TotalScore = totalScore;
      auditInstance.TotalMaxScore = totalMaxScore;
      auditInstance.PercentageScore = totalMaxScore > 0 ? (double)totalScore / totalMaxScore * 100 : 0;

      if (status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
      {
        auditInstance.Status = AuditStatus.Draft;
        TempData["SuccessMessage"] = "Audit Form saved as draft!";
      }
      else
      {
        bool hasPendingCorrectiveActions = await _context.CorrectiveActions.AnyAsync(ca => ca.AuditInstanceId == auditInstance.AuditInstanceId && ca.Status != CorrectiveActionStatus.Completed); // Corrected: Use Completed

        if (hasPendingCorrectiveActions)
        {
          auditInstance.Status = AuditStatus.NeedsCorrectiveAction;
          TempData["SuccessMessage"] = "Audit Form submitted. Corrective actions required!";
        }
        else
        {
          auditInstance.Status = AuditStatus.Completed;
          TempData["SuccessMessage"] = "Audit Form submitted successfully! All items fully scored.";
        }
      }

      await _context.SaveChangesAsync();

      if (isNewAuditCreation && jenisForm.Status == FormStatus.Published)
      {
        jenisForm.Status = FormStatus.Revised;
        _context.JenisForms.Update(jenisForm);
        await _context.SaveChangesAsync();
      }

      if (status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
      {
        return RedirectToAction("Index");
      }
      else
      {
        return RedirectToAction("AuditConfirmation", new { auditId = auditInstance.AuditInstanceId });
      }
    }

    public async Task<IActionResult> FollowUpAudit(int auditInstanceId)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot access follow-up audit: Current tenant not identified.";
        return Unauthorized();
      }

      var previousAuditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.FormItem)
                  .ThenInclude(fi => fi.Section)
          .Include(ai => ai.JenisForm)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId && ai.TenantId == currentTenantId);

      if (previousAuditInstance == null)
      {
        TempData["ErrorMessage"] = "Previous audit instance not found or does not belong to your organization.";
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

          if (originalFormItem.HasLooping && originalFormItem.LoopCount.HasValue)
          {
            for (int i = 0; i < originalFormItem.LoopCount.Value; i++)
            {
              var responseForLoop = previousResponsesForThisItem.FirstOrDefault(ar => ar.LoopIndex == i);
              if (responseForLoop != null)
              {
                // Look for CAs specifically tied to this audit response
                var relatedCa = await _context.CorrectiveActions
                    .FirstOrDefaultAsync(ca => ca.AuditResponseId == responseForLoop.AuditResponseId && ca.Status != CorrectiveActionStatus.Completed); // Corrected: Completed

                // Include in follow-up if it was deficient or has a pending CA
                if (responseForLoop.ScoredValue < responseForLoop.MaxPossibleScore || relatedCa != null)
                {
                  var followUpItemDto = new FollowUpAuditItemDto
                  {
                    AuditResponseId = responseForLoop.AuditResponseId,
                    FormItemId = originalFormItem.ItemId,
                    FormItemQuestion = originalFormItem.Question,
                    MaxScore = originalFormItem.MaxScore,
                    ExistingScoredValue = responseForLoop.ScoredValue,
                    ExistingResponseValue = responseForLoop.ResponseValue,
                    HasLooping = originalFormItem.HasLooping,
                    LoopCount = originalFormItem.LoopCount,
                    LoopLabel = originalFormItem.LoopLabel,
                    LoopIndex = i,
                    CorrectiveActionNotes = relatedCa?.CorrectiveActionNotes,
                    DueDate = relatedCa?.DueDate, // Corrected: DueDate
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
          }
          else
          {
            var responseForThisItem = previousResponsesForThisItem.FirstOrDefault();
            if (responseForThisItem != null)
            {
              var relatedCa = await _context.CorrectiveActions
                  .FirstOrDefaultAsync(ca => ca.AuditResponseId == responseForThisItem.AuditResponseId && ca.Status != CorrectiveActionStatus.Completed); // Corrected: Completed

              if (responseForThisItem.ScoredValue < responseForThisItem.MaxPossibleScore || relatedCa != null)
              {
                var followUpItemDto = new FollowUpAuditItemDto
                {
                  AuditResponseId = responseForThisItem.AuditResponseId,
                  FormItemId = originalFormItem.ItemId,
                  FormItemQuestion = originalFormItem.Question,
                  MaxScore = originalFormItem.MaxScore,
                  ExistingScoredValue = responseForThisItem.ScoredValue,
                  ExistingResponseValue = responseForThisItem.ResponseValue,
                  HasLooping = originalFormItem.HasLooping,
                  LoopCount = originalFormItem.LoopCount,
                  LoopLabel = originalFormItem.LoopLabel,
                  LoopIndex = null,
                  CorrectiveActionNotes = relatedCa?.CorrectiveActionNotes,
                  DueDate = relatedCa?.DueDate, // Corrected: DueDate
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
        }
        if (followUpSectionDto.Items.Any())
        {
          viewModel.Sections.Add(followUpSectionDto);
        }
      }

      if (!viewModel.Sections.Any(s => s.Items.Any()))
      {
        TempData["SuccessMessage"] = "This audit instance has no items requiring follow-up. All previous items were fully scored or their corrective actions are resolved!";
      }

      return View(viewModel);
    }

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

      var originalAudit = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
              .ThenInclude(ar => ar.FormItem)
          .Include(ai => ai.CorrectiveActions)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == submitDto.OriginalAuditInstanceId && ai.TenantId == currentTenantId);

      if (originalAudit == null)
      {
        return NotFound();
      }

      if (!ModelState.IsValid)
      {
        TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting the follow-up.";
        return await RebuildFollowUpAuditViewModelForView(submitDto.OriginalAuditInstanceId, currentTenantId);
      }

      // --- Create a NEW AuditInstance for the follow-up audit ---
      var followUpAuditInstance = new AuditInstance
      {
        FormTypeId = originalAudit.FormTypeId,
        FormName = originalAudit.FormName + " (Follow-Up)",
        AuditorName = User.Identity.Name ?? "Unknown Auditor",
        AuditDate = DateTime.Now,
        TenantId = currentTenantId,
        OriginalAuditInstanceId = originalAudit.AuditInstanceId,
        Status = AuditStatus.Draft, // Temporarily set as Draft, will be updated below based on results
        AuditResponses = new List<AuditResponse>()
      };
      _context.AuditInstances.Add(followUpAuditInstance);

      int followUpTotalScore = 0;
      int followUpTotalMaxScore = 0;
      bool anyNewDeficienciesInFollowUp = false; // Tracks deficiencies in the follow-up audit itself

      for (int i = 0; i < submitDto.Items.Count; i++) // Use for loop to get index for form field name
      {
        var submittedItem = submitDto.Items[i];
        var formItemTemplate = await _context.FormItems.FindAsync(submittedItem.FormItemId);
        if (formItemTemplate == null) continue;

        var fullFieldName = $"Items[{i}].ResponseValue"; // Corrected: Use index for DTO binding name
        StringValues submittedValuesForFollowUp = Request.Form[fullFieldName];
        var submittedValueCombinedForFollowUp = string.Join(",", submittedValuesForFollowUp.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)));

        int? newScoredValue = CalculateItemScore(formItemTemplate, submittedValueCombinedForFollowUp);

        followUpAuditInstance.AuditResponses.Add(new AuditResponse
        {
          FormItemId = submittedItem.FormItemId,
          FormItemQuestion = formItemTemplate.Question,
          ResponseValue = submittedValueCombinedForFollowUp,
          ScoredValue = newScoredValue,
          MaxPossibleScore = formItemTemplate.MaxScore,
          LoopIndex = submittedItem.LoopIndex,
        });

        // Only add to followUpTotalScore/MaxScore if the item is scoreable
        if (formItemTemplate.ItemType == ItemType.Number ||
            formItemTemplate.ItemType == ItemType.Checkbox ||
            formItemTemplate.ItemType == ItemType.Radio ||
            formItemTemplate.ItemType == ItemType.Dropdown)
        {
          followUpTotalScore += newScoredValue ?? 0;
          followUpTotalMaxScore += formItemTemplate.MaxScore ?? 0;

          if (newScoredValue < formItemTemplate.MaxScore || !newScoredValue.HasValue)
          {
            anyNewDeficienciesInFollowUp = true;
          }
        }


        // --- Corrective Action Status Update Logic for ORIGINAL Audit's CAs ---
        var existingCa = originalAudit.CorrectiveActions
                                .FirstOrDefault(ca => ca.AuditResponseId == submittedItem.OriginalAuditResponseId && ca.Status != CorrectiveActionStatus.Completed); // Corrected: Completed

        if (existingCa != null)
        {
          bool originalDeficiencyResolved = (newScoredValue >= formItemTemplate.MaxScore);

          if (originalDeficiencyResolved)
          {
            existingCa.Status = CorrectiveActionStatus.Completed; // Corrected: Completed
            existingCa.CompletedBy = User.Identity.Name; // Corrected: CompletedBy
            existingCa.CompletionDate = DateTime.Now; // Corrected: CompletionDate
            //existingCa.ResolutionNotes = submittedItem.CorrectiveActionNotes; // Corrected: CorrectiveActionNotes
            existingCa.LastModifiedBy = User.Identity.Name;
            existingCa.LastModifiedDate = DateTime.Now;
          }
          else
          {
            existingCa.CorrectiveActionNotes = submittedItem.CorrectiveActionNotes; // Corrected: CorrectiveActionNotes
            if (submittedItem.DueDate.HasValue)
            {
              existingCa.DueDate = submittedItem.DueDate.Value;
            }
            //existingCa.Status = CorrectiveActionStatus.NeedsFollowUp; // Corrected: NeedsFollowUp
            existingCa.LastModifiedBy = User.Identity.Name;
            existingCa.LastModifiedDate = DateTime.Now;
          }
          _context.CorrectiveActions.Update(existingCa);
        }
      }

      // --- Finalize NEW Follow-Up Audit Instance Scores and Status ---
      followUpAuditInstance.TotalScore = followUpTotalScore;
      followUpAuditInstance.TotalMaxScore = followUpTotalMaxScore;
      followUpAuditInstance.PercentageScore = followUpTotalMaxScore > 0 ? (double)followUpTotalScore / followUpTotalMaxScore * 100 : 0;

      if (anyNewDeficienciesInFollowUp)
      {
        followUpAuditInstance.Status = AuditStatus.NeedsCorrectiveAction;
      }
      else
      {
        followUpAuditInstance.Status = AuditStatus.Completed;
      }

      await _context.SaveChangesAsync(); // Save the new follow-up audit and its responses, and updated CAs

      // --- Update ORIGINAL Audit Instance Status based on ALL its Corrective Actions ---
      var allOriginalCAs = await _context.CorrectiveActions
                              .Where(ca => ca.AuditInstanceId == originalAudit.AuditInstanceId)
                              .ToListAsync();

      bool allOriginalCAsResolved = allOriginalCAs.All(ca => ca.Status == CorrectiveActionStatus.Completed); // Corrected: Completed

      if (allOriginalCAsResolved)
      {
        originalAudit.Status = AuditStatus.Completed;
        TempData["SuccessMessage"] = "Follow Up Audit recorded successfully. Original audit instance is now Completed!";
      }
      else
      {
        originalAudit.Status = AuditStatus.NeedsFollowUp; // Corrected: NeedsFollowUp
        TempData["SuccessMessage"] = "Follow Up Audit recorded successfully. Original audit instance still requires follow-up for outstanding items.";
      }
      _context.AuditInstances.Update(originalAudit);
      await _context.SaveChangesAsync(); // Save the updated status of the original audit

      return RedirectToAction(nameof(Details), new { id = followUpAuditInstance.AuditInstanceId });
    }


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

          if (originalFormItem.HasLooping && originalFormItem.LoopCount.HasValue)
          {
            for (int i = 0; i < originalFormItem.LoopCount.Value; i++)
            {
              var responseForLoop = previousResponsesForThisItem.FirstOrDefault(ar => ar.LoopIndex == i);
              if (responseForLoop != null)
              {
                var relatedCa = await _context.CorrectiveActions
                    .FirstOrDefaultAsync(ca => ca.AuditResponseId == responseForLoop.AuditResponseId && ca.Status != CorrectiveActionStatus.Completed); // Corrected: Completed

                if (responseForLoop.ScoredValue < responseForLoop.MaxPossibleScore || relatedCa != null)
                {
                  var followUpItemDto = new FollowUpAuditItemDto
                  {
                    AuditResponseId = responseForLoop.AuditResponseId,
                    FormItemId = originalFormItem.ItemId,
                    FormItemQuestion = originalFormItem.Question,
                    MaxScore = originalFormItem.MaxScore,
                    ExistingScoredValue = responseForLoop.ScoredValue,
                    ExistingResponseValue = responseForLoop.ResponseValue,
                    HasLooping = originalFormItem.HasLooping,
                    LoopCount = originalFormItem.LoopCount,
                    LoopLabel = originalFormItem.LoopLabel,
                    LoopIndex = i,
                    CorrectiveActionNotes = relatedCa?.CorrectiveActionNotes,
                    DueDate = relatedCa?.DueDate, // Corrected: DueDate
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
          }
          else
          {
            var responseForThisItem = previousResponsesForThisItem.FirstOrDefault();
            if (responseForThisItem != null)
            {
              var relatedCa = await _context.CorrectiveActions
                  .FirstOrDefaultAsync(ca => ca.AuditResponseId == responseForThisItem.AuditResponseId && ca.Status != CorrectiveActionStatus.Completed); // Corrected: Completed

              if (responseForThisItem.ScoredValue < responseForThisItem.MaxPossibleScore || relatedCa != null)
              {
                var followUpItemDto = new FollowUpAuditItemDto
                {
                  AuditResponseId = responseForThisItem.AuditResponseId,
                  FormItemId = originalFormItem.ItemId,
                  FormItemQuestion = originalFormItem.Question,
                  MaxScore = originalFormItem.MaxScore,
                  ExistingScoredValue = responseForThisItem.ScoredValue,
                  ExistingResponseValue = responseForThisItem.ResponseValue,
                  HasLooping = originalFormItem.HasLooping,
                  LoopCount = originalFormItem.LoopCount,
                  LoopLabel = originalFormItem.LoopLabel,
                  LoopIndex = null,
                  CorrectiveActionNotes = relatedCa?.CorrectiveActionNotes,
                  DueDate = relatedCa?.DueDate, // Corrected: DueDate
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
        }
        if (followUpSectionDto.Items.Any())
        {
          viewModel.Sections.Add(followUpSectionDto);
        }
      }

      if (!viewModel.Sections.Any(s => s.Items.Any()))
      {
        TempData["SuccessMessage"] = "This audit instance has no items requiring follow-up. All previous items were fully scored or their corrective actions are resolved!";
      }

      return View(viewModel);
    }

    public int? CalculateItemScore(FormItem item, string? submittedValueCombined)
    {
      int? score = null;

      if (item.ItemType == ItemType.Text ||
          item.ItemType == ItemType.Textarea ||
          item.ItemType == ItemType.File ||
          item.ItemType == ItemType.Signature)
      {
        return null;
      }

      if (!item.MaxScore.HasValue || item.MaxScore.Value <= 0)
      {
        return 0;
      }

      if (string.IsNullOrWhiteSpace(submittedValueCombined))
      {
        return 0;
      }

      switch (item.ItemType)
      {
        case ItemType.Number:
          if (int.TryParse(submittedValueCombined, out int numValue))
          {
            score = Math.Min(numValue, item.MaxScore.Value);
            score = Math.Max(score.Value, 0);
          }
          else
          {
            score = 0;
          }
          break;

        case ItemType.Checkbox:
          if (!string.IsNullOrEmpty(item.OptionsJson))
          {
            try
            {
              var allowedOptions = JsonSerializer.Deserialize<List<string>>(item.OptionsJson);
              var submittedOptions = new HashSet<string>(submittedValueCombined.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);

              bool anyValidOptionSelected = submittedOptions.Any(so => allowedOptions.Contains(so, StringComparer.OrdinalIgnoreCase));

              if (anyValidOptionSelected)
              {
                score = item.MaxScore.Value;
              }
              else
              {
                score = 0;
              }
            }
            catch (JsonException ex)
            {
              Console.WriteLine($"Error deserializing OptionsJson (List<string>) for checkbox item {item.ItemId}: {ex.Message}");
              score = 0;
            }
          }
          else
          {
            if (submittedValueCombined.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                submittedValueCombined.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
              score = item.MaxScore.Value;
            }
            else
            {
              score = 0;
            }
          }
          break;

        case ItemType.Radio:
        case ItemType.Dropdown:
          if (!string.IsNullOrEmpty(item.OptionsJson))
          {
            try
            {
              var options = JsonSerializer.Deserialize<List<string>>(item.OptionsJson);
              if (options != null && options.Contains(submittedValueCombined))
              {
                score = item.MaxScore.Value;
              }
              else
              {
                score = 0;
              }
            }
            catch (JsonException ex)
            {
              Console.WriteLine($"Error deserializing OptionsJson (List<string>) for item {item.ItemId}: {ex.Message}");
              score = 0;
            }
          }
          else
          {
            score = 0;
          }
          break;

        default:
          score = null;
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
                  .ThenInclude(fi => fi.Section)
          .Include(ai => ai.JenisForm)
          .Include(ai => ai.OriginalAuditInstance) // Include the original audit if this is a follow-up
          .FirstOrDefaultAsync(m => m.AuditInstanceId == id && m.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        TempData["ErrorMessage"] = "Audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      // Also retrieve follow-up audits related to this instance (if any)
      ViewBag.FollowUpAuditInstances = await _context.AuditInstances
          .Where(ai => ai.OriginalAuditInstanceId == id && ai.TenantId == currentTenantId)
          .OrderByDescending(ai => ai.AuditDate)
          .ToListAsync();

      return View(auditInstance);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot archive audit: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances.FirstOrDefaultAsync(ai => ai.AuditInstanceId == id && ai.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        TempData["ErrorMessage"] = "Audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      if (auditInstance.Status == AuditStatus.Draft)
      {
        TempData["ErrorMessage"] = "Draft audits cannot be archived. Please delete them if no longer needed.";
        return RedirectToAction(nameof(Index));
      }

      auditInstance.IsArchived = true;
      _context.AuditInstances.Update(auditInstance);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = $"Audit Instance {id} has been archived successfully!";
      return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot restore audit: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances.FirstOrDefaultAsync(ai => ai.AuditInstanceId == id && ai.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        TempData["ErrorMessage"] = "Audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      if (!auditInstance.IsArchived)
      {
        TempData["ErrorMessage"] = "This audit instance is not archived and cannot be restored.";
        return RedirectToAction(nameof(Archived));
      }

      auditInstance.IsArchived = false;
      _context.AuditInstances.Update(auditInstance);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = $"Audit Instance {id} has been restored successfully!";
      return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(int id)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot permanently delete audit: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
          .Include(ai => ai.CorrectiveActions)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == id && ai.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        TempData["ErrorMessage"] = "Audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      if (!auditInstance.IsArchived)
      {
        TempData["ErrorMessage"] = "Only archived audit instances can be permanently deleted.";
        return RedirectToAction(nameof(Archived));
      }

      _context.CorrectiveActions.RemoveRange(auditInstance.CorrectiveActions);
      _context.AuditResponses.RemoveRange(auditInstance.AuditResponses);
      _context.AuditInstances.Remove(auditInstance);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = $"Archived Audit Instance {id} has been permanently deleted.";
      return RedirectToAction(nameof(Archived));
    }


    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      if (string.IsNullOrEmpty(currentTenantId))
      {
        TempData["ErrorMessage"] = "Cannot delete audit: Current tenant not identified.";
        return Unauthorized();
      }

      var auditInstance = await _context.AuditInstances
          .Include(ai => ai.AuditResponses)
          .Include(ai => ai.CorrectiveActions)
          .FirstOrDefaultAsync(ai => ai.AuditInstanceId == id && ai.TenantId == currentTenantId);

      if (auditInstance == null)
      {
        TempData["ErrorMessage"] = "Audit instance not found or does not belong to your organization.";
        return NotFound();
      }

      if (auditInstance.Status != AuditStatus.Draft)
      {
        TempData["ErrorMessage"] = "Only Draft audit instances can be deleted directly. Other statuses must be archived.";
        return RedirectToAction(nameof(Index));
      }

      _context.CorrectiveActions.RemoveRange(auditInstance.CorrectiveActions);
      _context.AuditResponses.RemoveRange(auditInstance.AuditResponses);
      _context.AuditInstances.Remove(auditInstance);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = $"Draft Audit Instance {id} has been deleted successfully.";
      return RedirectToAction(nameof(Index));
    }

    private bool AuditInstanceExists(int id)
    {
      var currentTenantId = _tenantService.GetCurrentTenantId();
      return _context.AuditInstances.Any(e => e.AuditInstanceId == id && e.TenantId == currentTenantId);
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

      if (caData != null && caData.Any(c => !string.IsNullOrWhiteSpace(c.CorrectiveActionNotes)))
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

        if (auditInstance.Status == AuditStatus.NeedsCorrectiveAction)
        {
          auditInstance.Status = AuditStatus.NeedsFollowUp;
          await _context.SaveChangesAsync();
          TempData["SuccessMessage"] = "Corrective actions saved successfully! Audit status updated to Needs Follow-Up.";
        }
        else
        {
          TempData["SuccessMessage"] = "Corrective actions saved successfully!";
        }
      }
      else
      {
        TempData["ErrorMessage"] = "No corrective actions data received or notes were empty.";
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
