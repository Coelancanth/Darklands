using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Darklands.Application.Services;
using Darklands.Domain.Grid;
using Darklands.Application.Common;
using System.Collections.Generic;
using static LanguageExt.Prelude;

// Alias to resolve LogLevel namespace collision
using DomainLogLevel = Darklands.Application.Common.LogLevel;

namespace Darklands.Application.Infrastructure.Services;

/// <summary>
/// Mock implementation of IAudioService for testing purposes.
/// Records all audio operations for verification in unit tests.
/// Provides controllable failure scenarios for error handling tests.
/// </summary>
public sealed class MockAudioService : IAudioService
{
    private readonly ICategoryLogger _logger = null!;
    private readonly List<AudioOperation> _operations;
    private readonly object _operationsLock = new();

    // Configuration for controlling mock behavior
    public bool ShouldFailPlaySound { get; set; } = false;
    public bool ShouldFailSetMusic { get; set; } = false;
    public bool ShouldFailSetVolume { get; set; } = false;
    public bool ShouldFailStopAll { get; set; } = false;

    // State tracking
    public MusicId? CurrentMusicTrack { get; private set; }
    public Dictionary<AudioBus, float> BusVolumes { get; } = new();
    public bool IsAllStopped { get; private set; }

    public MockAudioService(ICategoryLogger? logger = null)
    {
        _logger = logger!;
        _operations = new List<AudioOperation>();

        // Initialize bus volumes to defaults
        BusVolumes[AudioBus.Master] = 1.0f;
        BusVolumes[AudioBus.Music] = 0.8f;
        BusVolumes[AudioBus.SoundEffects] = 1.0f;
        BusVolumes[AudioBus.UI] = 0.9f;
    }

    public Fin<Unit> PlaySound(SoundId soundId, Position? position = null)
    {
        if (ShouldFailPlaySound)
        {
            var error = Error.New($"Mock configured to fail PlaySound for {soundId}");
            _logger.Log(DomainLogLevel.Warning, LogCategory.System, "Mock audio service failing PlaySound: {Error}", error);
            return FinFail<Unit>(error);
        }

        var operation = new PlaySoundOperation(soundId, position, DateTime.UtcNow);
        RecordOperation(operation);

        IsAllStopped = false;
        _logger.Log(DomainLogLevel.Debug, LogCategory.System, "Mock played sound {SoundId} at position {Position}", soundId, position?.ToString() ?? "null");
        return FinSucc(unit);
    }

    public Fin<Unit> SetMusicTrack(MusicId musicId)
    {
        if (ShouldFailSetMusic)
        {
            var error = Error.New($"Mock configured to fail SetMusicTrack for {musicId}");
            _logger.Log(DomainLogLevel.Warning, LogCategory.System, "Mock audio service failing SetMusicTrack: {Error}", error);
            return FinFail<Unit>(error);
        }

        var operation = new SetMusicOperation(musicId, DateTime.UtcNow);
        RecordOperation(operation);

        CurrentMusicTrack = musicId;
        IsAllStopped = false;
        _logger.Log(DomainLogLevel.Debug, LogCategory.System, "Mock set music track to {MusicId}", musicId);
        return FinSucc(unit);
    }

    public Fin<Unit> SetBusVolume(AudioBus bus, float volume)
    {
        if (ShouldFailSetVolume)
        {
            var error = Error.New($"Mock configured to fail SetBusVolume for {bus}");
            _logger.Log(DomainLogLevel.Warning, LogCategory.System, "Mock audio service failing SetBusVolume: {Error}", error);
            return FinFail<Unit>(error);
        }

        // Clamp volume like the real implementation
        volume = Math.Clamp(volume, 0.0f, 1.0f);

        var operation = new SetVolumeOperation(bus, volume, DateTime.UtcNow);
        RecordOperation(operation);

        BusVolumes[bus] = volume;
        _logger.Log(DomainLogLevel.Debug, LogCategory.System, "Mock set {AudioBus} volume to {Volume}", bus, volume);
        return FinSucc(unit);
    }

    public Fin<Unit> StopAll()
    {
        if (ShouldFailStopAll)
        {
            var error = Error.New("Mock configured to fail StopAll");
            _logger.Log(DomainLogLevel.Warning, LogCategory.System, "Mock audio service failing StopAll: {Error}", error);
            return FinFail<Unit>(error);
        }

        var operation = new StopAllOperation(DateTime.UtcNow);
        RecordOperation(operation);

        IsAllStopped = true;
        _logger.Log(DomainLogLevel.Debug, LogCategory.System, "Mock stopped all audio");
        return FinSucc(unit);
    }

    /// <summary>
    /// Gets all recorded operations for test verification.
    /// Thread-safe snapshot of operations list.
    /// </summary>
    public IReadOnlyList<AudioOperation> GetRecordedOperations()
    {
        lock (_operationsLock)
        {
            return new List<AudioOperation>(_operations);
        }
    }

    /// <summary>
    /// Gets operations of a specific type for test verification.
    /// </summary>
    public IReadOnlyList<T> GetOperations<T>() where T : AudioOperation
    {
        lock (_operationsLock)
        {
            return _operations.OfType<T>().ToList();
        }
    }

    /// <summary>
    /// Clears all recorded operations. Useful for test setup.
    /// </summary>
    public void ClearOperations()
    {
        lock (_operationsLock)
        {
            _operations.Clear();
        }
    }

    /// <summary>
    /// Resets the mock to its initial state.
    /// </summary>
    public void Reset()
    {
        ClearOperations();
        CurrentMusicTrack = null;
        IsAllStopped = false;
        ShouldFailPlaySound = false;
        ShouldFailSetMusic = false;
        ShouldFailSetVolume = false;
        ShouldFailStopAll = false;

        // Reset volumes to defaults
        BusVolumes[AudioBus.Master] = 1.0f;
        BusVolumes[AudioBus.Music] = 0.8f;
        BusVolumes[AudioBus.SoundEffects] = 1.0f;
        BusVolumes[AudioBus.UI] = 0.9f;
    }

    private void RecordOperation(AudioOperation operation)
    {
        lock (_operationsLock)
        {
            _operations.Add(operation);
        }
    }
}

/// <summary>
/// Base class for recorded audio operations.
/// </summary>
public abstract record AudioOperation(DateTime Timestamp);

/// <summary>
/// Record of a PlaySound operation.
/// </summary>
public sealed record PlaySoundOperation(
    SoundId SoundId,
    Position? Position,
    DateTime Timestamp) : AudioOperation(Timestamp);

/// <summary>
/// Record of a SetMusicTrack operation.
/// </summary>
public sealed record SetMusicOperation(
    MusicId MusicId,
    DateTime Timestamp) : AudioOperation(Timestamp);

/// <summary>
/// Record of a SetBusVolume operation.
/// </summary>
public sealed record SetVolumeOperation(
    AudioBus Bus,
    float Volume,
    DateTime Timestamp) : AudioOperation(Timestamp);

/// <summary>
/// Record of a StopAll operation.
/// </summary>
public sealed record StopAllOperation(DateTime Timestamp) : AudioOperation(Timestamp);
