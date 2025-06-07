using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;

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
    public DbSet<ComplianceCategory> ComplianceCategories { get; set; } // Added this DbSet

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

      // Configure the relationship between JenisForm and ComplianceCategory
      modelBuilder.Entity<JenisForm>()
          .HasOne(jf => jf.ComplianceCategory)
          .WithMany() // Assuming ComplianceCategory doesn't have a navigation property back to JenisForm
          .HasForeignKey(jf => jf.ComplianceCategoryId)
          .IsRequired(false) // Make the foreign key nullable if ComplianceCategoryId is nullable in JenisForm
          .OnDelete(DeleteBehavior.Restrict); // Or .SetNull if you prefer, depending on your business logic
    }
  }
}
