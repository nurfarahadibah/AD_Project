using System;
using System.ComponentModel.DataAnnotations;
using AspnetCoreMvcFull.Models;

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
    public string Description { get; set; }

    public DateTime UploadDate { get; set; }

    [StringLength(100)]
    public string UploadedBy { get; set; }

    public DocumentStatus Status { get; set; }

    // Foreign key
    public int ComplianceFolderId { get; set; }
    public virtual ComplianceFolder ComplianceFolder { get; set; }

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
