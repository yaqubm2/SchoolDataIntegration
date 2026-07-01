using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolDataIntegration.Application;
using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Infrastructure;

public class StudentTransformer : IStudentTransformer
{
    #region private fields
    private readonly SchoolDbContext _dbContext;
    private readonly ILogger<StudentTransformer> _logger;

    #endregion

    #region constructor

    public StudentTransformer(SchoolDbContext dbContext, ILogger<StudentTransformer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    #endregion

    #region public method

    public async Task<TransformResult> TransformAsync(string schoolId, IReadOnlyCollection<StudentModel> students, CancellationToken ct = default)
    {
        var result = new TransformResult();

        foreach (var dto in students)
        {
            try
            {
                var existing = await _dbContext.Students
                    .FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.ExternalId == dto.ExternalId, ct);

                var now = DateTime.UtcNow;

                if (existing is null)
                {
                    _dbContext.Students.Add(new StudentRecord
                    {
                        SchoolId = schoolId,
                        ExternalId = dto.ExternalId,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        YearLevel = dto.YearLevel,
                        Status = dto.Status,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                else
                {
                    existing.FirstName = dto.FirstName;
                    existing.LastName = dto.LastName;
                    existing.YearLevel = dto.YearLevel;
                    existing.Status = dto.Status;
                    existing.UpdatedAt = now;
                }

                await _dbContext.SaveChangesAsync(ct);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"ExternalId={dto.ExternalId}: {ex.Message}");
                _logger.LogError(ex,
                    "Failed to persist student for school {SchoolId}, ExternalId {ExternalId}",
                    schoolId, dto.ExternalId);

                _dbContext.ChangeTracker.Clear();
            }
        }

        return result;
    }

    #endregion
}
