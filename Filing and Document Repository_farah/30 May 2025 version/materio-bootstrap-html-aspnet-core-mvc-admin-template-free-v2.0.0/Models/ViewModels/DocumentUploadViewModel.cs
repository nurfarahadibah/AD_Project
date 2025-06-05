// Models/ViewModels/DocumentUploadViewModel.cs
using Microsoft.AspNetCore.Http; // For IFormFile
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class DocumentUploadViewModel
  {
    [Required]
    public int ComplianceFolderId { get; set; }
    public string FolderName { get; set; } // Display purposes

    [Required(ErrorMessage = "Please select a file.")]
    public IFormFile File { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    // NEW: Property to hold the selected RequiredDocumentId
    public int? SelectedRequiredDocumentId { get; set; }

    // Optional: List for the dropdown in the view (populated by controller)
    public List<object>? AvailableRequiredDocuments { get; set; }
  }
}
