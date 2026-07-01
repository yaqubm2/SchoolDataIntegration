namespace SchoolDataIntegration.Api.Models;

/// <summary>
/// Body of POST /student-imports.
///
/// "Students" is optional: if the caller supplies records inline we ingest
/// those directly (handy for manual pushes / tests). If it's omitted or
/// empty, the Processor pulls data itself via the Retriever pipeline
/// (local CSV drop folder first, then the school's REST API), which is the
/// primary integration path this service is built for.
/// </summary>
public class StudentImportRequest
{
    public string SchoolId { get; set; } = string.Empty;

    public List<StudentModel>? Students { get; set; }
}
