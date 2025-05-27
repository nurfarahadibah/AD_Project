using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class DocumentRepositoryViewModel
  {
    public List<ComplianceFolder> Folders { get; set; } = new List<ComplianceFolder>();
    public string SearchQuery { get; set; } = string.Empty;
    public string SelectedFilter { get; set; } = string.Empty;
    public RepositoryStats Stats { get; set; } = new RepositoryStats();

    public CreateFolderViewModel NewFolder { get; set; } = new CreateFolderViewModel();
  }

  public class RepositoryStats
  {
    public int TotalFolders { get; set; }
    public int TotalDocuments { get; set; }
    public int PendingSubmissions { get; set; }
    public int ComplianceRate { get; set; }
  }
}
