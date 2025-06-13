using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class ComplianceCategory
  {
    [Key]
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

    // --- Multi-tenancy for ComplianceCategory ---
    [Required]
    public string TenantId { get; set; } // Foreign Key to Tenant
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!; // Navigation property to the Tenant

    // --- Auditing Properties (Added for consistency and tracking) ---
    [StringLength(256)]
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Default to current time

    [StringLength(256)]
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    // Navigation property to link back to ComplianceFolders
    public virtual ICollection<ComplianceFolder> ComplianceFolders { get; set; } = new List<ComplianceFolder>();

    // Optional: If JenisForm links to ComplianceCategory, add a navigation property here
    public virtual ICollection<JenisForm> JenisForms { get; set; } = new List<JenisForm>(); // Ensure JenisForm has ComplianceCategoryId
  }
}
