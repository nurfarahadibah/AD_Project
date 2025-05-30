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
    public DbSet<ComplianceCategory> ComplianceCategories { get; set; } // <--- NEW


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

      // Configure the one-to-many relationship between ComplianceCategory and ComplianceFolder (NEW)
      modelBuilder.Entity<ComplianceFolder>()
          .HasOne(cf => cf.ComplianceCategory) // A ComplianceFolder has one ComplianceCategory
          .WithMany(cc => cc.ComplianceFolders) // A ComplianceCategory can have many ComplianceFolders
          .HasForeignKey(cf => cf.ComplianceCategoryId) // Foreign key in ComplianceFolder
          .IsRequired(); // ComplianceFolder must have a ComplianceCategory


                         // --- NEW: Seed ComplianceCategory Data ---
      modelBuilder.Entity<ComplianceCategory>().HasData(
          new ComplianceCategory { Id = 1, Name = "SOX (Sarbanes-Oxley)", Code = "SOX", Description = "Regulations for financial reporting." },
          new ComplianceCategory { Id = 2, Name = "ISO 27001", Code = "ISO", Description = "Information security management system standard." },
          new ComplianceCategory { Id = 3, Name = "GDPR", Code = "GDPR", Description = "General Data Protection Regulation." },
          new ComplianceCategory { Id = 4, Name = "Financial Audit", Code = "FINAUD", Description = "Internal or external financial review." },
          new ComplianceCategory { Id = 5, Name = "Security Compliance", Code = "SECCOMP", Description = "Adherence to security policies and standards." },
          new ComplianceCategory { Id = 6, Name = "Quality Management", Code = "QUALMAN", Description = "Ensuring consistent quality of products/services." },
          new ComplianceCategory { Id = 7, Name = "Risk Management", Code = "RISKMAN", Description = "Identifying and mitigating potential risks." },
          new ComplianceCategory { Id = 8, Name = "Custom", Code = "CUST", Description = "User-defined compliance type." }
      );


    }
  }
}
