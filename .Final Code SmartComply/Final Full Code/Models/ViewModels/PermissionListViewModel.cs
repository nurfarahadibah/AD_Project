// File: Models/ViewModels/PermissionListViewModel.cs
// ViewModel for displaying a single permission in the permissions list.

using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class PermissionListViewModel
  {
    public string Name { get; set; }
    // Changed from int AssignedToCount to List<string> AssignedToRoles
    public List<string> AssignedToRoles { get; set; } = new List<string>();
    public DateTime CreatedDate { get; set; }

    // Helper property to get the count for display purposes
    // This is a read-only property; its value is derived from AssignedToRoles.Count
    public int AssignedToCount => AssignedToRoles.Count;
  }
}
