using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolDataIntegration.Api.Builder;
using SchoolDataIntegration.Api.Data;
using SchoolDataIntegration.Api.Events;
using SchoolDataIntegration.Api.Filter;
using SchoolDataIntegration.Api.Models;
using SchoolDataIntegration.Api.Processing;
using SchoolDataIntegration.Api.Retriever;
using SchoolDataIntegration.Api.Transformer;
using Xunit;

namespace SchoolDataIntegration.Tests.Processing;

public class ProcessorTests
{
    private static SchoolDbContext NewInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SchoolDbContext(options);
    }

    private static Processor BuildProcessor(
        SchoolDbContext db,
        Mock<IStudentDataRetriever>? retriever = null,
        Mock<IFilePersister>? filePersister = null,
        Mock<IStudentBuilder>? builder = null,
        IStudentFilter? filter = null,
        IStudentTransformer? transformer = null,
        Mock<IEventPublisher>? eventPublisher = null)
    {
        retriever ??= new Mock<IStudentDataRetriever>();
        filePersister ??= new Mock<IFilePersister>();
        builder ??= new Mock<IStudentBuilder>();
        eventPublisher ??= new Mock<IEventPublisher>();
        filter ??= new StudentFilter(NullLogger<StudentFilter>.Instance);
        transformer ??= new StudentTransformer(db, NullLogger<StudentTransformer>.Instance);

        return new Processor(
            retriever.Object,
            filePersister.Object,
            builder.Object,
            filter,
            transformer,
            eventPublisher.Object,
            db,
            NullLogger<Processor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_UsesInlineStudents_WhenProvidedInRequest_AndSkipsRetriever()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_UsesInlineStudents_WhenProvidedInRequest_AndSkipsRetriever));
        var retriever = new Mock<IStudentDataRetriever>();
        var processor = BuildProcessor(db, retriever: retriever);

        var request = new StudentImportRequest
        {
            SchoolId = "SCH-1",
            Students = new List<StudentModel>
            {
                new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
            }
        };

        var summary = await processor.ProcessAsync(request);

        Assert.Equal(1, summary.Total);
        Assert.Equal(1, summary.Success);
        Assert.Equal(0, summary.Failed);
        retriever.Verify(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_FallsBackToRetriever_WhenNoInlineStudentsProvided()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_FallsBackToRetriever_WhenNoInlineStudentsProvided));

        var retrieved = new RetrievedData { Content = "csv-content", Format = DataFormat.Csv, Source = DataSourceKind.LocalCsvDrop };
        var retriever = new Mock<IStudentDataRetriever>();
        retriever.Setup(r => r.RetrieveAsync("SCH-1", It.IsAny<CancellationToken>())).ReturnsAsync(retrieved);

        var builder = new Mock<IStudentBuilder>();
        builder.Setup(b => b.Build(retrieved)).Returns(new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
        });

        var processor = BuildProcessor(db, retriever: retriever, builder: builder);

        var summary = await processor.ProcessAsync(new StudentImportRequest { SchoolId = "SCH-1" });

        Assert.Equal(1, summary.Total);
        Assert.Equal(1, summary.Success);
        retriever.Verify(r => r.RetrieveAsync("SCH-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_CachesRestApiSourcedData_ToLocalCsv()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_CachesRestApiSourcedData_ToLocalCsv));

        var retrieved = new RetrievedData { Content = "[]", Format = DataFormat.Json, Source = DataSourceKind.RestApi };
        var retriever = new Mock<IStudentDataRetriever>();
        retriever.Setup(r => r.RetrieveAsync("SCH-1", It.IsAny<CancellationToken>())).ReturnsAsync(retrieved);

        var builder = new Mock<IStudentBuilder>();
        var students = new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
        };
        builder.Setup(b => b.Build(retrieved)).Returns(students);

        var filePersister = new Mock<IFilePersister>();

        var processor = BuildProcessor(db, retriever: retriever, builder: builder, filePersister: filePersister);

        await processor.ProcessAsync(new StudentImportRequest { SchoolId = "SCH-1" });

        filePersister.Verify(f => f.PersistAsync("SCH-1", students, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotCache_WhenSourceIsLocalCsv()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_DoesNotCache_WhenSourceIsLocalCsv));

        var retrieved = new RetrievedData { Content = "csv", Format = DataFormat.Csv, Source = DataSourceKind.LocalCsvDrop };
        var retriever = new Mock<IStudentDataRetriever>();
        retriever.Setup(r => r.RetrieveAsync("SCH-1", It.IsAny<CancellationToken>())).ReturnsAsync(retrieved);

        var builder = new Mock<IStudentBuilder>();
        builder.Setup(b => b.Build(retrieved)).Returns(new List<StudentModel>());

        var filePersister = new Mock<IFilePersister>();
        var processor = BuildProcessor(db, retriever: retriever, builder: builder, filePersister: filePersister);

        await processor.ProcessAsync(new StudentImportRequest { SchoolId = "SCH-1" });

        filePersister.Verify(f => f.PersistAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<StudentModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_PersistsImportRecord_WithCorrectCounts()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_PersistsImportRecord_WithCorrectCounts));
        var processor = BuildProcessor(db);

        var request = new StudentImportRequest
        {
            SchoolId = "SCH-1",
            Students = new List<StudentModel>
            {
                new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" },
                new() { ExternalId = "S1", FirstName = "Ada", LastName = "Dup", YearLevel = "10", Status = "Active" }, // duplicate, ignored
                new() { ExternalId = "", FirstName = "No", LastName = "Id", YearLevel = "10", Status = "Active" } // invalid, dropped
            }
        };

        var summary = await processor.ProcessAsync(request);

        Assert.Equal(3, summary.Total);
        Assert.Equal(1, summary.Success);
        Assert.Equal(1, summary.Failed); // the missing-ExternalId record
        Assert.Equal(1, summary.Duplicates);

        var record = await db.ImportRecords.SingleAsync();
        Assert.Equal(summary.ImportId, record.ImportId);
        Assert.Equal(ImportStatus.CompletedWithErrors, record.Status);
        Assert.NotNull(record.CompletedAt);
    }

    [Fact]
    public async Task ProcessAsync_PublishesStudentImportCompletedEvent()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_PublishesStudentImportCompletedEvent));
        var eventPublisher = new Mock<IEventPublisher>();
        var processor = BuildProcessor(db, eventPublisher: eventPublisher);

        var request = new StudentImportRequest
        {
            SchoolId = "SCH-1",
            Students = new List<StudentModel>
            {
                new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
            }
        };

        await processor.ProcessAsync(request);

        eventPublisher.Verify(p => p.PublishAsync(
            It.Is<StudentImportCompletedEvent>(e => e.SchoolId == "SCH-1" && e.Success == 1 && e.Failed == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ThrowsArgumentException_WhenSchoolIdMissing()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_ThrowsArgumentException_WhenSchoolIdMissing));
        var processor = BuildProcessor(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            processor.ProcessAsync(new StudentImportRequest { SchoolId = "" }));
    }

    [Fact]
    public async Task ProcessAsync_MarksImportFailed_WhenRetrievalThrows()
    {
        await using var db = NewInMemoryContext(nameof(ProcessAsync_MarksImportFailed_WhenRetrievalThrows));

        var retriever = new Mock<IStudentDataRetriever>();
        retriever.Setup(r => r.RetrieveAsync("SCH-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SIS API unreachable"));

        var processor = BuildProcessor(db, retriever: retriever);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            processor.ProcessAsync(new StudentImportRequest { SchoolId = "SCH-1" }));

        var record = await db.ImportRecords.SingleAsync();
        Assert.Equal(ImportStatus.Failed, record.Status);
        Assert.Contains("SIS API unreachable", record.ErrorSummary);
    }
}
