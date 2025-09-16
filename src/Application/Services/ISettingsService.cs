using LanguageExt;
using System;

namespace Darklands.Application.Services;

/// <summary>
/// Abstraction for game settings management.
/// Enables platform-specific storage, testing with in-memory settings, and type-safe configuration.
/// Per ADR-006: This service qualifies for abstraction due to platform differences and testing needs.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the value for a specific setting key.
    /// Returns the default value if the setting doesn't exist or can't be loaded.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">Strongly-typed setting key</param>
    /// <returns>The setting value or the key's default value</returns>
    T Get<T>(SettingKey<T> key);

    /// <summary>
    /// Sets a value for a specific setting key.
    /// The setting is stored in memory and will be persisted when Save() is called.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">Strongly-typed setting key</param>
    /// <param name="value">The value to set</param>
    /// <returns>Success/failure result</returns>
    Fin<Unit> Set<T>(SettingKey<T> key, T value);

    /// <summary>
    /// Persists all setting changes to storage.
    /// Should be called after a batch of setting changes or before application exit.
    /// </summary>
    /// <returns>Success/failure result</returns>
    Fin<Unit> Save();

    /// <summary>
    /// Reloads settings from persistent storage, discarding any unsaved changes.
    /// </summary>
    /// <returns>Success/failure result</returns>
    Fin<Unit> Reload();

    /// <summary>
    /// Resets a specific setting to its default value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key to reset</param>
    /// <returns>Success/failure result</returns>
    Fin<Unit> ResetToDefault<T>(SettingKey<T> key);

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    /// <returns>Success/failure result</returns>
    Fin<Unit> ResetAllToDefaults();
}

/// <summary>
/// Strongly-typed setting key that includes both the key name and default value.
/// Prevents magic strings and provides type safety for setting values.
/// </summary>
/// <typeparam name="T">The type of value this setting stores</typeparam>
public sealed record SettingKey<T>(string Key, T DefaultValue)
{
    public override string ToString() => Key;
}

/// <summary>
/// Predefined setting keys for common game configuration.
/// Provides a central registry of all available settings with their default values.
/// </summary>
public static class GameSettings
{
    // Audio Settings
    public static readonly SettingKey<float> MasterVolume = new("audio.master_volume", 1.0f);
    public static readonly SettingKey<float> MusicVolume = new("audio.music_volume", 0.8f);
    public static readonly SettingKey<float> SfxVolume = new("audio.sfx_volume", 1.0f);
    public static readonly SettingKey<float> UiVolume = new("audio.ui_volume", 0.9f);

    // Display Settings
    public static readonly SettingKey<bool> Fullscreen = new("display.fullscreen", false);
    public static readonly SettingKey<int> ResolutionWidth = new("display.resolution_width", 1920);
    public static readonly SettingKey<int> ResolutionHeight = new("display.resolution_height", 1080);
    public static readonly SettingKey<bool> VSync = new("display.vsync", true);
    public static readonly SettingKey<int> MaxFps = new("display.max_fps", 60);

    // Gameplay Settings  
    public static readonly SettingKey<bool> ShowGridLines = new("gameplay.show_grid_lines", true);
    public static readonly SettingKey<bool> ShowMovementPreview = new("gameplay.show_movement_preview", true);
    public static readonly SettingKey<bool> ShowDamageNumbers = new("gameplay.show_damage_numbers", true);
    public static readonly SettingKey<float> CombatAnimationSpeed = new("gameplay.combat_animation_speed", 1.0f);
    public static readonly SettingKey<bool> AutoEndTurn = new("gameplay.auto_end_turn", false);

    // Input Settings
    public static readonly SettingKey<float> MouseSensitivity = new("input.mouse_sensitivity", 1.0f);
    public static readonly SettingKey<bool> MouseInvert = new("input.mouse_invert", false);
    public static readonly SettingKey<float> CameraScrollSpeed = new("input.camera_scroll_speed", 1.0f);

    // Accessibility Settings
    public static readonly SettingKey<bool> HighContrast = new("accessibility.high_contrast", false);
    public static readonly SettingKey<float> TextScale = new("accessibility.text_scale", 1.0f);
    public static readonly SettingKey<bool> ReduceMotion = new("accessibility.reduce_motion", false);

    // Development/Debug Settings
    public static readonly SettingKey<bool> ShowDebugOverlay = new("debug.show_overlay", false);
    public static readonly SettingKey<bool> ShowPerformanceMetrics = new("debug.show_performance", false);
    public static readonly SettingKey<bool> EnableDevConsole = new("debug.enable_console", false);
}
