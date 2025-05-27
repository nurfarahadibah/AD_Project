using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class ComplianceFolder
  {
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ComplianceType { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<string> AssignedUsers { get; set; } = new List<string>();

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
  }
}
