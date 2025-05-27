using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class CreateFolderViewModel
  {
    [Required]
    [Display(Name = "Folder Name")]
    public string FolderName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Compliance Type")]
    public string ComplianceType { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Assigned Users")]
    public List<string> AssignedUsers { get; set; } = new List<string>();

    public List<string> AvailableUsers { get; set; } = new List<string>
        {
            "John Doe", "Jane Smith", "Mike Johnson",
            "Sarah Wilson", "Tom Brown", "Alice Cooper", "Bob Davis"
        };

    public List<string> ComplianceTypes { get; set; } = new List<string>
        {
            "SOX (Sarbanes-Oxley)", "ISO 27001", "GDPR",
            "Financial Audit", "Security Compliance",
            "Quality Management", "Risk Management", "Custom"
        };
  }
}
