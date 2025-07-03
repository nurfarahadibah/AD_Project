using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class CreateJenisFormViewModel
  {
    [Required(ErrorMessage = "Form Name is required.")]
    [StringLength(255, ErrorMessage = "Form Name cannot exceed 255 characters.")]
    [Display(Name = "Form Name")]
    public string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Compliance Category")]
    public int? ComplianceCategoryId { get; set; } // Foreign key for ComplianceCategory

    // Property to hold the list of compliance categories for the dropdown
    public IEnumerable<SelectListItem>? ComplianceCategories { get; set; }
  }
}
