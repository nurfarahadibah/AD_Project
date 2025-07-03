using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services; 

namespace AspnetCoreMvcFull.Data
{
  public class AppDbContext : DbContext
  {
    private readonly ITenantService _tenantService;

    // Constructor to inject DbContextOptions and ITenantService
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantService tenantService)
        : base(options)
    {
      _tenantService = tenantService;
    }

    // --- DbSet Properties ---
    // These properties represent the tables in your database
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<ComplianceFolder> ComplianceFolders { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<RequiredDocument> RequiredDocuments { get; set; }
    public DbSet<JenisForm> JenisForms { get; set; } // From first snippet
    public DbSet<FormSection> FormSections { get; set; } // From first snippet
    public DbSet<FormItem> FormItems { get; set; } // From first snippet
    public DbSet<ComplianceCategory> ComplianceCategories { get; set; }
    public DbSet<AuditInstance> AuditInstances { get; set; }
    public DbSet<AuditResponse> AuditResponses { get; set; }
    public DbSet<CorrectiveAction> CorrectiveActions { get; set; }

    // --- OnModelCreating Method ---
    // This method is used to configure the schema of your database tables,
    // including relationships, constraints, and global query filters.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder); // Always call the base implementation

      // --- Configure relationships with Tenant ---
      // A ComplianceFolder belongs to a Tenant, and if a Tenant is deleted,
      // its associated ComplianceFolders will also be deleted (Cascade).
      modelBuilder.Entity<ComplianceFolder>()
          .HasOne(f => f.Tenant)
          .WithMany() // Assuming Tenant does not have a navigation property back to ComplianceFolder
          .HasForeignKey(f => f.TenantId)
          .IsRequired()
          .OnDelete(DeleteBehavior.Cascade);

      // A ComplianceCategory belongs to a Tenant. If a Tenant is deleted,
      // deletion of ComplianceCategories is restricted to prevent accidental data loss.
      // This is a common pattern to ensure master data is not easily removed.
      modelBuilder.Entity<ComplianceCategory>()
          .HasOne(cc => cc.Tenant)
          .WithMany() // Assuming Tenant does not have a navigation property back to ComplianceCategory
          .HasForeignKey(cc => cc.TenantId)
          .IsRequired()
          .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to prevent cascade delete issues

      // --- Configure relationships for ComplianceFolder and Documents ---
      // A Document belongs to a ComplianceFolder, deleting a folder cascades to its documents.
      modelBuilder.Entity<Document>()
          .HasOne(d => d.ComplianceFolder)
          .WithMany(f => f.Documents)
          .HasForeignKey(d => d.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade);

      // A RequiredDocument belongs to a ComplianceFolder, deleting a folder cascades to its required documents.
      modelBuilder.Entity<RequiredDocument>()
          .HasOne(rd => rd.ComplianceFolder)
          .WithMany(cf => cf.RequiredDocuments)
          .HasForeignKey(rd => rd.ComplianceFolderId)
          .OnDelete(DeleteBehavior.Cascade);

      // A Document can optionally be associated with a RequiredDocument.
      // When a RequiredDocument is deleted, its associated Documents are not affected (NoAction).
      // This setup implies that the Document has the RequiredDocumentId foreign key.
      modelBuilder.Entity<Document>()
          .HasOne(d => d.RequiredDocument)
          .WithMany(rd => rd.Documents) // Assuming RequiredDocument has ICollection<Document> Documents
          .HasForeignKey(d => d.RequiredDocumentId)
          .IsRequired(false) // RequiredDocumentId can be null
          .OnDelete(DeleteBehavior.NoAction); // Prevents cascade delete from RequiredDocument to Document

      // --- Configure relationships for Forms ---
      // A FormSection belongs to a JenisForm (Form Type).
      // Deleting a JenisForm cascades to its sections.
      modelBuilder.Entity<FormSection>()
          .HasOne(s => s.JenisForm)
          .WithMany(f => f.Sections)
          .HasForeignKey(s => s.FormTypeId)
          .OnDelete(DeleteBehavior.Cascade);

      // A FormItem belongs to a FormSection.
      // Deleting a FormSection cascades to its items.
      modelBuilder.Entity<FormItem>()
          .HasOne(i => i.Section)
          .WithMany(s => s.Items)
          .HasForeignKey(i => i.SectionId)
          .OnDelete(DeleteBehavior.Cascade);

      // --- Configure relationship between JenisForm and ComplianceCategory ---
      // A JenisForm can optionally belong to a ComplianceCategory.
      // Deleting a ComplianceCategory will restrict (prevent) the deletion if any JenisForm is linked to it.
      modelBuilder.Entity<JenisForm>()
          .HasOne(jf => jf.ComplianceCategory)
          .WithMany() // Assuming ComplianceCategory doesn't have a navigation property back to JenisForm
          .HasForeignKey(jf => jf.ComplianceCategoryId)
          .IsRequired(false) // Make the foreign key nullable if ComplianceCategoryId is nullable in JenisForm
          .OnDelete(DeleteBehavior.Restrict); // Or .SetNull if preferred, depending on business logic

      // --- Configure relationship between ComplianceFolder and ComplianceCategory ---
      // A ComplianceFolder is required to have a ComplianceCategory.
      // The cascade behavior here is not problematic as ComplianceCategory is a parent.
      modelBuilder.Entity<ComplianceFolder>()
          .HasOne(cf => cf.ComplianceCategory)
          .WithMany(cc => cc.ComplianceFolders) // Assuming ComplianceCategory has ICollection<ComplianceFolder> ComplianceFolders
          .HasForeignKey(cf => cf.ComplianceCategoryId)
          .IsRequired(); // ComplianceCategoryId cannot be null

      // AuditInstance has many AuditResponses
      modelBuilder.Entity<AuditInstance>()
          .HasMany(ai => ai.AuditResponses)
          .WithOne(ar => ar.AuditInstance)
          .HasForeignKey(ar => ar.AuditInstanceId)
          .OnDelete(DeleteBehavior.Cascade);

      // AuditInstance refers to a JenisForm (the form template it's an instance of)
      modelBuilder.Entity<AuditInstance>()
          .HasOne(ai => ai.JenisForm)
          .WithMany()
          .HasForeignKey(ai => ai.FormTypeId)
          .OnDelete(DeleteBehavior.Restrict);

      // AuditResponse refers to a FormItem (the specific question it's a response to)
      modelBuilder.Entity<AuditResponse>()
          .HasOne(ar => ar.FormItem)
          .WithMany()
          .HasForeignKey(ar => ar.FormItemId)
          .OnDelete(DeleteBehavior.Restrict);

      // --- NEW: AuditInstance to Tenant Relationship ---
      modelBuilder.Entity<AuditInstance>()
          .HasOne(ai => ai.Tenant)
          .WithMany() // Assuming Tenant doesn't have a direct collection of AuditInstances
          .HasForeignKey(ai => ai.TenantId)
          .IsRequired()
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<CorrectiveAction>()
                .HasOne(ca => ca.AuditInstance)
                .WithMany(ai => ai.CorrectiveActions) // Or WithMany(ai => ai.CorrectiveActions) if you add a collection to AuditInstance
                .HasForeignKey(ca => ca.AuditInstanceId)
                .OnDelete(DeleteBehavior.Restrict);

      modelBuilder.Entity<CorrectiveAction>()
                .HasOne(ca => ca.AuditResponse)
                .WithMany(ar => ar.CorrectiveActions) // Or WithMany(ar => ar.CorrectiveActions) if you add a collection to AuditResponse
                .HasForeignKey(ca => ca.AuditResponseId)
                .OnDelete(DeleteBehavior.Restrict);


      // --- Global Query Filters ---
      // These filters automatically apply to all queries for these entities,
      // ensuring that only data relevant to the current tenant is retrieved.
      modelBuilder.Entity<ComplianceFolder>().HasQueryFilter(
          f => f.TenantId == _tenantService.GetCurrentTenantId()
      );
      modelBuilder.Entity<ComplianceCategory>().HasQueryFilter(
          cc => cc.TenantId == _tenantService.GetCurrentTenantId()
      );

      modelBuilder.Entity<JenisForm>().HasQueryFilter(
          f => f.TenantId == _tenantService.GetCurrentTenantId()
      );

      modelBuilder.Entity<AuditInstance>().HasQueryFilter(
                ai => ai.TenantId == _tenantService.GetCurrentTenantId()
      );

      modelBuilder.Entity<CorrectiveAction>().HasQueryFilter(
                ca => ca.AuditInstance.TenantId == _tenantService.GetCurrentTenantId() // Filter CorrectiveActions by AuditInstance's TenantId
            );

      // Add other global query filters if other entities also have TenantId
      // Example: modelBuilder.Entity<Document>().HasQueryFilter(
      //     d => d.ComplianceFolder.TenantId == _tenantService.GetCurrentTenantId()
      // );
    }
  }
}
