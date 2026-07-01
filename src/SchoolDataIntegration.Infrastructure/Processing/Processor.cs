using Microsoft.Extensions.Logging;
using SchoolDataIntegration.Application;
using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Infrastructure;

public class Processor : IProcessor
{

    #region private fields
    private readonly IStudentDataRetriever _retriever;
    private readonly IFilePersister _filePersister;
    private readonly IStudentBuilder _builder;
    private readonly IStudentFilter _filter;
    private readonly IStudentTransformer _transformer;
    private readonly IEventPublisher _eventPublisher;
    private readonly SchoolDbContext _dbContext;
    private readonly ILogger<Processor> _logger;

    #endregion

    #region constructor

    public Processor(
        IStudentDataRetriever retriever,
        IFilePersister filePersister,
        IStudentBuilder builder,
        IStudentFilter filter,
        IStudentTransformer transformer,
        IEventPublisher eventPublisher,
        SchoolDbContext dbContext,
        ILogger<Processor> logger)
    {
        _retriever = retriever;
        _filePersister = filePersister;
        _builder = builder;
        _filter = filter;
        _transformer = transformer;
        _eventPublisher = eventPublisher;
        _dbContext = dbContext;
        _logger = logger;
    }

    #endregion

    #region Public methods

    public async Task<ImportSummary> ProcessAsync(StudentImportRequest request, CancellationToken ct = default)
    {
        ValidatRequestSchoolId(request);

        var importRecord = await CreateImportRecordAsync(request.SchoolId, ct);

        try
        {
            var rawStudents = await RetrieveAndBuildAsync(request, ct);
            var filterResult = _filter.Filter(request.SchoolId, rawStudents);
            var transformResult = await _transformer.TransformAsync(request.SchoolId, filterResult.Students, ct);

            return await CompleteSuccessfulImportAsync(importRecord, rawStudents.Count, filterResult, transformResult, ct);
        }
        catch (Exception ex)
        {
            await CompleteFailedImportAsync(importRecord, ex, ct);
            throw;
        }
    }

    #endregion

    #region private methods

    private static void ValidatRequestSchoolId(StudentImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SchoolId))
        {
            throw new ArgumentException("SchoolId is required.", nameof(request));
        }
    }

    private async Task<ImportRecord> CreateImportRecordAsync(string schoolId, CancellationToken ct)
    {
        var importRecord = new ImportRecord
        {
            SchoolId = schoolId,
            ReceivedAt = DateTime.UtcNow,
            Status = ImportStatus.Processing
        };

        _dbContext.ImportRecords.Add(importRecord);
        await _dbContext.SaveChangesAsync(ct);

        return importRecord;
    }


    private async Task<List<StudentModel>> RetrieveAndBuildAsync(StudentImportRequest request, CancellationToken ct)
    {
        if (request.Students is { Count: > 0 })
        {
            _logger.LogInformation("Using {Count} student records supplied inline in the request for school {SchoolId}",
                request.Students.Count, request.SchoolId);
            return request.Students;
        }

        var retrieved = await _retriever.RetrieveAsync(request.SchoolId, ct);
        var students = _builder.Build(retrieved);

        if (retrieved.Source == DataSourceKind.RestApi && students.Count > 0)
        {
            await _filePersister.PersistAsync(request.SchoolId, students, ct);
        }

        return students;
    }


    private async Task<ImportSummary> CompleteSuccessfulImportAsync(
        ImportRecord importRecord,
        int totalRecords,
        FilterResult filterResult,
        TransformResult transformResult,
        CancellationToken ct)
    {
        var totalFailed = transformResult.FailedCount + filterResult.ValidationErrors.Count;

        importRecord.TotalRecords = totalRecords;
        importRecord.SuccessfulRecords = transformResult.SuccessCount;
        importRecord.FailedRecords = totalFailed;
        importRecord.DuplicateRecords = filterResult.DuplicatesIgnored;
        importRecord.CompletedAt = DateTime.UtcNow;
        importRecord.Status = totalFailed == 0 ? ImportStatus.Completed : ImportStatus.CompletedWithErrors;
        importRecord.ErrorSummary = BuildErrorSummary(filterResult, transformResult);

        await _dbContext.SaveChangesAsync(ct);
        LogSummary(importRecord);

        var summary = BuildSummary(importRecord, totalRecords, transformResult.SuccessCount, totalFailed, filterResult.DuplicatesIgnored);
        await PublishCompletedEventAsync(importRecord, ct);

        return summary;
    }

    private async Task CompleteFailedImportAsync(ImportRecord importRecord, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Import {ImportId} for school {SchoolId} failed before any records could be processed",
            importRecord.ImportId, importRecord.SchoolId);

        importRecord.Status = ImportStatus.Failed;
        importRecord.CompletedAt = DateTime.UtcNow;
        importRecord.ErrorSummary = ex.Message;

        await _dbContext.SaveChangesAsync(ct);
        await PublishCompletedEventAsync(importRecord, ct);
    }

    private static string? BuildErrorSummary(FilterResult filterResult, TransformResult transformResult)
    {
        var allErrors = filterResult.ValidationErrors.Concat(transformResult.Errors).ToList();

        // Cap what we store to keep the tracking row bounded in size.
        return allErrors.Count > 0 ? string.Join(" | ", allErrors.Take(20)) : null;
    }

    private static ImportSummary BuildSummary(ImportRecord importRecord, int total, int success, int failed, int duplicates) =>
        new()
        {
            ImportId = importRecord.ImportId,
            SchoolId = importRecord.SchoolId,
            Total = total,
            Success = success,
            Failed = failed,
            Duplicates = duplicates,
            Status = importRecord.Status.ToString()
        };

    private Task PublishCompletedEventAsync(ImportRecord importRecord, CancellationToken ct) =>
        _eventPublisher.PublishAsync(new StudentImportCompletedEvent
        {
            ImportId = importRecord.ImportId,
            SchoolId = importRecord.SchoolId,
            Total = importRecord.TotalRecords,
            Success = importRecord.SuccessfulRecords,
            Failed = importRecord.FailedRecords,
            Duplicates = importRecord.DuplicateRecords,
            Status = importRecord.Status.ToString(),
            CompletedAt = importRecord.CompletedAt!.Value
        }, ct);

    private void LogSummary(ImportRecord record)
    {
        _logger.LogInformation(
            "Import {ImportId} for school {SchoolId} finished with status {Status}: total={Total} success={Success} failed={Failed} duplicates={Duplicates}",
            record.ImportId, record.SchoolId, record.Status, record.TotalRecords, record.SuccessfulRecords, record.FailedRecords, record.DuplicateRecords);
    }

    #endregion
}
