// Data/AppDbContext.cs
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Data
{
  public class AppDbContext : DbContext
  {
    private readonly ITenantService _tenantService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantService tenantService)
        : base(options)
    {
      _tenantService = tenantService;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<ComplianceFolder> ComplianceFolders { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<RequiredDocument> RequiredDocuments { get; set; }
    public DbSet<ComplianceCategory> ComplianceCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Configure relationships for TenantId
      // Option 1: Keep Tenant -> ComplianceFolder as Cascade (most common for primary data)
      modelBuilder.Entity<ComplianceFolder>()
          .HasOne(f => f.Tenant)
          .WithMany() // Can be WithMany(t => t.ComplianceFolders) if you add ICollection<ComplianceFolder> to Tenant
          .HasForeignKey(f => f.TenantId)
          .IsRequired()
          .OnDelete(DeleteBehavior.Cascade); // Explicitly set or let it default to Cascade for required relationships

      // Option 2: Set Tenant -> ComplianceCategory to Restrict/NoAction
      // This is likely the one causing the conflict.
      modelBuilder.Entity<ComplianceCategory>()
          .HasOne(cc => cc.Tenant)
          .WithMany() // Can be WithMany(t => t.ComplianceCategories) if you add ICollection<ComplianceCategory> to Tenant
          .HasForeignKey(cc => cc.TenantId)
          .IsRequired()
          .OnDelete(DeleteBehavior.Restrict); // <--- CHANGE THIS TO RESTRICT OR NOACTION

      // Global Query Filters (remain the same)
      modelBuilder.Entity<ComplianceFolder>().HasQueryFilter(
          f => f.TenantId == _tenantService.GetCurrentTenantId()
      );
      modelBuilder.Entity<ComplianceCategory>().HasQueryFilter(
          cc => cc.TenantId == _tenantService.GetCurrentTenantId()
      );

      // Your existing configurations for Document, RequiredDocument, and ComplianceFolder relationships:
      // These can remain Cascade as they are children of ComplianceFolder
      modelBuilder.Entity<Document>()
          .HasOne(d => d.RequiredDocument)
          .WithMany(rd => rd.Documents)
          .HasForeignKey(d => d.RequiredDocumentId)
          .IsRequired(false);

      modelBuilder.Entity<RequiredDocument>()
          .HasOne(rd => rd.ComplianceFolder)
          .WithMany(cf => cf.RequiredDocuments)
          .HasForeignKey(rd => rd.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<Document>()
          .HasOne(d => d.ComplianceFolder)
          .WithMany(cf => cf.Documents)
          .HasForeignKey(d => d.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<ComplianceFolder>()
          .HasOne(cf => cf.ComplianceCategory)
          .WithMany(cc => cc.ComplianceFolders)
          .HasForeignKey(cf => cf.ComplianceCategoryId)
          .IsRequired(); // This relationship's cascade behavior is not problematic here
    }
  }
}
