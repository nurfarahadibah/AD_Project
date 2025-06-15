using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models
{
  public enum FormStatus
  {
    Draft = 1,
    Published = 2,
    Archived = 3,
    Revised = 4
  }

  public class JenisForm
  {
    [Key]
    public int FormTypeId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign key property for ComplianceCategory
    public int? ComplianceCategoryId { get; set; }

    // Navigation property for ComplianceCategory
    public virtual ComplianceCategory? ComplianceCategory { get; set; }

    // Property for Form Status
    public FormStatus Status { get; set; } = FormStatus.Draft; // Default status

    // --- NEW: TenantId property for multi-tenancy ---
    [Required]
    [StringLength(36)] // Assuming GUIDs for TenantId, adjust length if needed
    public string TenantId { get; set; }

    public virtual ICollection<FormSection> Sections { get; set; } = new List<FormSection>();
  }
}
