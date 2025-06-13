using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models
{
  public class Tenant
  {
    [Key]
    [Required]
    public string Id { get; set; } // Using string for TenantId for flexibility (e.g., GUID, short code)

    [Required]
    [StringLength(255)]
    public string Name { get; set; } // Company/Institution/Outlet Name

    [StringLength(1000)]
    public string Description { get; set; } // Company/Institution/Outlet Description

    // Optional: Add more fields as needed for tenant branding or contact
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LogoUrl { get; set; } // Path or URL to the tenant's logo

    public bool IsActive { get; set; } = true; // Is the tenant active?
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? LastModifiedDate { get; set; }

    // Navigation properties (if you link users or other entities directly)
    // public ICollection<ApplicationUser> Users { get; set; } // Assuming you have an ApplicationUser model
  }
}
