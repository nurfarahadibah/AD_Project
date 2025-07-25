using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO; 
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
    private readonly IWebHostEnvironment _webHostEnvironment;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

    public AuditController(AppDbContext context, ITenantService tenantService, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
    {
      _context = context;
      _tenantService = tenantService;
      _userManager = userManager;
      _webHostEnvironment = webHostEnvironment;
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
          var existingAuditResponses = existingAuditInstance.AuditResponses.ToDictionary(r => $"{r.FormItemId}{(r.LoopIndex.HasValue ? "_loop_" + r.LoopIndex.Value : "")}", r => r.ResponseValue);

          // Add new verification fields to existingAuditResponses for the view
          existingAuditResponses["AuditorName"] = existingAuditInstance.CheckedByName ?? "";
          existingAuditResponses["AuditorDesignation"] = existingAuditInstance.CheckedByDesignation ?? "";
          existingAuditResponses["AuditorDate"] = existingAuditInstance.CheckedByDate?.ToString("yyyy-MM-dd") ?? ""; // Format for HTML date input
          existingAuditResponses["AuditorSignatureData"] = existingAuditInstance.CheckedBySignatureData ?? "";

          existingAuditResponses["OutletName"] = existingAuditInstance.AcknowledgedByName ?? "";
          existingAuditResponses["OutletDesignation"] = existingAuditInstance.AcknowledgedByDesignation ?? "";
          existingAuditResponses["OutletDate"] = existingAuditInstance.AcknowledgedByDate?.ToString("yyyy-MM-dd") ?? "";
          existingAuditResponses["OutletSignatureData"] = existingAuditInstance.AcknowledgedBySignatureData ?? "";

          existingAuditResponses["VerifierName"] = existingAuditInstance.VerifiedByName ?? "";
          existingAuditResponses["VerifierDesignation"] = existingAuditInstance.VerifiedByDesignation ?? "";
          existingAuditResponses["VerifierDate"] = existingAuditInstance.VerifiedByDate?.ToString("yyyy-MM-dd") ?? "";
          existingAuditResponses["VerifierSignatureData"] = existingAuditInstance.VerifiedBySignatureData ?? "";

          ViewBag.ExistingAuditResponses = existingAuditResponses;
          //ViewBag.ExistingAuditResponses = existingAuditInstance.AuditResponses.ToDictionary(r => $"{r.FormItemId}{(r.LoopIndex.HasValue ? "_loop_" + r.LoopIndex.Value : "")}", r => r.ResponseValue);
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
    public async Task<IActionResult> SubmitAudit(
        int formTypeId,
        string status,
        int? auditInstanceId,
        // Add new parameters for the "Checked by: (Auditor/s)" section
        string? AuditorName,
        string? AuditorDesignation,
        DateTime? AuditorDate, // Direct binding for date
        string? AuditorSignatureData,
        // Add new parameters for the "Acknowledged by: (Outlet)" section
        string? OutletName,
        string? OutletDesignation,
        DateTime? OutletDate,
        string? OutletSignatureData,
        // Add new parameters for the "Verified by:" section
        string? VerifierName,
        string? VerifierDesignation,
        DateTime? VerifierDate,
        string? VerifierSignatureData
        )
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
      AuditStatus originalStatus = AuditStatus.Draft;

      if (auditInstanceId.HasValue && auditInstanceId.Value != 0)
      {
        auditInstance = await _context.AuditInstances
            .Include(ai => ai.AuditResponses)
            .ThenInclude(ar => ar.FormItem)
            .FirstOrDefaultAsync(ai => ai.AuditInstanceId == auditInstanceId.Value && ai.TenantId == currentTenantId);

        if (auditInstance == null)
        {
          TempData["ErrorMessage"] = "Cannot update audit: instance not found.";
          return NotFound();
        }
        originalStatus = auditInstance.Status;

        var relatedCorrectiveActions = await _context.CorrectiveActions
                                            .Where(ca => ca.AuditInstanceId == auditInstance.AuditInstanceId)
                                            .ToListAsync();
        _context.CorrectiveActions.RemoveRange(relatedCorrectiveActions);

        _context.AuditResponses.RemoveRange(auditInstance.AuditResponses);
        auditInstance.AuditResponses.Clear();
      }
      else
      {
        auditInstance = new AuditInstance
        {
          FormTypeId = jenisForm.FormTypeId,
          FormName = jenisForm.Name,
          AuditorName = User.Identity.Name ?? "Unknown Auditor",
          TenantId = currentTenantId,
          AuditResponses = new List<AuditResponse>(),
        };
        _context.AuditInstances.Add(auditInstance);
        await _context.SaveChangesAsync();
      }

      auditInstance.AuditDate = DateTime.Now;

      // Assign the new verification fields to the auditInstance
      auditInstance.CheckedByName = AuditorName;
      auditInstance.CheckedByDesignation = AuditorDesignation;
      auditInstance.CheckedByDate = AuditorDate;
      auditInstance.CheckedBySignatureData = AuditorSignatureData;

      auditInstance.AcknowledgedByName = OutletName;
      auditInstance.AcknowledgedByDesignation = OutletDesignation;
      auditInstance.AcknowledgedByDate = OutletDate;
      auditInstance.AcknowledgedBySignatureData = OutletSignatureData;

      auditInstance.VerifiedByName = VerifierName;
      auditInstance.VerifiedByDesignation = VerifierDesignation;
      auditInstance.VerifiedByDate = VerifierDate;
      auditInstance.VerifiedBySignatureData = VerifierSignatureData;


      var uploadedFilePaths = new Dictionary<string, string>();
      // Changed from fileResponses to Request.Form.Files
      if (Request.Form.Files != null && Request.Form.Files.Any())
      {
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
          Directory.CreateDirectory(uploadsFolder);
        }

        foreach (var file in Request.Form.Files) // Iterate Request.Form.Files directly
        {
          if (file.Length > 0)
          {
            var inputNameFromFile = file.Name; // This will now correctly be item.ItemId or item.ItemId_loop_i
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
              using (var stream = new FileStream(filePath, FileMode.Create))
              {
                await file.CopyToAsync(stream);
              }
              uploadedFilePaths[inputNameFromFile] = "/uploads/" + uniqueFileName;
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error saving file {file.FileName}: {ex.Message}");
              ModelState.AddModelError(inputNameFromFile, $"Could not upload file: {file.FileName}. Error: {ex.Message}");
            }
          }
        }
      }

      int totalScore = 0;
      int totalMaxScore = 0;

      foreach (var section in jenisForm.Sections.OrderBy(s => s.Order))
      {
        foreach (var item in section.Items.OrderBy(i => i.Order))
        {
          if (item.HasLooping && item.LoopCount.HasValue)
          {
            for (int i = 0; i < item.LoopCount.Value; i++)
            {
              var fieldName = $"{item.ItemId}_loop_{i}";
              StringValues submittedValues = Request.Form[fieldName];
              var submittedValueCombined = string.Join(",", submittedValues.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)));

              string responseValueForDb = submittedValueCombined;
              if (item.ItemType == ItemType.File || item.ItemType == ItemType.Signature)
              {
                if (uploadedFilePaths.ContainsKey(fieldName))
                {
                  responseValueForDb = uploadedFilePaths[fieldName];
                }
                else if (auditInstanceId.HasValue)
                {
                  var existingResponseForFile = await _context.AuditResponses
                      .FirstOrDefaultAsync(ar => ar.AuditInstanceId == auditInstance.AuditInstanceId &&
                                                 ar.FormItemId == item.ItemId && ar.LoopIndex == i);
                  if (existingResponseForFile != null)
                  {
                    responseValueForDb = existingResponseForFile.ResponseValue;
                  }
                  else
                  {
                    responseValueForDb = string.Empty;
                  }
                }
                else
                {
                  responseValueForDb = string.Empty;
                }
              }

              int? itemScore = CalculateItemScore(item, responseValueForDb);
              totalScore += itemScore ?? 0;
              totalMaxScore += item.MaxScore ?? 0;

              var newAuditResponse = new AuditResponse
              {
                AuditInstanceId = auditInstance.AuditInstanceId,
                FormItemId = item.ItemId,
                FormItemQuestion = item.Question,
                ResponseValue = responseValueForDb,
                ScoredValue = itemScore,
                MaxPossibleScore = item.MaxScore,
                LoopIndex = i,
                // DateSubmitted removed as requested
              };
              auditInstance.AuditResponses.Add(newAuditResponse);

              if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && item.IsRequired && string.IsNullOrWhiteSpace(responseValueForDb))
              {
                ModelState.AddModelError(fieldName, $"The '{item.Question} - {item.LoopLabel} {i + 1}' field is required.");
              }
              else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && (itemScore < item.MaxScore || !itemScore.HasValue))
              {
                _context.CorrectiveActions.Add(new CorrectiveAction
                {
                  AuditInstanceId = auditInstance.AuditInstanceId,
                  AuditResponse = newAuditResponse,
                  // FormItemId = item.ItemId, // COMMENTED OUT as per your request to avoid model change
                  CorrectiveActionNotes = string.Empty,
                  Status = CorrectiveActionStatus.Pending,
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
            StringValues submittedValues = Request.Form[fieldName];
            var submittedValueCombined = string.Join(",", submittedValues.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)));

            string responseValueForDb = submittedValueCombined;
            if (item.ItemType == ItemType.File || item.ItemType == ItemType.Signature)
            {
              if (uploadedFilePaths.ContainsKey(fieldName))
              {
                responseValueForDb = uploadedFilePaths[fieldName];
              }
              else if (auditInstanceId.HasValue)
              {
                var existingResponseForFile = await _context.AuditResponses
                    .FirstOrDefaultAsync(ar => ar.AuditInstanceId == auditInstance.AuditInstanceId &&
                                               ar.FormItemId == item.ItemId && ar.LoopIndex == null);
                if (existingResponseForFile != null)
                {
                  responseValueForDb = existingResponseForFile.ResponseValue;
                }
                else
                {
                  responseValueForDb = string.Empty;
                }
              }
              else
              {
                responseValueForDb = string.Empty;
              }
            }

            int? itemScore = CalculateItemScore(item, responseValueForDb);
            totalScore += itemScore ?? 0;
            totalMaxScore += item.MaxScore ?? 0;

            var newAuditResponse = new AuditResponse
            {
              AuditInstanceId = auditInstance.AuditInstanceId,
              FormItemId = item.ItemId,
              FormItemQuestion = item.Question,
              ResponseValue = responseValueForDb,
              ScoredValue = itemScore,
              MaxPossibleScore = item.MaxScore,
              // DateSubmitted removed as requested
            };
            auditInstance.AuditResponses.Add(newAuditResponse);

            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && item.IsRequired && string.IsNullOrWhiteSpace(responseValueForDb))
            {
              ModelState.AddModelError(fieldName, $"The '{item.Question}' field is required.");
            }
            else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && (itemScore < item.MaxScore || !itemScore.HasValue))
            {
              _context.CorrectiveActions.Add(new CorrectiveAction
              {
                AuditInstanceId = auditInstance.AuditInstanceId,
                AuditResponse = newAuditResponse,
                // FormItemId = item.ItemId, // COMMENTED OUT as per your request to avoid model change
                CorrectiveActionNotes = string.Empty,
                Status = CorrectiveActionStatus.Pending,
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
        //ViewBag.ExistingAuditResponses = auditInstance.AuditResponses
            //.ToDictionary(r => $"{r.FormItemId}{(r.LoopIndex.HasValue ? "_loop_" + r.LoopIndex.Value : "")}", r => r.ResponseValue);
        var existingAuditResponsesForReload = auditInstance.AuditResponses
            .ToDictionary(r => $"{r.FormItemId}{(r.LoopIndex.HasValue ? "_loop_" + r.LoopIndex.Value : "")}", r => r.ResponseValue);

        existingAuditResponsesForReload["AuditorName"] = auditInstance.CheckedByName ?? "";
        existingAuditResponsesForReload["AuditorDesignation"] = auditInstance.CheckedByDesignation ?? "";
        existingAuditResponsesForReload["AuditorDate"] = auditInstance.CheckedByDate?.ToString("yyyy-MM-dd") ?? "";
        existingAuditResponsesForReload["AuditorSignatureData"] = auditInstance.CheckedBySignatureData ?? "";

        existingAuditResponsesForReload["OutletName"] = auditInstance.AcknowledgedByName ?? "";
        existingAuditResponsesForReload["OutletDesignation"] = auditInstance.AcknowledgedByDesignation ?? "";
        existingAuditResponsesForReload["OutletDate"] = auditInstance.AcknowledgedByDate?.ToString("yyyy-MM-dd") ?? "";
        existingAuditResponsesForReload["OutletSignatureData"] = auditInstance.AcknowledgedBySignatureData ?? "";

        existingAuditResponsesForReload["VerifierName"] = auditInstance.VerifiedByName ?? "";
        existingAuditResponsesForReload["VerifierDesignation"] = auditInstance.VerifiedByDesignation ?? "";
        existingAuditResponsesForReload["VerifierDate"] = auditInstance.VerifiedByDate?.ToString("yyyy-MM-dd") ?? "";
        existingAuditResponsesForReload["VerifierSignatureData"] = auditInstance.VerifiedBySignatureData ?? "";

        ViewBag.ExistingAuditResponses = existingAuditResponsesForReload;

        var reloadedJenisForm = await _context.JenisForms
            .Include(f => f.Sections)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(f => f.FormTypeId == formTypeId && f.TenantId == currentTenantId);
        return View("~/Views/Audit/AuditForm.cshtml", reloadedJenisForm);
      }

      auditInstance.TotalScore = totalScore;
      auditInstance.TotalMaxScore = totalMaxScore;
      auditInstance.PercentageScore = totalMaxScore > 0 ? (double)totalScore / totalMaxScore * 100 : 0;

      if (isNewAuditCreation)
      {
        // No explicit action needed here for new creation, as it's added earlier
      }
      else
      {
        _context.AuditInstances.Update(auditInstance);
      }

      await _context.SaveChangesAsync();

      if (status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
      {
        auditInstance.Status = AuditStatus.Draft;
        TempData["SuccessMessage"] = "Audit Form saved as draft!";
      }
      else
      {
        bool hasPendingCorrectiveActions = await _context.CorrectiveActions.AnyAsync(ca =>
            ca.AuditInstanceId == auditInstance.AuditInstanceId &&
            ca.Status != CorrectiveActionStatus.Completed);

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
        TempData["ErrorMessage"] = "Original audit instance not found.";
        return NotFound();
      }

      if (!ModelState.IsValid)
      {
        TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting the follow-up.";
        return await RebuildFollowUpAuditViewModelForView(submitDto.OriginalAuditInstanceId, currentTenantId);
      }

      // --- Update existing AuditResponses for the original audit instance ---
      foreach (var submittedItem in submitDto.Items)
      {
        var formItemTemplate = await _context.FormItems.FindAsync(submittedItem.FormItemId);
        if (formItemTemplate == null) continue;

        var existingAuditResponse = originalAudit.AuditResponses
            .FirstOrDefault(ar => ar.AuditResponseId == submittedItem.OriginalAuditResponseId);

        if (existingAuditResponse != null)
        {
          string inputNameForFile = submittedItem.FormItemId.ToString();
          if (submittedItem.LoopIndex.HasValue)
          {
            inputNameForFile = $"{submittedItem.FormItemId}_loop_{submittedItem.LoopIndex.Value}";
          }

          string responseValueForDb = "";

          var file = Request.Form.Files.FirstOrDefault(f => f.Name == inputNameForFile);
          if (file != null && file.Length > 0)
          {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
              Directory.CreateDirectory(uploadsFolder);
            }
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
              using (var stream = new FileStream(filePath, FileMode.Create))
              {
                await file.CopyToAsync(stream);
              }
              responseValueForDb = "/uploads/" + uniqueFileName;
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error saving file {file.FileName}: {ex.Message}");
              ModelState.AddModelError(inputNameForFile, $"Could not upload file: {file.FileName}. Error: {ex.Message}");
              responseValueForDb = existingAuditResponse.ResponseValue ?? string.Empty;
            }
          }
          else
          {
            if (formItemTemplate.ItemType == ItemType.File || formItemTemplate.ItemType == ItemType.Signature)
            {
              responseValueForDb = existingAuditResponse.ResponseValue ?? string.Empty;
            }
            else
            {
              var formFieldName = $"Items[{submitDto.Items.IndexOf(submittedItem)}].ResponseValue";
              StringValues submittedValues = Request.Form[formFieldName];
              responseValueForDb = string.Join(",", submittedValues.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)));
            }
          }

          int? newScoredValue = CalculateItemScore(formItemTemplate, responseValueForDb);

          existingAuditResponse.ResponseValue = responseValueForDb;
          existingAuditResponse.ScoredValue = newScoredValue;
          existingAuditResponse.MaxPossibleScore = formItemTemplate.MaxScore;

          _context.AuditResponses.Update(existingAuditResponse);

          // --- Corrective Action Status Update Logic for ORIGINAL Audit's CAs ---
          var existingCa = originalAudit.CorrectiveActions
              .FirstOrDefault(ca => ca.AuditResponseId == submittedItem.OriginalAuditResponseId && ca.Status != CorrectiveActionStatus.Completed);

          if (existingCa != null)
          {
            bool originalDeficiencyResolved = (newScoredValue >= formItemTemplate.MaxScore);
            if (originalDeficiencyResolved)
            {
              existingCa.Status = CorrectiveActionStatus.Completed;
              existingCa.CompletedBy = User.Identity.Name;
              existingCa.CompletionDate = DateTime.Now;
              existingCa.LastModifiedBy = User.Identity.Name;
              existingCa.LastModifiedDate = DateTime.Now;
            }
            else
            {
              existingCa.CorrectiveActionNotes = submittedItem.CorrectiveActionNotes;
              // Directly assign DueDate, no .HasValue or .Value needed
              //existingCa.DueDate = submittedItem.DueDate;

              if (existingCa.Status == CorrectiveActionStatus.Pending && !string.IsNullOrWhiteSpace(existingCa.CorrectiveActionNotes))
              {
                existingCa.Status = CorrectiveActionStatus.InProgress;
              }
              // Check if DueDate has passed AND it's not completed
              if (existingCa.DueDate < DateTime.Now && existingCa.Status != CorrectiveActionStatus.Completed)
              {
                existingCa.Status = CorrectiveActionStatus.Overdue;
              }

              existingCa.LastModifiedBy = User.Identity.Name;
              existingCa.LastModifiedDate = DateTime.Now;
            }
            _context.CorrectiveActions.Update(existingCa);
          }
        }
      }

      originalAudit.TotalScore = originalAudit.AuditResponses.Sum(ar => ar.ScoredValue ?? 0);
      originalAudit.TotalMaxScore = originalAudit.AuditResponses.Sum(ar => ar.MaxPossibleScore ?? 0);
      originalAudit.PercentageScore = originalAudit.TotalMaxScore > 0 ? (double)originalAudit.TotalScore / originalAudit.TotalMaxScore * 100 : 0;

      bool anyOutstandingCAs = originalAudit.CorrectiveActions.Any(ca =>
          ca.Status == CorrectiveActionStatus.Pending ||
          ca.Status == CorrectiveActionStatus.InProgress ||
          ca.Status == CorrectiveActionStatus.Overdue);

      if (anyOutstandingCAs)
      {
        originalAudit.Status = AuditStatus.NeedsFollowUp;
        TempData["SuccessMessage"] = "Follow Up Audit recorded successfully. Original audit instance still requires follow-up for outstanding items.";
      }
      else
      {
        originalAudit.Status = AuditStatus.Completed;
        TempData["SuccessMessage"] = "Follow Up Audit recorded successfully. Original audit instance is now Completed!";
      }

      _context.AuditInstances.Update(originalAudit);
      await _context.SaveChangesAsync();

      return RedirectToAction(nameof(Details), new { id = originalAudit.AuditInstanceId });
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

    private int? CalculateItemScore(FormItem item, string? submittedValue)
    {
      // If item is not scoreable or has no max score, return 0.
      if (!item.MaxScore.HasValue || item.MaxScore.Value <= 0)
      {
        return 0;
      }

      // If there's no submitted value, the score is 0.
      if (string.IsNullOrWhiteSpace(submittedValue))
      {
        return 0;
      }

      switch (item.ItemType)
      {
        case ItemType.Number:
          if (int.TryParse(submittedValue, out int numValue))
          {
            // Score is the minimum of submitted value and max score, ensuring it's not negative.
            return Math.Max(0, Math.Min(numValue, item.MaxScore.Value));
          }
          // If parsing fails for a number type, score is 0.
          return 0;

        case ItemType.Checkbox:
          if (!string.IsNullOrEmpty(item.OptionsJson))
          {
            try
            {
              // Deserialize options and check if any submitted option is valid.
              var allowedOptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.OptionsJson);
              var submittedOptions = new HashSet<string>(
                  submittedValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()),
                  StringComparer.OrdinalIgnoreCase
              );

              // If any valid option was selected, give max score.
              if (submittedOptions.Any(so => allowedOptions!.Contains(so, StringComparer.OrdinalIgnoreCase)))
              {
                return item.MaxScore.Value;
              }
              return 0; // No valid option selected
            }
            catch (System.Text.Json.JsonException ex)
            {
              Console.WriteLine($"Error deserializing OptionsJson (List<string>) for checkbox item {item.ItemId}: {ex.Message}");
              return 0; // Error in options, assume no score
            }
          }
          else
          {
            // Fallback for checkboxes without OptionsJson: score based on "true" or "on".
            if (submittedValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                submittedValue.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
              return item.MaxScore.Value;
            }
            return 0;
          }

        case ItemType.Radio:
        case ItemType.Dropdown:
          if (!string.IsNullOrEmpty(item.OptionsJson))
          {
            try
            {
              // Deserialize options and check if the submitted value is one of them.
              var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.OptionsJson);
              if (options != null && options.Contains(submittedValue))
              {
                return item.MaxScore.Value; // Submitted value is a valid option
              }
              return 0; // Submitted value is not a valid option
            }
            catch (System.Text.Json.JsonException ex)
            {
              Console.WriteLine($"Error deserializing OptionsJson (List<string>) for item {item.ItemId}: {ex.Message}");
              return 0; // Error in options, assume no score
            }
          }
          return 0; // No options defined, no score

        case ItemType.Text:
        case ItemType.Textarea:
        case ItemType.File:
        case ItemType.Signature:
          // For these types, if a value is submitted (not null/whitespace), it gets the max score.
          // Otherwise, it gets 0.
          return string.IsNullOrWhiteSpace(submittedValue) ? 0 : item.MaxScore.Value;

        default:
          // For any other item type not explicitly handled, it's not scoreable.
          return 0;
      }
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

      ViewBag.ExistingAuditResponses = auditInstance.AuditResponses
        .Where(ar => ar.FormItem != null && ar.ResponseValue != null)
        .ToDictionary(ar => ar.FormItem.ItemId.ToString(), ar => ar.ResponseValue);

      // Also retrieve follow-up audits related to this instance (if any)
      ViewBag.FollowUpAuditInstances = await _context.AuditInstances
          .Where(ai => ai.OriginalAuditInstanceId == id && ai.TenantId == currentTenantId)
          .OrderByDescending(ai => ai.AuditDate)
          .ToListAsync();

      foreach (var response in auditInstance.AuditResponses)
      {
        if (response.FormItem?.Question.Contains("Auditor Signature Data", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.CheckedBySignatureData = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Auditor Name", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.CheckedByName = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Auditor Designation", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.CheckedByDesignation = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Auditor Date", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          if (DateTime.TryParse(response.ResponseValue, out DateTime date))
          {
            auditInstance.CheckedByDate = date;
          }
        }
        if (response.FormItem?.Question.Contains("Outlet RepresentativeSignature Data", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.AcknowledgedBySignatureData = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Outlet Representative Name", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.AcknowledgedByName = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Outlet Representative Designation", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.AcknowledgedByDesignation = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Outlet Date", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          if (DateTime.TryParse(response.ResponseValue, out DateTime date))
          {
            auditInstance.AcknowledgedByDate = date;
          }
        }
        if (response.FormItem?.Question.Contains("Verification Representative Signature Data", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.VerifiedBySignatureData = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Verification Representative Name", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.VerifiedByName = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Verification Representative Designation", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          auditInstance.VerifiedByDesignation = response.ResponseValue;
        }
        else if (response.FormItem?.Question.Contains("Verification Date", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          if (DateTime.TryParse(response.ResponseValue, out DateTime date))
          {
            auditInstance.VerifiedByDate = date;
          }
        }
        // ... similar logic for AcknowledgedBy and VerifiedBy fields
        // (This code block is inferred from the `SubmitAudit` method and the need for these properties in the `Details` view)
      }

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
