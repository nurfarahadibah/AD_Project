// Services/TenantService.cs (TEMPORARY - FOR TESTING ONLY)
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Services
{
  public class TenantService : ITenantService
  {
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
      _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentTenantId()
    {
      // For testing: Temporarily hardcode a tenant ID
      // REMEMBER TO REMOVE THIS FOR PRODUCTION!
      // return "tenantA"; // Uncomment to test as tenantA
      // return "tenantB"; // Uncomment to test as tenantB

      // Your actual logic:
      var httpContext = _httpContextAccessor.HttpContext;
      if (httpContext == null || !httpContext.User.Identity.IsAuthenticated)
      {
        return null; // Or a default tenant ID if your app supports public access
      }

      var tenantIdClaim = httpContext.User.FindFirst("TenantId");
      return tenantIdClaim?.Value;
    }
  }
}
