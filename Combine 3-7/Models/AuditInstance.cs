using System;

using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;



namespace AspnetCoreMvcFull.Models

{

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
    public int AuditInstanceId { get; set; }

    public int FormTypeId { get; set; }
    public string FormName { get; set; }
    public DateTime AuditDate { get; set; }
    public string AuditorName { get; set; } // Existing auditor name, distinct from new Auditor verification
    public string TenantId { get; set; }

    public int TotalScore { get; set; }
    public int TotalMaxScore { get; set; }
    public double PercentageScore { get; set; }

    public AuditStatus Status { get; set; } = AuditStatus.Draft;
    public bool IsArchived { get; set; } = false; // Default to not archived

    // New fields for "Checked by: (Auditor/s)" section
    public string? CheckedByName { get; set; }
    public string? CheckedByDesignation { get; set; }
    public DateTime? CheckedByDate { get; set; }
    public string? CheckedBySignatureData { get; set; } // Base64 string for signature

    // New fields for "Acknowledged by: (Outlet)" section
    public string? AcknowledgedByName { get; set; }
    public string? AcknowledgedByDesignation { get; set; }
    public DateTime? AcknowledgedByDate { get; set; }
    public string? AcknowledgedBySignatureData { get; set; } // Base64 string for signature

    // New fields for "Verified by:" section
    public string? VerifiedByName { get; set; }
    public string? VerifiedByDesignation { get; set; }
    public DateTime? VerifiedByDate { get; set; }
    public string? VerifiedBySignatureData { get; set; } // Base64 string for signature

    public ICollection<AuditResponse> AuditResponses { get; set; }

    public virtual JenisForm JenisForm { get; set; }
    public virtual Tenant Tenant { get; set; }
    public ICollection<CorrectiveAction> CorrectiveActions { get; set; }

    public int? OriginalAuditInstanceId { get; set; }

    [ForeignKey("OriginalAuditInstanceId")]
    public AuditInstance? OriginalAuditInstance { get; set; }
  }



  public class CorrectiveAction

  {

    [Key]

    public int CorrectiveActionId { get; set; }



    public int AuditInstanceId { get; set; }

    [ForeignKey("AuditInstanceId")]

    public virtual AuditInstance AuditInstance { get; set; }



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



    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }



    public DateTime? CompletionDate { get; set; }

    public string? CompletedBy { get; set; }



  }

}





namespace AspnetCoreMvcFull.Models

{

  public class AuditResponse

  {

    public int AuditResponseId { get; set; }

    public int AuditInstanceId { get; set; }

    public int FormItemId { get; set; }



    public string FormItemQuestion { get; set; }

    public string ResponseValue { get; set; }

    public int? ScoredValue { get; set; }

    public int? MaxPossibleScore { get; set; }



    public int? LoopIndex { get; set; }

    public int? OriginalAuditResponseId { get; set; }



    public AuditInstance AuditInstance { get; set; }

    public FormItem FormItem { get; set; }

    public ICollection<CorrectiveAction> CorrectiveActions { get; set; }

  }

}



namespace AspnetCoreMvcFull.Models

{

  public class ItemOption

  {

    public string Value { get; set; }

    public string Text { get; set; }

    public int? Score { get; set; }

    public int Order { get; set; }

  }

}
