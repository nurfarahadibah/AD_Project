using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // For IFormFile

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class UploadDocumentViewModel
  {
    public int ComplianceFolderId { get; set; }
    public string? ComplianceFolderName { get; set; } // To display in the view

    public int? RequiredDocumentId { get; set; }
    public string? RequiredDocumentName { get; set; } // To display in the view

    [Display(Name = "Document Description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select a file to upload.")]
    [Display(Name = "Select File")]
    public IFormFile File { get; set; } = default!; // Use default! to satisfy nullable warning, or initialize to null

    public bool IsRequiredDocumentUpload => RequiredDocumentId.HasValue;
  }
}
