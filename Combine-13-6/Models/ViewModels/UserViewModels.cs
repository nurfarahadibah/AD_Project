using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectListItem in User

namespace AspnetCoreMvcFull.Models.ViewModels
{
  // ViewModel for displaying a user in a list
  public class UserViewModels
  {
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Roles")]
    // This will hold a comma-separated string of roles for display
    public string Roles { get; set; } = string.Empty;
  }

  // ViewModel for creating a new user
  public class UserCreateViewModel
  {
    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
  }

  // ViewModel for editing an existing user
  public class UserEditViewModel
  {
    public string Id { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Current Roles")]
    // This will display the roles the user currently has
    public IList<string> CurrentRoles { get; set; } = new List<string>();

    // Although you mentioned "not yet manage/assign" roles,
    // I'm including these properties for future extensibility.
    // You can uncomment and use them when you're ready to add role assignment.
    // public IList<string> SelectedRoles { get; set; } = new List<string>();
    // public IEnumerable<SelectListItem> AllRoles { get; set; } = new List<SelectListItem>();
  }
}
