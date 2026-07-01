using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolDataIntegration.Api.Data;
using SchoolDataIntegration.Api.Models;
using SchoolDataIntegration.Api.Transformer;
using Xunit;

namespace SchoolDataIntegration.Tests.Transformer;

public class StudentTransformerTests
{
    private static SchoolDbContext NewInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SchoolDbContext(options);
    }

    [Fact]
    public async Task TransformAsync_CreatesNewStudent_WhenNoneExists()
    {
        await using var db = NewInMemoryContext(nameof(TransformAsync_CreatesNewStudent_WhenNoneExists));
        var transformer = new StudentTransformer(db, NullLogger<StudentTransformer>.Instance);

        var students = new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
        };

        var result = await transformer.TransformAsync("SCH-1", students);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);

        var saved = await db.Students.SingleAsync();
        Assert.Equal("Ada", saved.FirstName);
        Assert.Equal("SCH-1", saved.SchoolId);
    }

    [Fact]
    public async Task TransformAsync_UpdatesExistingStudent_WhenExternalIdAlreadyExists()
    {
        await using var db = NewInMemoryContext(nameof(TransformAsync_UpdatesExistingStudent_WhenExternalIdAlreadyExists));
        db.Students.Add(new StudentRecord
        {
            SchoolId = "SCH-1",
            ExternalId = "S1",
            FirstName = "Ada",
            LastName = "OldLastName",
            YearLevel = "9",
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        });
        await db.SaveChangesAsync();

        var transformer = new StudentTransformer(db, NullLogger<StudentTransformer>.Instance);

        var students = new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
        };

        var result = await transformer.TransformAsync("SCH-1", students);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, await db.Students.CountAsync());

        var saved = await db.Students.SingleAsync();
        Assert.Equal("Lovelace", saved.LastName);
        Assert.Equal("10", saved.YearLevel);
    }

    [Fact]
    public async Task TransformAsync_SameExternalId_DifferentSchool_CreatesSeparateRecords()
    {
        await using var db = NewInMemoryContext(nameof(TransformAsync_SameExternalId_DifferentSchool_CreatesSeparateRecords));
        var transformer = new StudentTransformer(db, NullLogger<StudentTransformer>.Instance);

        await transformer.TransformAsync("SCH-1", new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
        });
        await transformer.TransformAsync("SCH-2", new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Someone", LastName = "Else", YearLevel = "8", Status = "Active" }
        });

        Assert.Equal(2, await db.Students.CountAsync());
    }

    [Fact]
    public async Task TransformAsync_ContinuesProcessing_WhenOneRecordFailsToPersist()
    {
        // A DbContext subclass that fails to save any change touching a
        // student flagged with ExternalId "FAIL", to prove the transformer
        // isolates and continues past a single bad record rather than
        // aborting or losing the rest of the batch.
        await using var db = new ThrowingOnFailIdDbContext(
            new DbContextOptionsBuilder<SchoolDbContext>()
                .UseInMemoryDatabase(nameof(TransformAsync_ContinuesProcessing_WhenOneRecordFailsToPersist))
                .Options);

        var transformer = new StudentTransformer(db, NullLogger<StudentTransformer>.Instance);

        var students = new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "FAIL", FirstName = "Broken", LastName = "Record", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "S2", FirstName = "Alan", LastName = "Turing", YearLevel = "11", Status = "Active" }
        };

        var result = await transformer.TransformAsync("SCH-1", students);

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
        Assert.Equal(2, await db.Students.CountAsync());
        Assert.DoesNotContain(await db.Students.ToListAsync(), s => s.ExternalId == "FAIL");
    }

    private class ThrowingOnFailIdDbContext : SchoolDbContext
    {
        public ThrowingOnFailIdDbContext(DbContextOptions<SchoolDbContext> options) : base(options) { }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var touchesFailRecord = ChangeTracker.Entries<StudentRecord>()
                .Any(e => e.Entity.ExternalId == "FAIL");

            if (touchesFailRecord)
            {
                throw new InvalidOperationException("Simulated persistence failure for ExternalId 'FAIL'.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
