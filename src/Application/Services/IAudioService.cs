using LanguageExt;
using Darklands.Domain.Grid;

namespace Darklands.Application.Services;

/// <summary>
/// Abstraction for audio playback functionality.
/// Enables testing, platform differences handling, and potential sound modding.
/// Per ADR-006: This service qualifies for abstraction due to testing needs and platform variance.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Plays a sound effect at the specified position.
    /// Position is optional - if null, sound plays without spatial positioning.
    /// </summary>
    /// <param name="soundId">Identifier for the sound to play</param>
    /// <param name="position">Optional world position for spatial audio</param>
    /// <returns>Success/failure result</returns>
    Fin<Unit> PlaySound(SoundId soundId, Position? position = null);

    /// <summary>
    /// Sets the current background music track.
    /// </summary>
    /// <param name="musicId">Identifier for the music track</param>
    /// <returns>Success/failure result</returns>
    Fin<Unit> SetMusicTrack(MusicId musicId);

    /// <summary>
    /// Adjusts the volume for a specific audio bus.
    /// </summary>
    /// <param name="bus">The audio bus to adjust</param>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    /// <returns>Success/failure result</returns>
    Fin<Unit> SetBusVolume(AudioBus bus, float volume);

    /// <summary>
    /// Stops all currently playing sounds and music.
    /// Useful for scene transitions or pause functionality.
    /// </summary>
    /// <returns>Success/failure result</returns>
    Fin<Unit> StopAll();
}

/// <summary>
/// Strongly-typed identifier for sound effects.
/// Prevents magic strings and enables compile-time validation.
/// </summary>
public readonly record struct SoundId(string Value)
{
    public static readonly SoundId SwordHit = new("sword_hit");
    public static readonly SoundId SwordMiss = new("sword_miss");
    public static readonly SoundId FootstepStone = new("footstep_stone");
    public static readonly SoundId FootstepGrass = new("footstep_grass");
    public static readonly SoundId ActorDied = new("actor_died");
    public static readonly SoundId ButtonClick = new("button_click");

    public override string ToString() => Value;
}

/// <summary>
/// Strongly-typed identifier for background music tracks.
/// </summary>
public readonly record struct MusicId(string Value)
{
    public static readonly MusicId CombatTheme = new("combat_theme");
    public static readonly MusicId MenuTheme = new("menu_theme");
    public static readonly MusicId VictoryTheme = new("victory_theme");
    public static readonly MusicId DefeatTheme = new("defeat_theme");

    public override string ToString() => Value;
}

/// <summary>
/// Audio bus categories for volume control.
/// Matches typical game audio organization.
/// </summary>
public enum AudioBus
{
    Master,
    Music,
    SoundEffects,
    UI
}
