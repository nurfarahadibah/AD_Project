// In a file like AspnetCoreMvcFull/Models/ViewModels/FollowUpAuditViewModel.cs
namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class FollowUpAuditViewModel
  {
    public int OriginalAuditInstanceId { get; set; }
    public int FormTypeId { get; set; }
    public string FormName { get; set; }
    public string FormDescription { get; set; }
    public List<FollowUpAuditItemDto> ItemsForFollowUp { get; set; } = new List<FollowUpAuditItemDto>();
  }

  public class FollowUpAuditItemDto
  {
    public int AuditResponseId { get; set; }
    public int FormItemId { get; set; }
    public string FormItemQuestion { get; set; }
    public string CorrectiveActionNotes { get; set; }
    public int? MaxScore { get; set; }
    public int? ExistingScoredValue { get; set; }
    public string? ExistingResponseValue { get; set; }
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public int? LoopIndex { get; set; } // Nullable for non-looping items
    public FormItemViewModel FormItem { get; set; } // To render the item
  }

  public class FormItemViewModel
  {
    public int ItemId { get; set; }
    public string Question { get; set; }
    public ItemType ItemType { get; set; }
    public string ItemTypeName { get; set; } // String representation of the ItemType enum
    public int? MaxScore { get; set; }
    public string? OptionsJson { get; set; } // JSON string of options
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public bool IsRequired { get; set; } // ADDED THIS PROPERTY
  }

  public class FollowUpAuditSubmitDto
  {
    public int OriginalAuditInstanceId { get; set; }
    public int FormTypeId { get; set; }
    public List<FollowUpAuditSubmittedItemDto> Items { get; set; } = new List<FollowUpAuditSubmittedItemDto>();
  }

  public class FollowUpAuditSubmittedItemDto
  {
    public int OriginalAuditResponseId { get; set; }
    public int ItemId { get; set; }
    public string? ResponseValue { get; set; } // The new response
    public int? LoopIndex { get; set; } // Nullable for non-looping items
  }

 /* public class ItemOption
  {
    public string? Value { get; set; }
    public int? Score { get; set; }
  } */
  // Used in CalculateItemScore for deserializing FormItem.PossibleResponses

}
