using System.ComponentModel.DataAnnotations;
namespace AspnetCoreMvcFull.Models
{
  public class JenisForm
  {
    [Key]
    public int FormTypeId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public int? FrameworkId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<FormSection> Sections { get; set; } = new List<FormSection>();
  }
}

