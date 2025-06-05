using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Make sure this is included if not already

namespace AspnetCoreMvcFull.Models
{
  public enum FormStatus
  {
    Draft = 1,
    Published = 2,
    Archived = 3
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

    public virtual ICollection<FormSection> Sections { get; set; } = new List<FormSection>();
  }
}
