using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class RolesIndexViewModel
  {
    // Data for the role cards section
    public List<RoleCardViewModel> RoleCards { get; set; } = new List<RoleCardViewModel>();

    // Data for the "Total users with their roles" table
    public List<UserRolesListViewModel> UserRolesList { get; set; } = new List<UserRolesListViewModel>();

    // Model for the Add/Edit Role modal form
    public AddEditRoleViewModel AddEditRoleForm { get; set; } = new AddEditRoleViewModel();

    // New: List of roles for the "Select Role" dropdown filter
    public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();

    // New: Properties to hold current filter values for the user table
    public int ShowEntries { get; set; } = 10; // Default to 10 entries
    public string SearchUser { get; set; } = ""; // Default empty search
    public string SelectedRole { get; set; } = ""; // Default empty (all roles)

    // New: Pagination properties
    public int TotalUsers { get; set; }
    public int CurrentPage { get; set; } = 1; // Default to page 1
    public int TotalPages => (int)Math.Ceiling((double)TotalUsers / ShowEntries);
  }
}
