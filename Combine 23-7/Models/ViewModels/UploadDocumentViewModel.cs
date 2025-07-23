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
    
    // Change from IFormFile to IFormFile[] to allow multiple file uploads
    public IFormFile[] Files { get; set; } = default!;
    public bool IsRequiredDocumentUpload => RequiredDocumentId.HasValue;
  }
}
