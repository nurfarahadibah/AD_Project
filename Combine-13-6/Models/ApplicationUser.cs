// Example: YourProjectName/Models/ApplicationUser.cs
// OR YourProjectName/Data/ApplicationUser.cs
// OR YourProjectName/Areas/Identity/Data/ApplicationUser.cs (if scaffolded)

using Microsoft.AspNetCore.Identity;
using AspnetCoreMvcFull.Models;
using System.ComponentModel.DataAnnotations; // For [Required]
using System.ComponentModel.DataAnnotations.Schema; // For [ForeignKey] if you add a navigation property to Tenant

namespace AspnetCoreMvcFull.Models // Use the correct namespace for your project
{
  public class ApplicationUser : IdentityUser
  {
    // Add the TenantId property
    public string? TenantId { get; set; }

    // Optional: Navigation property to your Tenant model for easier access
    // Make sure your Tenant model is in the same namespace or referenced.
    [ForeignKey("TenantId")]
    public Tenant? Tenant { get; set; }

    // You can add other custom user properties here if needed
    // public string FullName { get; set; }
  }
}
