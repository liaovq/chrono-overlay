using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChronoOverlay.Models;

namespace ChronoOverlay.Services;

public sealed class SettingsService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _settingsDirectory;
    private readonly string _settingsPath;
    private readonly object _sync = new();
    private System.Threading.Timer? _saveTimer;
    private AppSettings? _pendingSettings;

    public SettingsService(string? settingsDirectory = null)
    {
        _settingsDirectory = settingsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChronoOverlay");
        _settingsPath = Path.Combine(_settingsDirectory, "config.json");
    }

    public string SettingsPath => _settingsPath;

    public event EventHandler<Exception>? SaveFailed;

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return CreateDefaults();
        }

        try
        {
            string json = File.ReadAllText(_settingsPath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            AppSettings loaded = (settings ?? CreateDefaults()).Normalize();
            loaded.AppVersion = VersionService.Current;
            return loaded;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            BackupCorruptFile();
            return CreateDefaults();
        }
    }

    public void SaveDebounced(AppSettings settings, TimeSpan? delay = null)
    {
        lock (_sync)
        {
            _pendingSettings = settings;
            _saveTimer?.Dispose();
            _saveTimer = new System.Threading.Timer(
                _ => SavePending(),
                null,
                delay ?? TimeSpan.FromMilliseconds(400),
                Timeout.InfiniteTimeSpan);
        }
    }

    public void SaveImmediately(AppSettings settings)
    {
        lock (_sync)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
            _pendingSettings = null;
        }

        TrySave(settings);
    }

    public static string Serialize(AppSettings settings)
    {
        settings.AppVersion = VersionService.Current;
        return JsonSerializer.Serialize(settings.Normalize(), JsonOptions);
    }

    private static AppSettings CreateDefaults() => new() { AppVersion = VersionService.Current };

    private void SavePending()
    {
        AppSettings? settings;
        lock (_sync)
        {
            settings = _pendingSettings;
            _pendingSettings = null;
            _saveTimer?.Dispose();
            _saveTimer = null;
        }

        if (settings is not null)
        {
            TrySave(settings);
        }
    }

    private void TrySave(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            settings.AppVersion = VersionService.Current;
            string temporaryPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, Serialize(settings));

            if (File.Exists(_settingsPath))
            {
                File.Replace(temporaryPath, _settingsPath, null);
            }
            else
            {
                File.Move(temporaryPath, _settingsPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SaveFailed?.Invoke(this, exception);
        }
    }

    private void BackupCorruptFile()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string backupPath = Path.Combine(_settingsDirectory, $"config.corrupt-{timestamp}.json");
            File.Move(_settingsPath, backupPath, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SaveFailed?.Invoke(this, exception);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
    }
}
