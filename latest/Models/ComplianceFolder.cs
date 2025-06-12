using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class ComplianceFolder
  {
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty; // Initialize to prevent null warnings

    // Change from string ComplianceType to int ComplianceCategoryId
    [Required]
    [Display(Name = "Compliance Type")]
    public int ComplianceCategoryId { get; set; } // <--- CHANGED TO INT FK

    [StringLength(1000)]
    public string? Description { get; set; } 

    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty; // Initialize to prevent null warnings

    public FolderStatus Status { get; set; }

    // ADD THESE TWO PROPERTIES:
    public DateTime? LastModifiedDate { get; set; } // Nullable, as it might not be modified initially
    public string? LastModifiedBy { get; set; } // Nullable



    // --- Multi-tenancy properties ---
    [Required]
    public string TenantId { get; set; } // Foreign key to Tenant
    [Required]
    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!; // Navigation property
    // Navigation properties
    public virtual ComplianceCategory? ComplianceCategory { get; set; } // <--- NEW Navigation Property
    public virtual ICollection<Document> Documents { get; set; } = new HashSet<Document>();
    public virtual ICollection<RequiredDocument> RequiredDocuments { get; set; } = new HashSet<RequiredDocument>();

    public ComplianceFolder()
    {
      CreatedDate = DateTime.Now;
      Status = FolderStatus.Active;
    }
  }

  public enum FolderStatus
  {
    Active,
    Archived,
    Completed
  }
}
