using AspnetCoreMvcFull.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ComplianceFolder> ComplianceFolders { get; set; }
    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ComplianceFolder>()
          .Property(e => e.AssignedUsers)
          .HasConversion(
              v => string.Join(',', v),
              v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
          );

      base.OnModelCreating(modelBuilder);
    }
  }
}
