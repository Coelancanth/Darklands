namespace Darklands.Core.Features.Grid.Domain;

/// <summary>
/// Vision system constants (MVP: shared default radius for all actors).
/// </summary>
/// <remarks>
/// FUTURE EVOLUTION (Option A - Per-Actor Vision System):
///
/// When implementing racial bonuses, equipment modifiers, or magical effects:
/// 1. Create VisionRadius value object (int baseRadius + modifiers)
/// 2. Add Vision component to Actor aggregate (base vision + equipped bonuses)
/// 3. Create GetActorVisionRadiusQuery(ActorId) → returns Result{int}
/// 4. Replace VisionConstants.DefaultVisionRadius with query call
/// 5. Benefits: Supports Elf racial bonus (+2 tiles), Darkvision items, blind status effects
///
/// Migration path preserves existing FOV infrastructure (CalculateFOVQuery unchanged).
/// </remarks>
public static class VisionConstants
{
    /// <summary>
    /// Default vision radius for all actors (MVP scope).
    /// </summary>
    /// <remarks>
    /// WHY 8 tiles: Balances tactical visibility (see enemies before engagement)
    /// with fog-of-war exploration tension. Matches roguelike conventions (NetHack, DCSS).
    ///
    /// CURRENT USAGE:
    /// - MoveActorCommandHandler: FOV calculation after movement
    /// - EnemyDetectionEventHandler: Enemy detection radius for combat initiation
    /// - GetVisibleActorsQuery: Hostile actor detection within player's vision
    /// </remarks>
    public const int DefaultVisionRadius = 8;
}
