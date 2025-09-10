using Xunit;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Darklands.Core.Infrastructure.Services;
using Darklands.Core.Domain.Services;
using System.Collections.Generic;

namespace Darklands.Core.Tests.Infrastructure.Services;

[Trait("Category", "Phase2")]
public class MockSettingsServiceTests
{
    private readonly MockSettingsService _settingsService;

    public MockSettingsServiceTests()
    {
        _settingsService = new MockSettingsService();
    }

    [Fact]
    public void Get_WithUnsetKey_ShouldReturnDefaultValue()
    {
        // Arrange
        var testKey = new SettingKey<float>("test.volume", 0.5f);

        // Act
        var result = _settingsService.Get(testKey);

        // Assert
        result.Should().Be(0.5f);
    }

    [Fact]
    public void Set_WithValidKey_ShouldUpdateValue()
    {
        // Arrange
        var testKey = new SettingKey<bool>("test.enabled", false);
        var newValue = true;

        // Act
        var result = _settingsService.Set(testKey, newValue);

        // Assert
        result.IsSucc.Should().BeTrue();
        _settingsService.Get(testKey).Should().Be(newValue);
        _settingsService.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public void Set_WithNullKey_ShouldReturnFailure()
    {
        // Act
        var result = _settingsService.Set<int>(null!, 42);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => Assert.Fail("Should have failed"),
            Fail: error => error.Message.Should().Contain("Setting key cannot be null")
        );
    }

    [Fact]
    public void Save_WithoutFailureConfiguration_ShouldSucceedAndClearUnsavedFlag()
    {
        // Arrange
        var testKey = GameSettings.MasterVolume;
        _settingsService.Set(testKey, 0.7f);
        _settingsService.HasUnsavedChanges.Should().BeTrue();

        // Act
        var result = _settingsService.Save();

        // Assert
        result.IsSucc.Should().BeTrue();
        _settingsService.HasUnsavedChanges.Should().BeFalse();
        _settingsService.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public void Save_WithFailureConfigured_ShouldReturnFailure()
    {
        // Arrange
        _settingsService.ShouldFailSave = true;

        // Act
        var result = _settingsService.Save();

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => Assert.Fail("Should have failed"),
            Fail: error => error.Message.Should().Contain("Mock configured to fail Save")
        );
        _settingsService.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public void Reload_WithoutFailureConfiguration_ShouldSucceedAndClearUnsavedFlag()
    {
        // Arrange
        _settingsService.Set(GameSettings.ShowGridLines, false);
        _settingsService.HasUnsavedChanges.Should().BeTrue();

        // Act
        var result = _settingsService.Reload();

        // Assert
        result.IsSucc.Should().BeTrue();
        _settingsService.HasUnsavedChanges.Should().BeFalse();
        _settingsService.ReloadCallCount.Should().Be(1);
    }

    [Fact]
    public void ResetToDefault_ShouldSetKeyToDefaultValue()
    {
        // Arrange
        var testKey = GameSettings.CombatAnimationSpeed;
        _settingsService.Set(testKey, 2.0f); // Change from default 1.0f

        // Act
        var result = _settingsService.ResetToDefault(testKey);

        // Assert
        result.IsSucc.Should().BeTrue();
        _settingsService.Get(testKey).Should().Be(1.0f); // Back to default
        _settingsService.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public void ResetAllToDefaults_ShouldClearAllSettings()
    {
        // Arrange
        _settingsService.Set(GameSettings.Fullscreen, true);
        _settingsService.Set(GameSettings.MusicVolume, 0.3f);
        _settingsService.Set(GameSettings.ShowDebugOverlay, true);

        var settingsBefore = _settingsService.GetAllSettings();
        settingsBefore.Should().HaveCount(3);

        // Act
        var result = _settingsService.ResetAllToDefaults();

        // Assert
        result.IsSucc.Should().BeTrue();
        _settingsService.GetAllSettings().Should().BeEmpty();
        _settingsService.HasUnsavedChanges.Should().BeTrue();

        // Verify values return to defaults
        _settingsService.Get(GameSettings.Fullscreen).Should().Be(false);
        _settingsService.Get(GameSettings.MusicVolume).Should().Be(0.8f);
        _settingsService.Get(GameSettings.ShowDebugOverlay).Should().Be(false);
    }

    [Fact]
    public void PreloadSettings_ShouldInitializeWithGivenValues()
    {
        // Arrange
        var initialSettings = new Dictionary<string, object>
        {
            { "test.string", "hello" },
            { "test.number", 42 },
            { "test.boolean", true }
        };

        // Act
        _settingsService.PreloadSettings(initialSettings);

        // Assert
        _settingsService.HasUnsavedChanges.Should().BeFalse();

        var stringKey = new SettingKey<string>("test.string", "default");
        var numberKey = new SettingKey<int>("test.number", 0);
        var boolKey = new SettingKey<bool>("test.boolean", false);

        _settingsService.Get(stringKey).Should().Be("hello");
        _settingsService.Get(numberKey).Should().Be(42);
        _settingsService.Get(boolKey).Should().Be(true);
    }

    [Fact]
    public void SimulateExternalSettingsChange_ShouldUpdateSettingsWithoutUnsavedFlag()
    {
        // Arrange
        var externalChanges = new Dictionary<string, object>
        {
            { GameSettings.ResolutionWidth.Key, 2560 },
            { GameSettings.ResolutionHeight.Key, 1440 }
        };

        // Act
        _settingsService.SimulateExternalSettingsChange(externalChanges);

        // Assert
        _settingsService.HasUnsavedChanges.Should().BeFalse(); // External changes don't set unsaved flag
        _settingsService.Get(GameSettings.ResolutionWidth).Should().Be(2560);
        _settingsService.Get(GameSettings.ResolutionHeight).Should().Be(1440);
    }

    [Fact]
    public void Reset_ShouldClearAllStateAndConfiguration()
    {
        // Arrange
        _settingsService.Set(GameSettings.MouseSensitivity, 2.0f);
        _settingsService.ShouldFailSave = true;
        _settingsService.ShouldFailReload = true;
        _settingsService.Save(); // Increment counters

        // Act
        _settingsService.Reset();

        // Assert
        _settingsService.GetAllSettings().Should().BeEmpty();
        _settingsService.HasUnsavedChanges.Should().BeFalse();
        _settingsService.SaveCallCount.Should().Be(0);
        _settingsService.ReloadCallCount.Should().Be(0);
        _settingsService.ShouldFailSave.Should().BeFalse();
        _settingsService.ShouldFailReload.Should().BeFalse();
        _settingsService.ShouldFailSet.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(bool), false)]
    [InlineData(typeof(int), 100)]
    [InlineData(typeof(float), 1.5f)]
    [InlineData(typeof(string), "test")]
    public void TypedSettings_ShouldHandleVariousTypes(Type settingType, object value)
    {
        // This test ensures the mock handles different types correctly
        if (settingType == typeof(bool))
        {
            var key = new SettingKey<bool>("test.bool", true);
            var result = _settingsService.Set(key, (bool)value);
            result.IsSucc.Should().BeTrue();
            _settingsService.Get(key).Should().Be((bool)value);
        }
        else if (settingType == typeof(int))
        {
            var key = new SettingKey<int>("test.int", 0);
            var result = _settingsService.Set(key, (int)value);
            result.IsSucc.Should().BeTrue();
            _settingsService.Get(key).Should().Be((int)value);
        }
        else if (settingType == typeof(float))
        {
            var key = new SettingKey<float>("test.float", 0.0f);
            var result = _settingsService.Set(key, (float)value);
            result.IsSucc.Should().BeTrue();
            _settingsService.Get(key).Should().Be((float)value);
        }
        else if (settingType == typeof(string))
        {
            var key = new SettingKey<string>("test.string", "default");
            var result = _settingsService.Set(key, (string)value);
            result.IsSucc.Should().BeTrue();
            _settingsService.Get(key).Should().Be((string)value);
        }
    }

    [Fact]
    public void GameSettings_StaticKeys_ShouldHaveCorrectDefaults()
    {
        // This test verifies our predefined game settings have sensible defaults

        // Audio defaults
        _settingsService.Get(GameSettings.MasterVolume).Should().Be(1.0f);
        _settingsService.Get(GameSettings.MusicVolume).Should().Be(0.8f);
        _settingsService.Get(GameSettings.SfxVolume).Should().Be(1.0f);
        _settingsService.Get(GameSettings.UiVolume).Should().Be(0.9f);

        // Display defaults
        _settingsService.Get(GameSettings.Fullscreen).Should().BeFalse();
        _settingsService.Get(GameSettings.ResolutionWidth).Should().Be(1920);
        _settingsService.Get(GameSettings.ResolutionHeight).Should().Be(1080);
        _settingsService.Get(GameSettings.VSync).Should().BeTrue();

        // Gameplay defaults
        _settingsService.Get(GameSettings.ShowGridLines).Should().BeTrue();
        _settingsService.Get(GameSettings.ShowMovementPreview).Should().BeTrue();
        _settingsService.Get(GameSettings.CombatAnimationSpeed).Should().Be(1.0f);

        // Debug defaults
        _settingsService.Get(GameSettings.ShowDebugOverlay).Should().BeFalse();
        _settingsService.Get(GameSettings.EnableDevConsole).Should().BeFalse();
    }

    [Fact]
    public void FailureConfiguration_ShouldOnlyAffectConfiguredOperations()
    {
        // Arrange
        _settingsService.ShouldFailSet = true;

        // Act & Assert
        var setResult = _settingsService.Set(GameSettings.MaxFps, 144);
        setResult.IsFail.Should().BeTrue();

        var saveResult = _settingsService.Save(); // Should still work
        saveResult.IsSucc.Should().BeTrue();

        var reloadResult = _settingsService.Reload(); // Should still work  
        reloadResult.IsSucc.Should().BeTrue();
    }
}
