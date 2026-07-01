using System.Xml.Serialization;

namespace SchoolDataIntegration.Application;

[XmlRoot("SchoolDataIntegrationConfig")]
public class AppConfig
{
    [XmlElement("LocalCsvDropFolder")]
    public string LocalCsvDropFolder { get; set; } = @"D:\Temp\school";

    [XmlElement("RestApi")]
    public RestApiConfig RestApi { get; set; } = new();

    [XmlElement("Database")]
    public DatabaseConfig Database { get; set; } = new();

    [XmlElement("Mail")]
    public MailConfig Mail { get; set; } = new();
}

public class RestApiConfig
{
    [XmlElement("BaseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    [XmlElement("Username")]
    public string Username { get; set; } = string.Empty;

    [XmlElement("Password")]
    public string Password { get; set; } = string.Empty;

    [XmlElement("TimeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;
}

public class DatabaseConfig
{
    [XmlElement("ConnectionString")]
    public string ConnectionString { get; set; } = "Data Source=schoolapp.db";
}

public class MailConfig
{
    [XmlElement("SmtpHost")]
    public string SmtpHost { get; set; } = string.Empty;

    [XmlElement("SmtpPort")]
    public int SmtpPort { get; set; } = 25;

    [XmlElement("FromAddress")]
    public string FromAddress { get; set; } = "noreply@schoolapp.local";

    [XmlElement("ToAddress")]
    public string ToAddress { get; set; } = string.Empty;

    [XmlElement("EnableSsl")]
    public bool EnableSsl { get; set; } = true;

    [XmlElement("DryRun")]
    public bool DryRun { get; set; } = true;
}
