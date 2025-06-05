using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class ComplianceCategory
  {
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Compliance Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    // Navigation property to link back to ComplianceFolders
    public virtual ICollection<ComplianceFolder> ComplianceFolders { get; set; } = new List<ComplianceFolder>();
  }
}
