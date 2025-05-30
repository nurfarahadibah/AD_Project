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
      // Configure the one-to-many relationship between RequiredDocument and Document
      // (One RequiredDocument can have many Documents)
      modelBuilder.Entity<Document>() // Start from the 'many' side (Document)
          .HasOne(d => d.RequiredDocument) // A Document has one RequiredDocument
          .WithMany(rd => rd.Documents)    // A RequiredDocument has many Documents (collection)
          .HasForeignKey(d => d.RequiredDocumentId) // The foreign key is in the Document model
          .IsRequired(false); // Make the foreign key nullable if RequiredDocumentId is nullable in Document model

      // Configure the one-to-many relationship between ComplianceFolder and RequiredDocument
      // (One ComplianceFolder can have many RequiredDocuments)
      modelBuilder.Entity<RequiredDocument>()
          .HasOne(rd => rd.ComplianceFolder)
          .WithMany(cf => cf.RequiredDocuments)
          .HasForeignKey(rd => rd.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade); // Adjust OnDelete behavior as needed

      // Configure the one-to-many relationship between ComplianceFolder and Document
      // (One ComplianceFolder can have many Documents)
      modelBuilder.Entity<Document>()
          .HasOne(d => d.ComplianceFolder)
          .WithMany(cf => cf.Documents)
          .HasForeignKey(d => d.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade); // Adjust OnDelete behavior as needed



    }
  }
}
