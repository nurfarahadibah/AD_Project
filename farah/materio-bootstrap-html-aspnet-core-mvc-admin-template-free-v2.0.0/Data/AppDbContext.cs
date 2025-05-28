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
    public DbSet<RequiredDocument> RequiredDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

      base.OnModelCreating(modelBuilder);
      // Configure relationships
      modelBuilder.Entity<Document>()
          .HasOne(d => d.ComplianceFolder)
          .WithMany(f => f.Documents)
          .HasForeignKey(d => d.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade);

     

      modelBuilder.Entity<RequiredDocument>()
          .HasOne(rd => rd.ComplianceFolder)
          .WithMany(f => f.RequiredDocuments)
          .HasForeignKey(rd => rd.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<RequiredDocument>()
          .HasOne(rd => rd.Document)
          .WithMany()
          .HasForeignKey(rd => rd.DocumentId)
          .OnDelete(DeleteBehavior.NoAction);

      
    }
  }
}
