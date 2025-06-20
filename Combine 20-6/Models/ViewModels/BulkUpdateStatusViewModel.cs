// Models/ViewModels/BulkUpdateStatusViewModel.cs
namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class BulkUpdateStatusViewModel
  {
    public int[] FolderIds { get; set; }
    public string NewStatus { get; set; }
  }
}
