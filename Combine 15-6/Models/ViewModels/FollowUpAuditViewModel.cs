// In a file like AspnetCoreMvcFull/Models/ViewModels/FollowUpAuditViewModel.cs
namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class FollowUpAuditViewModel
  {
    public int OriginalAuditInstanceId { get; set; }
    public int FormTypeId { get; set; }
    public string FormName { get; set; }
    public string FormDescription { get; set; }

    // CHANGE: Now holds a list of sections, each containing its relevant follow-up items.
    public List<FollowUpAuditSectionDto> Sections { get; set; } = new(); // Simplified collection initialization
  }

  // NEW: ViewModel for a Follow-up Audit Section
  // This will hold the section's metadata and its specific items that need follow-up
  public class FollowUpAuditSectionDto
  {
    public int SectionId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; } // To maintain the original section order in the UI

    // The items within this specific section that need follow-up
    public List<FollowUpAuditItemDto> Items { get; set; } = new(); // Simplified collection initialization
  }

  public class FollowUpAuditItemDto
  {
    public int AuditResponseId { get; set; } // The ID of the original AuditResponse being followed up
    public int FormItemId { get; set; }      // The ID of the FormItem template
    public string FormItemQuestion { get; set; }
    public string CorrectiveActionNotes { get; set; }
    public int? MaxScore { get; set; }
    public int? ExistingScoredValue { get; set; } // The score from the previous audit for this item
    public string? ExistingResponseValue { get; set; } // The response from the previous audit for this item
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public int? LoopIndex { get; set; } // Nullable for non-looping items

    // FormItem is the *template* for the item, so it should carry its template properties
    public FormItemViewModel FormItem { get; set; } // To render the item (template details)
  }

  // This ViewModel holds the static template properties of a FormItem
  public class FormItemViewModel
  {
    public int ItemId { get; set; }
    public string Question { get; set; }
    public ItemType ItemType { get; set; }
    public string ItemTypeName { get; set; }
    public int? MaxScore { get; set; }
    public string? OptionsJson { get; set; }
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public bool IsRequired { get; set; }

    // FIX: Added 'Order' property to resolve the compilation error in the Razor view
    public int Order { get; set; }

    // NEW: Add SectionId here to link FormItemViewModel back to its section
    // This is crucial for correctly populating FollowUpAuditSectionDto in the controller
    public int SectionId { get; set; }
  }

  // This DTO is for submitting the new responses from the Follow Up Audit form
  public class FollowUpAuditSubmitDto
  {
    public int OriginalAuditInstanceId { get; set; }
    public int FormTypeId { get; set; }
    // This list will contain the *new* responses for the items that were displayed and re-audited
    public List<FollowUpAuditSubmittedItemDto> Items { get; set; } = new(); // Simplified collection initialization
  }

  public class FollowUpAuditSubmittedItemDto
  {
    public int OriginalAuditResponseId { get; set; } // The ID of the original AuditResponse this follow-up refers to
    public int ItemId { get; set; } // The FormItemId of the question
    public string? ResponseValue { get; set; } // The new response value from the follow-up
    public int? LoopIndex { get; set; } // Nullable for non-looping items
  }

  /* public class ItemOption
  {
     public string? Value { get; set; }
     public int? Score { get; set; }
  } */
}
