using SchoolDataIntegration.Infrastructure;
using Xunit;

namespace SchoolDataIntegration.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.xml");

    [Fact]
    public void Load_DeserializesAllSections_FromValidXml()
    {
        File.WriteAllText(_tempFile, """
        <?xml version="1.0" encoding="utf-8"?>
        <SchoolDataIntegrationConfig>
          <LocalCsvDropFolder>D:\Temp\school</LocalCsvDropFolder>
          <RestApi>
            <BaseUrl>https://sis.example.local/api</BaseUrl>
            <Username>svc-user</Username>
            <Password>secret</Password>
            <TimeoutSeconds>45</TimeoutSeconds>
          </RestApi>
          <Database>
            <ConnectionString>Data Source=test.db</ConnectionString>
          </Database>
          <Mail>
            <SmtpHost>smtp.example.local</SmtpHost>
            <SmtpPort>587</SmtpPort>
            <FromAddress>noreply@schoolapp.local</FromAddress>
            <ToAddress>ops@schoolapp.local</ToAddress>
            <EnableSsl>true</EnableSsl>
            <DryRun>false</DryRun>
          </Mail>
        </SchoolDataIntegrationConfig>
        """);

        var config = new ConfigLoader(_tempFile).Load();

        Assert.Equal(@"D:\Temp\school", config.LocalCsvDropFolder);
        Assert.Equal("https://sis.example.local/api", config.RestApi.BaseUrl);
        Assert.Equal("svc-user", config.RestApi.Username);
        Assert.Equal(45, config.RestApi.TimeoutSeconds);
        Assert.Equal("Data Source=test.db", config.Database.ConnectionString);
        Assert.Equal("smtp.example.local", config.Mail.SmtpHost);
        Assert.False(config.Mail.DryRun);
    }

    [Fact]
    public void Load_Caches_SoFileIsOnlyReadOnce()
    {
        File.WriteAllText(_tempFile, """
        <?xml version="1.0" encoding="utf-8"?>
        <SchoolDataIntegrationConfig>
          <LocalCsvDropFolder>D:\Temp\school</LocalCsvDropFolder>
        </SchoolDataIntegrationConfig>
        """);

        var loader = new ConfigLoader(_tempFile);
        var first = loader.Load();

        File.WriteAllText(_tempFile, """
        <?xml version="1.0" encoding="utf-8"?>
        <SchoolDataIntegrationConfig>
          <LocalCsvDropFolder>D:\Changed</LocalCsvDropFolder>
        </SchoolDataIntegrationConfig>
        """);

        var second = loader.Load();

        Assert.Same(first, second);
        Assert.Equal(@"D:\Temp\school", second.LocalCsvDropFolder);
    }

    [Fact]
    public void Load_ThrowsFileNotFoundException_WhenConfigFileMissing()
    {
        var loader = new ConfigLoader(Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid()}.xml"));

        Assert.Throws<FileNotFoundException>(() => loader.Load());
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }
}
