using System;
using System.ComponentModel.DataAnnotations;
// using AspnetCoreMvcFull.Models; // Already in namespace

namespace AspnetCoreMvcFull.Models
{
  public class Document
  {
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FileName { get; set; }

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; }

    [StringLength(100)]
    public string FileType { get; set; }

    public long FileSize { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; }

    [StringLength(100)]
    public string UploadedBy { get; set; }

    public DocumentStatus Status { get; set; }

    // Foreign key to ComplianceFolder (already exists)
    public int ComplianceFolderId { get; set; }
    public virtual ComplianceFolder ComplianceFolder { get; set; }

    // NEW: Foreign key to RequiredDocument
    // A Document can optionally fulfill a specific RequiredDocument
    // Use int? to make it nullable, as not all uploaded documents might fulfill a *specific* requirement.
    public int? RequiredDocumentId { get; set; }
    public virtual RequiredDocument? RequiredDocument { get; set; } // Navigation property

    public Document()
    {
      UploadDate = DateTime.Now;
      Status = DocumentStatus.Active;
    }
  }

  public enum DocumentStatus
  {
    Active,
    Archived,
    PendingReview,
    Approved,
    Rejected
  }
}
