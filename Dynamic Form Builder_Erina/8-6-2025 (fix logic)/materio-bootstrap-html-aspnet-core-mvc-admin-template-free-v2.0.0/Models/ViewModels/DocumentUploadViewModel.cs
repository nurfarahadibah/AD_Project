using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class DocumentUploadViewModel
  {
    [Required]
    public int ComplianceFolderId { get; set; }

    [Required]
    public IFormFile File { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public string FolderName { get; set; }
  }
}
