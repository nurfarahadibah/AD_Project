using System;
using System.ComponentModel.DataAnnotations;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Models
{
  public class RequiredDocument
  {
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string DocumentName { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public bool IsRequired { get; set; }
    public bool IsSubmitted { get; set; }

    public DateTime? SubmissionDate { get; set; }

    [StringLength(100)]
    public string SubmittedBy { get; set; }

    // Foreign key
    public int ComplianceFolderId { get; set; }
    public virtual ComplianceFolder ComplianceFolder { get; set; }

    // Optional reference to actual document
    public int? DocumentId { get; set; }
    public virtual Document Document { get; set; }

    public RequiredDocument()
    {
      IsRequired = true;
      IsSubmitted = false;
    }
  }
}
