namespace SchoolDataIntegration.Api.Transformer;

public class TransformResult
{
    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public List<string> Errors { get; } = new();
}
