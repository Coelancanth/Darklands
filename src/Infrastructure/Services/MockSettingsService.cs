using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Darklands.Application.Services;
using Darklands.Application.Common;
using System.Collections.Generic;
using static LanguageExt.Prelude;

// Alias to resolve LogLevel namespace collision
using DomainLogLevel = Darklands.Application.Common.LogLevel;

namespace Darklands.Application.Infrastructure.Services;

/// <summary>
/// Mock implementation of ISettingsService for testing purposes.
/// Provides in-memory settings storage with controllable failure scenarios.
/// Enables testing settings-dependent behavior without file system dependencies.
/// </summary>
public sealed class MockSettingsService : ISettingsService
{
    private readonly ICategoryLogger? _logger;
    private readonly Dictionary<string, object> _settings;
    private readonly object _settingsLock = new();

    // Configuration for controlling mock behavior
    public bool ShouldFailSave { get; set; } = false;
    public bool ShouldFailReload { get; set; } = false;
    public bool ShouldFailSet { get; set; } = false;

    // State tracking
    public bool HasUnsavedChanges { get; private set; } = false;
    public int SaveCallCount { get; private set; } = 0;
    public int ReloadCallCount { get; private set; } = 0;

    public MockSettingsService(ICategoryLogger? logger = null)
    {
        _logger = logger;
        _settings = new Dictionary<string, object>();
    }

    public T Get<T>(SettingKey<T> key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        lock (_settingsLock)
        {
            if (_settings.TryGetValue(key.Key, out var value))
            {
                try
                {
                    if (value is T directValue)
                    {
                        _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Retrieved setting {Key} = {Value}", key.Key, value);
                        return directValue;
                    }

                    // Attempt conversion for numeric types
                    if (typeof(T).IsPrimitive && value != null)
                    {
                        var convertedValue = (T)Convert.ChangeType(value, typeof(T));
                        _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Retrieved and converted setting {Key} = {Value}", key.Key, convertedValue);
                        return convertedValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "Failed to convert setting {Key} value {Value} to type {Type}, using default: {Error}",
                        key.Key, value, typeof(T).Name, ex.Message);
                }
            }

            _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Setting {Key} not found, using default value {Default}", key.Key, key.DefaultValue?.ToString() ?? "null");
            return key.DefaultValue;
        }
    }

    public Fin<Unit> Set<T>(SettingKey<T> key, T value)
    {
        if (key == null)
            return FinFail<Unit>(Error.New("Setting key cannot be null"));

        if (ShouldFailSet)
        {
            var error = Error.New($"Mock configured to fail Set for {key.Key}");
            _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "Mock settings service failing Set: {Error}", error);
            return FinFail<Unit>(error);
        }

        try
        {
            lock (_settingsLock)
            {
                _settings[key.Key] = value!;
                HasUnsavedChanges = true;
                _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Set setting {Key} to {Value}", key.Key, value?.ToString() ?? "null");
            }

            return FinSucc(unit);
        }
        catch (Exception ex)
        {
            var error = Error.New($"Failed to set setting {key.Key}: {ex.Message}", ex);
            _logger?.Log(DomainLogLevel.Error, LogCategory.System, "Setting update failed: {Error}", error);
            return FinFail<Unit>(error);
        }
    }

    public Fin<Unit> Save()
    {
        SaveCallCount++;

        if (ShouldFailSave)
        {
            var error = Error.New("Mock configured to fail Save");
            _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "Mock settings service failing Save: {Error}", error);
            return FinFail<Unit>(error);
        }

        try
        {
            lock (_settingsLock)
            {
                HasUnsavedChanges = false;
                _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Mock saved {Count} settings", _settings.Count);
            }

            return FinSucc(unit);
        }
        catch (Exception ex)
        {
            var error = Error.New($"Failed to save settings: {ex.Message}", ex);
            _logger?.Log(DomainLogLevel.Error, LogCategory.System, "Settings save failed: {Error}", error);
            return FinFail<Unit>(error);
        }
    }

    public Fin<Unit> Reload()
    {
        ReloadCallCount++;

        if (ShouldFailReload)
        {
            var error = Error.New("Mock configured to fail Reload");
            _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "Mock settings service failing Reload: {Error}", error);
            return FinFail<Unit>(error);
        }

        try
        {
            lock (_settingsLock)
            {
                // In a real implementation, this would reload from storage
                // For the mock, we just clear unsaved changes flag
                HasUnsavedChanges = false;
                _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Mock reloaded settings");
            }

            return FinSucc(unit);
        }
        catch (Exception ex)
        {
            var error = Error.New($"Failed to reload settings: {ex.Message}", ex);
            _logger?.Log(DomainLogLevel.Error, LogCategory.System, "Settings reload failed: {Error}", error);
            return FinFail<Unit>(error);
        }
    }

    public Fin<Unit> ResetToDefault<T>(SettingKey<T> key)
    {
        if (key == null)
            return FinFail<Unit>(Error.New("Setting key cannot be null"));

        return Set(key, key.DefaultValue);
    }

    public Fin<Unit> ResetAllToDefaults()
    {
        try
        {
            lock (_settingsLock)
            {
                var settingsCount = _settings.Count;
                _settings.Clear();
                HasUnsavedChanges = true;
                _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Reset {Count} settings to defaults", settingsCount);
            }

            return FinSucc(unit);
        }
        catch (Exception ex)
        {
            var error = Error.New($"Failed to reset all settings: {ex.Message}", ex);
            _logger?.Log(DomainLogLevel.Error, LogCategory.System, "Reset all settings failed: {Error}", error);
            return FinFail<Unit>(error);
        }
    }

    /// <summary>
    /// Gets all current settings for test verification.
    /// </summary>
    public IReadOnlyDictionary<string, object> GetAllSettings()
    {
        lock (_settingsLock)
        {
            return new Dictionary<string, object>(_settings);
        }
    }

    /// <summary>
    /// Preloads the mock with initial settings values.
    /// Useful for test setup that requires specific initial state.
    /// </summary>
    public void PreloadSettings(Dictionary<string, object> initialSettings)
    {
        if (initialSettings == null)
            throw new ArgumentNullException(nameof(initialSettings));

        lock (_settingsLock)
        {
            _settings.Clear();
            foreach (var kvp in initialSettings)
            {
                _settings[kvp.Key] = kvp.Value;
            }
            HasUnsavedChanges = false;
            _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Preloaded {Count} settings", initialSettings.Count);
        }
    }

    /// <summary>
    /// Resets the mock to its initial state.
    /// </summary>
    public void Reset()
    {
        lock (_settingsLock)
        {
            _settings.Clear();
            HasUnsavedChanges = false;
            SaveCallCount = 0;
            ReloadCallCount = 0;
            ShouldFailSave = false;
            ShouldFailReload = false;
            ShouldFailSet = false;
            _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Mock settings service reset to initial state");
        }
    }

    /// <summary>
    /// Simulates loading settings from a save file or external source.
    /// Useful for testing reload behavior.
    /// </summary>
    public void SimulateExternalSettingsChange(Dictionary<string, object> externalSettings)
    {
        if (externalSettings == null)
            throw new ArgumentNullException(nameof(externalSettings));

        lock (_settingsLock)
        {
            // Simulate external changes by modifying settings directly
            foreach (var kvp in externalSettings)
            {
                _settings[kvp.Key] = kvp.Value;
            }
            // Don't set unsaved changes flag since this simulates external modification
            _logger?.Log(DomainLogLevel.Debug, LogCategory.System, "Simulated external change to {Count} settings", externalSettings.Count);
        }
    }
}
