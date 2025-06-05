using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class CreateFolderViewModel
  {
    [Required]
    [StringLength(200)]
    [Display(Name = "Folder Name")]
    public string Name { get; set; }

    [Required]
    [Display(Name = "Compliance Type")]
    public string ComplianceType { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }


    [Display(Name = "Required Documents")]
    public List<RequiredDocumentViewModel> RequiredDocuments { get; set; }

    public CreateFolderViewModel()
    {
      RequiredDocuments = new List<RequiredDocumentViewModel>();
    }
  }

  public class RequiredDocumentViewModel
  {
    [Required]
    [StringLength(200)]
    public string DocumentName { get; set; } = string.Empty; 

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;
  }
}
