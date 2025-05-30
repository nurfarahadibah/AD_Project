
using AspnetCoreMvcFull.Models;
using System.ComponentModel.DataAnnotations;
namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class FormBuilderViewModel
  {
    public JenisForm JenisForm { get; set; }
    public List<FormSection> Sections { get; set; } = new();
    public FormItem? SelectedItem { get; set; }
  }

  public class CreateJenisFormViewModel
  {
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; }
  }

  public class CreateSectionViewModel
  {
    public int FormTypeId { get; set; }

    [Required]
    public string Title { get; set; }

    public string Description { get; set; }
  }

  public class ItemConfigViewModel
  {
    public int ItemId { get; set; }
    public string Question { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxScore { get; set; }
    public bool HasLooping { get; set; }
    public int? LoopCount { get; set; }
    public string? LoopLabel { get; set; }
    public List<string> Options { get; set; } = new();
  }
}

