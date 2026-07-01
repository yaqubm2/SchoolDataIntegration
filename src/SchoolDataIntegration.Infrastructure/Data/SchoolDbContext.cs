using Microsoft.EntityFrameworkCore;
using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Infrastructure;

public class SchoolDbContext : DbContext
{
    #region constructor
    public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
    {
    }

    #endregion

    #region properties

    public DbSet<StudentRecord> Students => Set<StudentRecord>();

    public DbSet<ImportRecord> ImportRecords => Set<ImportRecord>();

    #endregion

    #region Methods
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentRecord>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasIndex(s => new { s.SchoolId, s.ExternalId }).IsUnique();

            entity.Property(s => s.ExternalId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.SchoolId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.FirstName).IsRequired().HasMaxLength(200);
            entity.Property(s => s.LastName).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ImportRecord>(entity =>
        {
            entity.HasKey(r => r.ImportId);
            entity.Property(r => r.SchoolId).IsRequired().HasMaxLength(100);
        });
    }

    #endregion
}
