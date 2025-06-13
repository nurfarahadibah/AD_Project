using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Added for [Key]
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  // Define the AuditStatus enum here or in a separate file like CommonEnums.cs
  public enum AuditStatus
  {
    Draft = 0,
    Completed = 1,
    NeedsCorrectiveAction = 2,
    NeedsFollowUp = 3
  }

  public enum CorrectiveActionStatus
  {
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Overdue = 3
  }


  public class AuditInstance
  {
    [Key]
    public int AuditInstanceId { get; set; } // Renamed from AuditId to match the new column

    public int FormTypeId { get; set; }
    public string FormName { get; set; }
    public DateTime AuditDate { get; set; }
    public string AuditorName { get; set; } // Removing this as per request from Index.cshtml
    public string TenantId { get; set; }

    public int TotalScore { get; set; }
    public int TotalMaxScore { get; set; }
    public double PercentageScore { get; set; }

    // NEW: Branch Name
    public string? BranchName { get; set; } // Assuming this comes from the audit context or form

    // NEW: Audit Status
    public AuditStatus Status { get; set; } = AuditStatus.Draft; // Default to Draft, update to Completed on submission

    public ICollection<AuditResponse> AuditResponses { get; set; }

    public virtual JenisForm JenisForm { get; set; }
    public virtual Tenant Tenant { get; set; }
    public ICollection<CorrectiveAction> CorrectiveActions { get; set; }
  }

  public class CorrectiveAction
  {
    [Key]
    public int CorrectiveActionId { get; set; }

    // Foreign key to the AuditInstance this action belongs to
    public int AuditInstanceId { get; set; }
    [ForeignKey("AuditInstanceId")]
    public virtual AuditInstance AuditInstance { get; set; }

    // Foreign key to the specific AuditResponse that needs correction
    public int AuditResponseId { get; set; }
    [ForeignKey("AuditResponseId")]
    public virtual AuditResponse AuditResponse { get; set; }

    [Required]
    [StringLength(500)]
    public string CorrectiveActionNotes { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime DueDate { get; set; }

    public CorrectiveActionStatus Status { get; set; } = CorrectiveActionStatus.Pending;

    // Optional: Who created/last updated this corrective action
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    // Optional: When it was completed and by whom
    public DateTime? CompletionDate { get; set; }
    public string? CompletedBy { get; set; }

  }
}


// Models/AuditResponse.cs
namespace AspnetCoreMvcFull.Models
{
  public class AuditResponse
  {
    public int AuditResponseId { get; set; }
    public int AuditInstanceId { get; set; } // Foreign key to AuditInstance
    public int FormItemId { get; set; } // Link back to the form item template

    public string FormItemQuestion { get; set; } // Store question text for easier reporting
    public string ResponseValue { get; set; }
    //public string SubmittedValue { get; set; } // The actual value submitted by the auditor
    public int? ScoredValue { get; set; } // The score calculated for this specific response
    public int? MaxPossibleScore { get; set; } // Max score for this item from the template

    public int? LoopIndex { get; set; } // Null for non-looping, or 0, 1, 2... for looped items
    public int? OriginalAuditResponseId { get; set; }

    public AuditInstance AuditInstance { get; set; } // Navigation property
    public FormItem FormItem { get; set; } // Navigation property to the template item
    public ICollection<CorrectiveAction> CorrectiveActions { get; set; }
  }
}

// Models/ItemOption.cs (if not already existing and you're using it for score-based options)
namespace AspnetCoreMvcFull.Models
{
  public class ItemOption
  {
    public string Value { get; set; } // The actual value stored (e.g., "Yes", "OptionA")
    public string Text { get; set; }  // The display text (e.g., "Yes, it complies")
    public int? Score { get; set; }   // The score associated with this option
    public int Order { get; set; }    // For ordering options in dropdown/radio/checkbox lists
  }
}
