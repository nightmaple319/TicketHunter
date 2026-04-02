using System.Text.Json;
using TicketHunter.Core.Models;

namespace TicketHunter.Core.Services;

public class ConfigService : IDisposable
{
    private readonly string _configPath;
    private readonly FileSystemWatcher _watcher;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppConfig _config;
    private readonly object _lock = new();

    public event Action<AppConfig>? ConfigChanged;

    public AppConfig Config
    {
        get { lock (_lock) return _config; }
    }

    public ConfigService(string configPath = "settings.json")
    {
        _configPath = Path.GetFullPath(configPath);
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // Load or create default config (before watcher is set up)
        if (File.Exists(_configPath))
        {
            _config = LoadFromDisk();
        }
        else
        {
            _config = new AppConfig();
            var json = JsonSerializer.Serialize(_config, _jsonOptions);
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_configPath, json);
        }

        // Now set up file watcher
        var watchDir = Path.GetDirectoryName(_configPath) ?? ".";
        var watchFile = Path.GetFileName(_configPath);
        _watcher = new FileSystemWatcher(watchDir, watchFile)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Task.Delay(200).ContinueWith(_ =>
        {
            try
            {
                var newConfig = LoadFromDisk();
                lock (_lock) _config = newConfig;
                ConfigChanged?.Invoke(newConfig);
            }
            catch
            {
                // Ignore reload errors (file might be locked during write)
            }
        });
    }

    private AppConfig LoadFromDisk()
    {
        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        lock (_lock) _config = config;

        _watcher.EnableRaisingEvents = false;
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }
        finally
        {
            _watcher.EnableRaisingEvents = true;
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        GC.SuppressFinalize(this);
    }
}
