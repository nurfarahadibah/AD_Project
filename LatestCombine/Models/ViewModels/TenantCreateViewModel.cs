// File: Models/ViewModels/TenantCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class TenantCreateViewModel
  {
    [Required(ErrorMessage = "Tenant Name is required.")]
    [StringLength(100, ErrorMessage = "Tenant Name cannot exceed 100 characters.")]
    [Display(Name = "Tenant Name")]
    public string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
  }
}
