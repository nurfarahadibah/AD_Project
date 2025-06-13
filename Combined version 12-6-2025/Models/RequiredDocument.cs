using System;
using System.Collections.Generic; // Add this using statement for ICollection
using System.ComponentModel.DataAnnotations;
// using AspnetCoreMvcFull.Models; // Already in namespace

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
    public string? SubmittedBy { get; set; } // Make it nullable if a document isn't "submitted" until it's linked

    // Foreign key to ComplianceFolder (already exists)
    public int ComplianceFolderId { get; set; }
    public virtual ComplianceFolder ComplianceFolder { get; set; }

    // REMOVE: public int? DocumentId { get; set; } and public virtual Document Document { get; set; }

    // NEW: Collection of Documents that fulfill this RequiredDocument
    // One RequiredDocument can have many Documents.
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();


    public RequiredDocument()
    {
      IsRequired = true;
      IsSubmitted = false;
    }
  }
}
