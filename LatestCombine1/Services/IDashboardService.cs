using AspnetCoreMvcFull.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using AspnetCoreMvcFull.Models; // Needed for User/Roles context if passed to service

namespace AspnetCoreMvcFull.Services
{
  public interface IDashboardService
  {
    /// <summary>
    /// Retrieves all data required for the main dashboard view.
    /// </summary>
    /// <param name="user">The current authenticated user.</param>
    /// <returns>A DashboardViewModel populated with relevant data.</returns>
    Task<DashboardViewModels> GetDashboardDataAsync(ApplicationUser user);
  }
}
