using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class Document
  {
    public int Id { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; } = DateTime.Now;

    public int ComplianceFolderId { get; set; }

    public virtual ComplianceFolder? ComplianceFolder { get; set; } 
  }
}
