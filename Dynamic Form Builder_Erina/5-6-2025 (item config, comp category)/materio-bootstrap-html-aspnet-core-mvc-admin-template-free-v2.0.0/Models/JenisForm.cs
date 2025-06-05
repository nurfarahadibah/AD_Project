using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Make sure this is included if not already

namespace AspnetCoreMvcFull.Models
{
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

    // New foreign key property for ComplianceCategory
    public int? ComplianceCategoryId { get; set; }

    // New navigation property for ComplianceCategory
    public virtual ComplianceCategory? ComplianceCategory { get; set; }

    public virtual ICollection<FormSection> Sections { get; set; } = new List<FormSection>();
  }
}
