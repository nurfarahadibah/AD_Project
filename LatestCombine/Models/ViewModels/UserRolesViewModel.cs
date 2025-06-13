namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class UserRolesViewModel
  {
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; } = new List<string>(); // Initialize to avoid null reference
  }
}

