using Godot;

namespace Darklands.Tests;

/// <summary>
/// Test scene to verify i18n (translation) system is working correctly.
/// Phase 1 of VS_021 - validates tr() function and translation keys.
/// </summary>
public partial class I18nTest : Control
{
    private Label _actorLabel = null!;
    private Label _uiLabel = null!;
    private Label _errorLabel = null!;

    public override void _Ready()
    {
        _actorLabel = GetNode<Label>("VBoxContainer/ActorLabel");
        _uiLabel = GetNode<Label>("VBoxContainer/UILabel");
        _errorLabel = GetNode<Label>("VBoxContainer/ErrorLabel");

        // Test translation of different key types
        TestTranslations();
    }

    private void TestTranslations()
    {
        // Test ACTOR_ key
        var actorName = Tr("ACTOR_PLAYER");
        _actorLabel.Text = $"Actor Name: {actorName}";
        GD.Print($"[i18n Test] ACTOR_PLAYER -> '{actorName}' (expected: 'Player')");

        // Test UI_ key
        var uiText = Tr("UI_BUTTON_ATTACK");
        _uiLabel.Text = $"UI Text: {uiText}";
        GD.Print($"[i18n Test] UI_BUTTON_ATTACK -> '{uiText}' (expected: 'Attack')");

        // Test ERROR_ key
        var errorText = Tr("ERROR_DAMAGE_NEGATIVE");
        _errorLabel.Text = $"Error: {errorText}";
        GD.Print($"[i18n Test] ERROR_DAMAGE_NEGATIVE -> '{errorText}' (expected: 'Damage cannot be negative')");

        // Test missing key (should return raw key)
        var missingKey = Tr("MISSING_KEY_TEST");
        GD.Print($"[i18n Test] MISSING_KEY_TEST -> '{missingKey}' (expected: 'MISSING_KEY_TEST' - raw key fallback)");

        // Validation
        bool allPassed = actorName == "Player" &&
                        uiText == "Attack" &&
                        errorText == "Damage cannot be negative" &&
                        missingKey == "MISSING_KEY_TEST";

        if (allPassed)
        {
            GD.Print("[i18n Test] ✅ All translations working correctly!");
        }
        else
        {
            GD.PrintErr("[i18n Test] ❌ Translation test FAILED! Check en.csv and project.godot configuration.");
        }
    }
}
