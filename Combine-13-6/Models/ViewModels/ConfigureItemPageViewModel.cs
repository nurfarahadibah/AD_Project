using AspnetCoreMvcFull.Models; // Ensure you have access to ItemType
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class ConfigureItemPageViewModel
  {
    public int FormTypeId { get; set; } // To link back to the main form
    public string FormName { get; set; } = string.Empty; // To display form name
    public string FormDescription { get; set; } = string.Empty; // To display form description

    public ItemConfigViewModel ItemConfig { get; set; } = new ItemConfigViewModel();
  }

  // Reuse your existing ItemConfigViewModel from FormBuilderViewModel.cs
  // No changes needed for ItemConfigViewModel itself.
}
