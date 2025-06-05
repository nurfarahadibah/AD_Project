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
    public DbSet<JenisForm> JenisForms { get; set; }
    public DbSet<FormSection> FormSections { get; set; }
    public DbSet<FormItem> FormItems { get; set; }

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
      // JenisForm relationships
      modelBuilder.Entity<FormSection>()
          .HasOne(s => s.JenisForm)
          .WithMany(f => f.Sections)
          .HasForeignKey(s => s.FormTypeId)
          .OnDelete(DeleteBehavior.Cascade);

      // FormSection relationships
      modelBuilder.Entity<FormItem>()
          .HasOne(i => i.Section)
          .WithMany(s => s.Items)
          .HasForeignKey(i => i.SectionId)
          .OnDelete(DeleteBehavior.Cascade);


    }
  }
}
