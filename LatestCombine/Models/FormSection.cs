// AspnetCoreMvcFull.Models/FormSection.cs
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class FormSection
  {
    [Key]
    public int SectionId { get; set; }

    public int FormTypeId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public int Order { get; set; }

    public virtual JenisForm JenisForm { get; set; }
    public virtual ICollection<FormItem> Items { get; set; } = new List<FormItem>();
  }
}
