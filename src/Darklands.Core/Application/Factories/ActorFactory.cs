using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Components;
using Darklands.Core.Domain.Entities;

namespace Darklands.Core.Application.Factories;

/// <summary>
/// Factory for creating Actor entities from template data.
/// Integrates with ADR-006 data-driven entity design.
/// </summary>
/// <remarks>
/// <para><b>Cookie Cutter Pattern</b>:</para>
/// <para>
/// Template = cookie cutter (reusable configuration)
/// Entity = cookie (independent instance with copied data)
/// Actor has NO reference to template after creation.
/// </para>
///
/// <para><b>Component Assembly</b>:</para>
/// <para>
/// Factory reads template properties and conditionally adds components:
/// - MaxHealth > 0 → Add HealthComponent
/// - WeaponDamage > 0 → Add WeaponComponent
/// - Future: HasEquipment flag → Add EquipmentComponent
/// </para>
///
/// <para><b>Usage</b>:</para>
/// <code>
/// // Get template from service
/// var templateResult = _templates.GetTemplate("goblin");
///
/// // Create actor from template
/// var actorResult = ActorFactory.CreateFromTemplate(templateResult.Value);
///
/// // Actor is independent - template can be modified without affecting existing actors
/// var actor = actorResult.Value; // Has HealthComponent + WeaponComponent
/// </code>
/// </remarks>
public static class ActorFactory
{
    /// <summary>
    /// Creates an Actor entity from template data.
    /// Conditionally adds components based on template properties.
    /// </summary>
    /// <param name="templateData">Template data containing actor stats</param>
    /// <returns>Result with Actor if successful, Failure if template data invalid</returns>
    /// <remarks>
    /// <para><b>Template Data Structure</b>:</para>
    /// <para>
    /// Must implement interface with these properties:
    /// - Id (string): Template identifier
    /// - NameKey (string): Translation key for actor name
    /// - MaxHealth (float): Maximum health points
    /// - WeaponNameKey (string): Translation key for weapon name (empty = unarmed)
    /// - WeaponDamage (float): Weapon damage (0 = no weapon)
    /// - WeaponTimeCost (int): Attack time cost
    /// - WeaponRange (int): Attack range in tiles
    /// - WeaponType (int): 0=Melee, 1=Ranged
    /// </para>
    /// </remarks>
    public static Result<Actor> CreateFromTemplate(IActorTemplateData templateData)
    {
        // Validate template ID
        if (string.IsNullOrWhiteSpace(templateData.Id))
        {
            return Result.Failure<Actor>("Template ID cannot be empty");
        }

        // Validate name key
        if (string.IsNullOrWhiteSpace(templateData.NameKey))
        {
            return Result.Failure<Actor>($"Template {templateData.Id} has empty NameKey");
        }

        // Create actor entity (just ID + name)
        var actor = new Actor(ActorId.NewId(), templateData.NameKey);

        // Add HealthComponent if max health > 0
        if (templateData.MaxHealth > 0)
        {
            var healthResult = Health.Create(templateData.MaxHealth, templateData.MaxHealth);
            if (healthResult.IsFailure)
            {
                return Result.Failure<Actor>(
                    $"Template {templateData.Id} has invalid MaxHealth: {healthResult.Error}");
            }

            var healthComponent = new HealthComponent(healthResult.Value);
            var addResult = actor.AddComponent<IHealthComponent>(healthComponent);

            if (addResult.IsFailure)
            {
                return Result.Failure<Actor>(
                    $"Failed to add HealthComponent to actor: {addResult.Error}");
            }
        }

        // Add WeaponComponent if weapon configured (damage > 0 and name key not empty)
        if (templateData.WeaponDamage > 0 && !string.IsNullOrWhiteSpace(templateData.WeaponNameKey))
        {
            var weaponTypeEnum = templateData.WeaponType == 0
                ? WeaponType.Melee
                : WeaponType.Ranged;

            var weaponResult = Weapon.Create(
                templateData.WeaponNameKey,
                templateData.WeaponDamage,
                templateData.WeaponTimeCost,
                templateData.WeaponRange,
                weaponTypeEnum);

            if (weaponResult.IsFailure)
            {
                return Result.Failure<Actor>(
                    $"Template {templateData.Id} has invalid weapon config: {weaponResult.Error}");
            }

            var weaponComponent = new WeaponComponent(weaponResult.Value);
            var addResult = actor.AddComponent<IWeaponComponent>(weaponComponent);

            if (addResult.IsFailure)
            {
                return Result.Failure<Actor>(
                    $"Failed to add WeaponComponent to actor: {addResult.Error}");
            }
        }

        return Result.Success(actor);
    }
}

/// <summary>
/// Interface for actor template data.
/// Allows factory to work with any template source (Godot Resources, JSON, etc.).
/// </summary>
public interface IActorTemplateData
{
    /// <summary>Template identifier (e.g., "goblin")</summary>
    string Id { get; }

    /// <summary>Translation key for actor name (e.g., "ACTOR_GOBLIN")</summary>
    string NameKey { get; }

    /// <summary>Maximum health points</summary>
    float MaxHealth { get; }

    /// <summary>Translation key for weapon name (empty = no weapon)</summary>
    string WeaponNameKey { get; }

    /// <summary>Weapon damage per attack</summary>
    float WeaponDamage { get; }

    /// <summary>Time units per attack</summary>
    int WeaponTimeCost { get; }

    /// <summary>Maximum attack range in tiles</summary>
    int WeaponRange { get; }

    /// <summary>Weapon type: 0=Melee, 1=Ranged</summary>
    int WeaponType { get; }
}
