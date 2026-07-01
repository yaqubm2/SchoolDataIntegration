namespace SchoolDataIntegration.Domain;

public class StudentModel
{
    #region constructor

    public StudentModel()
    {
        ExternalId = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        YearLevel = string.Empty;
        Status = string.Empty;
    }

    #endregion

    #region properties

    public string ExternalId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string YearLevel { get; set; }
    public string Status { get; set; }

    #endregion
}
