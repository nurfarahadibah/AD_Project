using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Not strictly needed for basic EF Core, but good for clarity

namespace AspnetCoreMvcFull.Models
{
  public class GradeRange
  {
    public int GradeRangeId { get; set; } // Primary Key

    // Foreign Key to GradeConfiguration
    public int GradeConfigurationId { get; set; }
    public virtual GradeConfiguration GradeConfiguration { get; set; } // Navigation property back to GradeConfiguration

    [Required]
    [Range(0, 100, ErrorMessage = "Min Percentage must be between 0 and 100.")]
    public int MinPercentage { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Max Percentage must be between 0 and 100.")]
    public int MaxPercentage { get; set; }

    [Required]
    [StringLength(10, ErrorMessage = "Grade letter cannot exceed 10 characters.")]
    public string GradeLetter { get; set; }

    public int Order { get; set; } // To maintain the order of grades (e.g., A, B, C)
  }
}
