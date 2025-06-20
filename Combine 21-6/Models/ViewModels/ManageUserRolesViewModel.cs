using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class ManageUserRolesViewModel
  {
    public string UserId { get; set; }

    [Display(Name = "Username")]
    public string UserName { get; set; }

    public List<RoleSelectionViewModel> RoleSelections { get; set; } = new List<RoleSelectionViewModel>();
  }

  public class RoleSelectionViewModel
  {
    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public bool IsSelected { get; set; }
  }
}
