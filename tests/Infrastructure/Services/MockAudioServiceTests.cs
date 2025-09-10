using Xunit;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Darklands.Core.Infrastructure.Services;
using Darklands.Core.Domain.Services;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Infrastructure.Services;

[Trait("Category", "Phase2")]
public class MockAudioServiceTests
{
    private readonly MockAudioService _audioService;

    public MockAudioServiceTests()
    {
        _audioService = new MockAudioService();
    }

    [Fact]
    public void PlaySound_WithValidSoundId_ShouldRecordOperation()
    {
        // Arrange
        var soundId = SoundId.SwordHit;
        var position = new Position(5, 10);

        // Act
        var result = _audioService.PlaySound(soundId, position);

        // Assert
        result.IsSucc.Should().BeTrue();

        var operations = _audioService.GetOperations<PlaySoundOperation>();
        operations.Should().HaveCount(1);
        operations[0].SoundId.Should().Be(soundId);
        operations[0].Position.Should().Be(position);
    }

    [Fact]
    public void PlaySound_WithFailureConfigured_ShouldReturnFailure()
    {
        // Arrange
        _audioService.ShouldFailPlaySound = true;
        var soundId = SoundId.FootstepStone;

        // Act
        var result = _audioService.PlaySound(soundId);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => Assert.Fail("Should have failed"),
            Fail: error => error.Message.Should().Contain("Mock configured to fail PlaySound")
        );
    }

    [Fact]
    public void SetMusicTrack_WithValidMusicId_ShouldUpdateCurrentTrack()
    {
        // Arrange
        var musicId = MusicId.CombatTheme;

        // Act
        var result = _audioService.SetMusicTrack(musicId);

        // Assert
        result.IsSucc.Should().BeTrue();
        _audioService.CurrentMusicTrack.Should().Be(musicId);
        _audioService.IsAllStopped.Should().BeFalse();

        var operations = _audioService.GetOperations<SetMusicOperation>();
        operations.Should().HaveCount(1);
        operations[0].MusicId.Should().Be(musicId);
    }

    [Fact]
    public void SetBusVolume_WithValidParameters_ShouldUpdateBusVolume()
    {
        // Arrange
        var bus = AudioBus.Music;
        var volume = 0.7f;

        // Act
        var result = _audioService.SetBusVolume(bus, volume);

        // Assert
        result.IsSucc.Should().BeTrue();
        _audioService.BusVolumes[bus].Should().Be(volume);

        var operations = _audioService.GetOperations<SetVolumeOperation>();
        operations.Should().HaveCount(1);
        operations[0].Bus.Should().Be(bus);
        operations[0].Volume.Should().Be(volume);
    }

    [Fact]
    public void SetBusVolume_WithVolumeOutOfRange_ShouldClampToValidRange()
    {
        // Arrange
        var bus = AudioBus.Master;

        // Act & Assert - Test upper bound
        var resultHigh = _audioService.SetBusVolume(bus, 1.5f);
        resultHigh.IsSucc.Should().BeTrue();
        _audioService.BusVolumes[bus].Should().Be(1.0f);

        // Act & Assert - Test lower bound
        var resultLow = _audioService.SetBusVolume(bus, -0.5f);
        resultLow.IsSucc.Should().BeTrue();
        _audioService.BusVolumes[bus].Should().Be(0.0f);
    }

    [Fact]
    public void StopAll_WhenCalled_ShouldSetStoppedFlag()
    {
        // Arrange
        _audioService.SetMusicTrack(MusicId.MenuTheme);
        _audioService.PlaySound(SoundId.ButtonClick);

        // Act
        var result = _audioService.StopAll();

        // Assert
        result.IsSucc.Should().BeTrue();
        _audioService.IsAllStopped.Should().BeTrue();

        var operations = _audioService.GetOperations<StopAllOperation>();
        operations.Should().HaveCount(1);
    }

    [Fact]
    public void GetRecordedOperations_WithMultipleOperations_ShouldReturnAllInOrder()
    {
        // Arrange & Act
        _audioService.PlaySound(SoundId.SwordHit, new Position(1, 1));
        _audioService.SetMusicTrack(MusicId.CombatTheme);
        _audioService.SetBusVolume(AudioBus.SoundEffects, 0.8f);
        _audioService.StopAll();

        // Assert
        var allOperations = _audioService.GetRecordedOperations();
        allOperations.Should().HaveCount(4);

        allOperations[0].Should().BeOfType<PlaySoundOperation>();
        allOperations[1].Should().BeOfType<SetMusicOperation>();
        allOperations[2].Should().BeOfType<SetVolumeOperation>();
        allOperations[3].Should().BeOfType<StopAllOperation>();

        // Verify timestamps are in ascending order
        for (int i = 1; i < allOperations.Count; i++)
        {
            allOperations[i].Timestamp.Should().BeOnOrAfter(allOperations[i - 1].Timestamp);
        }
    }

    [Fact]
    public void Reset_WhenCalled_ShouldClearAllStateAndOperations()
    {
        // Arrange
        _audioService.PlaySound(SoundId.ActorDied);
        _audioService.SetMusicTrack(MusicId.DefeatTheme);
        _audioService.SetBusVolume(AudioBus.UI, 0.5f);
        _audioService.ShouldFailPlaySound = true;

        // Act
        _audioService.Reset();

        // Assert
        _audioService.GetRecordedOperations().Should().BeEmpty();
        _audioService.CurrentMusicTrack.Should().BeNull();
        _audioService.IsAllStopped.Should().BeFalse();
        _audioService.ShouldFailPlaySound.Should().BeFalse();
        _audioService.ShouldFailSetMusic.Should().BeFalse();
        _audioService.ShouldFailSetVolume.Should().BeFalse();
        _audioService.ShouldFailStopAll.Should().BeFalse();

        // Verify bus volumes are reset to defaults
        _audioService.BusVolumes[AudioBus.Master].Should().Be(1.0f);
        _audioService.BusVolumes[AudioBus.Music].Should().Be(0.8f);
        _audioService.BusVolumes[AudioBus.SoundEffects].Should().Be(1.0f);
        _audioService.BusVolumes[AudioBus.UI].Should().Be(0.9f);
    }

    [Fact]
    public void ClearOperations_WhenCalled_ShouldOnlyClearOperationsNotState()
    {
        // Arrange
        _audioService.PlaySound(SoundId.FootstepGrass);
        _audioService.SetMusicTrack(MusicId.VictoryTheme);
        var currentMusic = _audioService.CurrentMusicTrack;

        // Act
        _audioService.ClearOperations();

        // Assert
        _audioService.GetRecordedOperations().Should().BeEmpty();
        _audioService.CurrentMusicTrack.Should().Be(currentMusic); // State preserved
        _audioService.IsAllStopped.Should().BeFalse(); // State preserved
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void FailureConfiguration_ShouldOnlyAffectConfiguredOperations(
        bool failPlaySound, bool failSetMusic, bool failSetVolume, bool failStopAll)
    {
        // Arrange
        _audioService.ShouldFailPlaySound = failPlaySound;
        _audioService.ShouldFailSetMusic = failSetMusic;
        _audioService.ShouldFailSetVolume = failSetVolume;
        _audioService.ShouldFailStopAll = failStopAll;

        // Act & Assert
        var playSoundResult = _audioService.PlaySound(SoundId.SwordMiss);
        playSoundResult.IsSucc.Should().Be(!failPlaySound);

        var setMusicResult = _audioService.SetMusicTrack(MusicId.MenuTheme);
        setMusicResult.IsSucc.Should().Be(!failSetMusic);

        var setVolumeResult = _audioService.SetBusVolume(AudioBus.Master, 0.9f);
        setVolumeResult.IsSucc.Should().Be(!failSetVolume);

        var stopAllResult = _audioService.StopAll();
        stopAllResult.IsSucc.Should().Be(!failStopAll);
    }
}
