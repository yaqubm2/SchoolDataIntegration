namespace SchoolDataIntegration.Api.Models;


public class StudentRecord
{
    #region constructor
    public StudentRecord()
    {
        ExternalId = string.Empty;
        SchoolId = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        YearLevel = string.Empty;
        Status = string.Empty;
    }

    #endregion

    #region properties

    public int Id { get; set; }
    
    public string ExternalId { get; set; } 
    
    public string SchoolId { get; set; } 

    public string FirstName { get; set; } 

    public string LastName { get; set; } 

    public string YearLevel { get; set; } 
    
    public string Status { get; set; } 

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    #endregion

}
