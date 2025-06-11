using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  // ViewModel for displaying a single role as a card
  public class RoleCardViewModel
  {
    public string Id { get; set; } // Role ID
    public string Name { get; set; } // Role Name (e.g., "Admin", "Editor")
    public int UserCount { get; set; } // Number of users assigned to this role
                                       // You can add a list of user avatars/image paths here if you want to display them
                                       // public List<string> UserAvatarUrls { get; set; } = new List<string>();
                                       // For now, we'll just display the count
  }

  // ViewModel for a single permission item (e.g., "User Management - Read")
  public class PermissionItemViewModel
  {
    public string Name { get; set; } // Name of the permission (e.g., "User Management Read")
    public bool IsSelected { get; set; } // Whether this permission is selected for a role
  }

  // ViewModel for the "Add/Edit Role" modal, including permissions
  public class AddEditRoleViewModel
  {
    public string Id { get; set; } // Role ID (null for new role)

    [Required]
    [Display(Name = "Role Name")]
    public string Name { get; set; } // New or existing role name

    // This list will hold all possible permissions, with IsSelected indicating if it's for this role
    public List<PermissionItemViewModel> Permissions { get; set; } = new List<PermissionItemViewModel>();
  }

  // ViewModel for displaying a list of users with their roles (used in the table below role cards)
  public class UserRolesListViewModel
  {
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
  }

  // ViewModel for the separate Permissions List page
  //public class PermissionListViewModel
  //{
  //  public string Name { get; set; } // e.g., "User Management Read"
  //  public int AssignedToCount { get; set; } // Number of roles this permission is assigned to
  //  public DateTime CreatedDate { get; set; } // Placeholder
  //}
}
