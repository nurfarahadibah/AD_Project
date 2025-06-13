//AspnetCoreMvcFull.Models/ViewModels/FormBuilderViewModel.cs
using AspnetCoreMvcFull.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Ensure this is included
using Newtonsoft.Json; // Still needed if you're loading JSON into Options

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class FormBuilderViewModel
  {
    public JenisForm JenisForm { get; set; } = null!; // Initialize to avoid null warnings
    public List<FormSection> Sections { get; set; } = new();
    public FormItem? SelectedItem { get; set; }
  }

  public class CreateSectionViewModel
  {
    public int FormTypeId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty; // Initialize to avoid null warnings

    public string Description { get; set; } = string.Empty; // Initialize to avoid null warnings
  }


  public class ItemConfigViewModel
  {
    [Required] // ItemId is required for an update operation
    public int ItemId { get; set; }

    // ItemType is passed from the client as a hidden input and used for logic
    public ItemType ItemType { get; set; }

    [Required(ErrorMessage = "Question is required.")]
    [StringLength(500)]
    public string Question { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    // MaxScore can be null based on your FormItem definition
    [Range(0, int.MaxValue, ErrorMessage = "Max Score must be a non-negative number.")]
    public int? MaxScore { get; set; }

    public bool HasLooping { get; set; }

    // LoopCount can be null based on your FormItem definition
    [Range(1, int.MaxValue, ErrorMessage = "Loop Count must be at least 1.")]
    public int? LoopCount { get; set; }

    [StringLength(100)]
    public string? LoopLabel { get; set; }

    // This property directly binds to the "Options[0]", "Options[1]", etc. sent from the form.
    public List<string> Options { get; set; } = new List<string>();

    // This property is purely for passing the JSON string *from* the database *to* the view
    // (in the GET request for GetItemConfig) so it can be parsed into the 'Options' list
    // for display. It should NOT be used for direct binding on POST requests.
    public string? OptionsJson { get; set; }
  }
}
