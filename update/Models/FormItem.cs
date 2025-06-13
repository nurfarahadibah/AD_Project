// AspnetCoreMvcFull.Models/FormItem.cs
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public enum ItemType
  {
    Text = 1,
    Number = 2,
    Checkbox = 3,
    Radio = 4,
    Dropdown = 5,
    Textarea = 6,
    File = 7,
    Signature = 8
  }

  public class FormItem
  {
    [Key]
    public int ItemId { get; set; }

    public int SectionId { get; set; }

    [Required]
    [StringLength(500)]
    public string Question { get; set; }

    public ItemType ItemType { get; set; }

    public string? OptionsJson { get; set; }

    public bool IsRequired { get; set; }

    public int? MaxScore { get; set; }

    public int Order { get; set; }

    public bool HasLooping { get; set; }

    public int? LoopCount { get; set; }

    [StringLength(100)]
    public string? LoopLabel { get; set; }

    public virtual FormSection Section { get; set; }
  }
}
