using System.Xml.Serialization;
using SchoolDataIntegration.Application;

namespace SchoolDataIntegration.Infrastructure;

public class ConfigLoader : IConfigLoader
{
    #region private fields
    private readonly string _configPath;
    private AppConfig? _cached;
    private readonly object _lock = new();

    #endregion

    #region public methods

    public ConfigLoader(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfig Load()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        lock (_lock)
        {
            if (_cached is not null)
            {
                return _cached;
            }

            if (!File.Exists(_configPath))
            {
                throw new FileNotFoundException(
                    $"Configuration file not found at '{_configPath}'. " +
                    "Copy Config/config.xml alongside the application and adjust values for your environment.",
                    _configPath);
            }

            var serializer = new XmlSerializer(typeof(AppConfig));
            using var stream = File.OpenRead(_configPath);
            var loaded = (AppConfig?)serializer.Deserialize(stream)
                         ?? throw new InvalidOperationException("config.xml deserialized to null.");

            _cached = loaded;
            return _cached;
        }
    }

    #endregion
}
