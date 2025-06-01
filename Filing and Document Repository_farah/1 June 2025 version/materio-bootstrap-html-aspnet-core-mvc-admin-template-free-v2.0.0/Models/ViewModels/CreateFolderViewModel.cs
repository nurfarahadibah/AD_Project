using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Add this for SelectList

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class CreateFolderViewModel
  {
    public int Id { get; set; } // <--- ADD THIS LINE

    [Required]
    [StringLength(200)]
    [Display(Name = "Folder Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a Compliance Type.")]
    [Display(Name = "Compliance Type")]
    public int ComplianceCategoryId { get; set; } // <--- CHANGED TO INT ID

    // Property to hold the dropdown items
    public SelectList? ComplianceCategories { get; set; } // <--- NEW for dropdown

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;


    [Display(Name = "Required Documents")]
    public List<RequiredDocumentViewModel> RequiredDocuments { get; set; }

    public CreateFolderViewModel()
    {
      RequiredDocuments = new List<RequiredDocumentViewModel>();
    }
  }

  public class RequiredDocumentViewModel
  {
    public int Id { get; set; } // <--- ADD THIS LINE

    [Required]
    [StringLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;
  }
}
