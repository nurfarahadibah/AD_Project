using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class ComplianceFolder
  {
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [Required]
    [StringLength(100)]
    public string ComplianceType { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }

    [StringLength(100)]
    public string CreatedBy { get; set; }

    public FolderStatus Status { get; set; }

    // Navigation properties
    public virtual ICollection<Document> Documents { get; set; }
    public virtual ICollection<RequiredDocument> RequiredDocuments { get; set; }

    public ComplianceFolder()
    {
      Documents = new HashSet<Document>();
      RequiredDocuments = new HashSet<RequiredDocument>();
      CreatedDate = DateTime.Now;
      Status = FolderStatus.Active;
      Name = string.Empty; // <--- Initialize Name here
      Description = string.Empty; // Also initialize Description if it's non-nullable
    }
  }

  public enum FolderStatus
  {
    Active,
    Archived,
    InReview,
    Completed
  }
}
