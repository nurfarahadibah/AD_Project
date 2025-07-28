// Data/DataSeeder.cs (or Utilities/DataSeeder.cs)

using AspnetCoreMvcFull.Data; // Ensure you have this using directive for AppDbContext
using AspnetCoreMvcFull.Models; // Ensure you have this using directive for your models
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Required for .AnyAsync() and .AddRange(), .SaveChangesAsync()

namespace AspnetCoreMvcFull.Data // Or whatever namespace fits your project structure
{
  public static class DataSeeder
  {
    public static async Task SeedData(AppDbContext context)
    {
      // Seed Tenants
      if (!await context.Tenants.AnyAsync()) // Use AnyAsync() for async operations
      {
        context.Tenants.AddRange(
            new Tenant { Id = "tenantA", Name = "Tenant Alpha Org", Description = "First tenant for testing.", IsActive = true, CreatedDate = DateTime.Now },
            new Tenant { Id = "tenantB", Name = "Tenant Beta Corp", Description = "Second tenant for testing.", IsActive = true, CreatedDate = DateTime.Now }
        );
        await context.SaveChangesAsync();
      }

      // Seed Compliance Categories for Tenant Alpha
      // We'll keep the AnyAsync() check commented out for now to force insertion attempts for debugging
      // if (!await context.ComplianceCategories.AnyAsync(c => c.TenantId == "tenantA"))
      
      // You can add more seeding logic here for folders and documents if needed
      // just remember to assign the correct TenantId to each entity.
    }
  }
}
