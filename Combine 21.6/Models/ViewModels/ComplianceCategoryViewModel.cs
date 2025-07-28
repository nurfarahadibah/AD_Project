using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class ComplianceCategoryViewModel
  {
    public int Id { get; set; }

    [Display(Name = "Compliance Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Created Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "Last Modified By")]
    public string? LastModifiedBy { get; set; }

    [Display(Name = "Last Modified Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? LastModifiedDate { get; set; }

    // --- NEW: Status for Archiving/Unarchiving ---
    [Display(Name = "Status")]
    public int Status { get; set; }
  }

  public class CreateComplianceCategoryViewModel
  {
    [Required(ErrorMessage = "Compliance Name is required.")]
    [StringLength(100, ErrorMessage = "Compliance Name cannot exceed 100 characters.")]
    [Display(Name = "Compliance Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required.")]
    [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters.")]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
  }

  public class EditComplianceCategoryViewModel : CreateComplianceCategoryViewModel
  {
    public int Id { get; set; }
  }
}
