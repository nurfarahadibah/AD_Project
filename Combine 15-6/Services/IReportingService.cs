// Services/IReportingService.cs
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public interface IReportingService
  {
    // Method for tenant-specific reports (User, Manager, Admin)
    Task<ReportingViewModel> GetTenantReportsAsync(ApplicationUser user);

    // Method for SuperAdmin global reports
    Task<ReportingViewModel> GetGlobalReportsAsync();
  }
}
