using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Added for [DataType(DataType.Date)]
using AspnetCoreMvcFull.Models; // Ensure this is present for ItemType enum etc.

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class FollowUpAuditViewModel
  {
    public int OriginalAuditInstanceId { get; set; }
    public int FormTypeId { get; set; }
    public string? FormName { get; set; } // Made nullable for safety
    public string? FormDescription { get; set; } // Made nullable for safety

    public List<FollowUpAuditSectionDto> Sections { get; set; } = new List<FollowUpAuditSectionDto>();
  }

  public class FollowUpAuditSectionDto
  {
    public int SectionId { get; set; }
    public string Title { get; set; } = string.Empty; // Ensure initialized
    public string? Description { get; set; }
    public int Order { get; set; }

    public List<FollowUpAuditItemDto> Items { get; set; } = new List<FollowUpAuditItemDto>();
  }

  public class FollowUpAuditItemDto
  {
    public int AuditResponseId { get; set; }
    public int FormItemId { get; set; }
    public string FormItemQuestion { get; set; } = string.Empty; // Ensure initialized
    public string? CorrectiveActionNotes { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; } // Added this missing property

    public int? MaxScore { get; set; }
    public int? ExistingScoredValue { get; set; }
    public string? ExistingResponseValue { get; set; }
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public int? LoopIndex { get; set; }

    public FormItemViewModel FormItem { get; set; } = new FormItemViewModel(); // Ensure initialized
  }

  public class FormItemViewModel
  {
    public int ItemId { get; set; }
    public string Question { get; set; } = string.Empty; // Ensure initialized
    public ItemType ItemType { get; set; }
    public string ItemTypeName { get; set; } = string.Empty; // Ensure initialized
    public int? MaxScore { get; set; }
    public string? OptionsJson { get; set; }
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public int SectionId { get; set; }
  }

  public class FollowUpAuditSubmitDto
  {
    public int OriginalAuditInstanceId { get; set; }
    public int FormTypeId { get; set; } // This property was not in your provided DTO, but was expected by my controller. Added for consistency.
    public List<FollowUpAuditSubmittedItemDto> Items { get; set; } = new List<FollowUpAuditSubmittedItemDto>();
  }

  public class FollowUpAuditSubmittedItemDto
  {
    public int OriginalAuditResponseId { get; set; }
    public int FormItemId { get; set; } // Added this missing property
    public string? ResponseValue { get; set; } // Made nullable for consistency
    public int? LoopIndex { get; set; }

    // --- Added these missing properties ---
    public string? CorrectiveActionNotes { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }
  }

  /*
   * The commented out ItemOption class was not directly part of the reported errors
   * but if you intended to use it in your ViewModels, keep it.
   * I've left it commented as per your input.
   */
}
