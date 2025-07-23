// Services/IDashboardService.cs
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public interface IDashboardService
  {
    Task<DashboardViewModel> GetDashboardDataAsync(ApplicationUser? user);
    Task<DashboardViewModel> GetGlobalSystemStatsAsync();
  }
}
