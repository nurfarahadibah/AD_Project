using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class GradeConfigurationViewModel
  {
    [Required(ErrorMessage = "Form Type ID is required.")]
    public int FormTypeId { get; set; }

    public List<GradeRangeViewModel> GradeRanges { get; set; } = new List<GradeRangeViewModel>();
  }

  public class GradeRangeViewModel
  {
    [Required(ErrorMessage = "Min Percentage is required.")]
    [Range(0, 100, ErrorMessage = "Min percentage must be between 0 and 100.")]
    public int MinPercentage { get; set; }

    [Required(ErrorMessage = "Max Percentage is required.")]
    [Range(0, 100, ErrorMessage = "Max percentage must be between 0 and 100.")]
    public int MaxPercentage { get; set; }

    [Required(ErrorMessage = "Grade Letter is required.")]
    [StringLength(10, ErrorMessage = "Grade letter cannot exceed 10 characters.")]
    public string GradeLetter { get; set; }
  }
}
