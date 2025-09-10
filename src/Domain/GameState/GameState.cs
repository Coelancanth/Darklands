using System.Collections.Immutable;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Domain.Grid;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.GameState;

/// <summary>
/// Root aggregate containing all persistent game state.
/// This is the complete state that gets saved and loaded.
/// 
/// Design principles per ADR-005:
/// - Immutable record for safe snapshot creation
/// - ID references instead of object references  
/// - Implements IPersistentEntity for save system compatibility
/// - Contains ModData for extensibility
/// - Separates persistent from transient state
/// </summary>
public sealed record GameState(
    GameSessionId SessionId,
    ulong Seed,
    ulong CurrentTurn,
    Domain.SaveSystem.GamePhase Phase,
    ImmutableDictionary<ActorId, Actor.Actor> Actors,
    ImmutableDictionary<GridId, Grid.Grid> Grids,
    ImmutableDictionary<string, CampaignVariable> Variables,
    RandomState RandomStates,
    ImmutableDictionary<string, string> ModData
) : IPersistentEntity
{
    /// <summary>
    /// IPersistentEntity implementation - exposes session ID for save system.
    /// </summary>
    IEntityId IPersistentEntity.Id => SessionId;

    /// <summary>
    /// Creates a new game state with initial values.
    /// Used when starting a new campaign.
    /// </summary>
    /// <param name="seed">Random seed for deterministic gameplay</param>
    /// <param name="campaignName">Name of this campaign</param>
    /// <param name="modData">Optional mod data</param>
    /// <returns>New game state ready for gameplay</returns>
    public static GameState CreateNew(
        ulong seed,
        string campaignName,
        ImmutableDictionary<string, string>? modData = null)
    {
        var sessionId = GameSessionId.NewId();
        var variables = ImmutableDictionary<string, CampaignVariable>.Empty
            .Add("campaign_name", new CampaignVariable("campaign_name", campaignName, VariableType.String))
            .Add("start_time", new CampaignVariable("start_time", DateTimeOffset.UtcNow.ToString("O"), VariableType.String));

        return new GameState(
            sessionId,
            seed,
            1, // Start at turn 1
            Domain.SaveSystem.GamePhase.CampaignMap,
            ImmutableDictionary<ActorId, Actor.Actor>.Empty,
            ImmutableDictionary<GridId, Grid.Grid>.Empty,
            variables,
            RandomState.CreateNew(seed),
            modData ?? ImmutableDictionary<string, string>.Empty
        );
    }

    /// <summary>
    /// Creates a snapshot of this state that is safe to save.
    /// Since records are immutable, this returns the same instance.
    /// </summary>
    /// <returns>Save-ready snapshot of game state</returns>
    public GameState CreateSaveSnapshot()
    {
        // Records provide immutability for free - this is already safe!
        return this;
    }

    /// <summary>
    /// Advances to the next turn, updating turn counter.
    /// </summary>
    /// <returns>New game state with incremented turn</returns>
    public GameState AdvanceToNextTurn()
    {
        return this with { CurrentTurn = CurrentTurn + 1 };
    }

    /// <summary>
    /// Changes the current game phase (combat, map, etc.).
    /// </summary>
    /// <param name="newPhase">Phase to transition to</param>
    /// <returns>New game state in the specified phase</returns>
    public GameState ChangePhase(Domain.SaveSystem.GamePhase newPhase)
    {
        return this with { Phase = newPhase };
    }

    /// <summary>
    /// Adds or updates an actor in the game state.
    /// </summary>
    /// <param name="actor">Actor to add or update</param>
    /// <returns>New game state with actor included</returns>
    public GameState WithActor(Actor.Actor actor)
    {
        return this with { Actors = Actors.SetItem(actor.Id, actor) };
    }

    /// <summary>
    /// Removes an actor from the game state.
    /// </summary>
    /// <param name="actorId">ID of actor to remove</param>
    /// <returns>New game state without the specified actor</returns>
    public GameState WithoutActor(ActorId actorId)
    {
        return this with { Actors = Actors.Remove(actorId) };
    }

    /// <summary>
    /// Adds or updates a grid in the game state.
    /// </summary>
    /// <param name="grid">Grid to add or update</param>
    /// <returns>New game state with grid included</returns>
    public GameState WithGrid(Grid.Grid grid)
    {
        return this with { Grids = Grids.SetItem(grid.Id, grid) };
    }

    /// <summary>
    /// Sets a campaign variable.
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <param name="value">Variable value</param>
    /// <param name="type">Variable type</param>
    /// <returns>New game state with variable set</returns>
    public GameState SetVariable(string name, string value, VariableType type = VariableType.String)
    {
        var variable = new CampaignVariable(name, value, type);
        return this with { Variables = Variables.SetItem(name, variable) };
    }

    /// <summary>
    /// Gets a campaign variable value.
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <returns>Variable value or None if not found</returns>
    public Option<CampaignVariable> GetVariable(string name)
    {
        return Variables.TryGetValue(name, out var variable) ? Some(variable) : None;
    }

    /// <summary>
    /// Updates the random state for all generators.
    /// </summary>
    /// <param name="newRandomState">New random state</param>
    /// <returns>New game state with updated random state</returns>
    public GameState WithRandomState(RandomState newRandomState)
    {
        return this with { RandomStates = newRandomState };
    }

    public override string ToString() =>
        $"GameState(Turn {CurrentTurn}, Phase: {Phase}, {Actors.Count} actors, {Grids.Count} grids)";
}

/// <summary>
/// Unique identifier for game sessions.
/// </summary>
public readonly record struct GameSessionId(Guid Value) : IEntityId
{
    public static GameSessionId NewId() => new(Guid.NewGuid());
    public static GameSessionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString("N")[..8];
}

/// <summary>
/// Campaign variable that can be saved and modified by mods.
/// </summary>
public sealed record CampaignVariable(
    string Name,
    string Value,
    VariableType Type
) : IPersistentEntity
{
    /// <summary>
    /// Unique identifier for this variable.
    /// </summary>
    public CampaignVariableId Id { get; init; } = new(Name);

    /// <summary>
    /// IPersistentEntity implementation.
    /// </summary>
    IEntityId IPersistentEntity.Id => Id;

    /// <summary>
    /// Gets the value as the specified type.
    /// </summary>
    /// <typeparam name="T">Type to convert to</typeparam>
    /// <returns>Converted value or error if conversion failed</returns>
    public Fin<T> GetValue<T>()
    {
        try
        {
            return Type switch
            {
                VariableType.String => (T)(object)Value,
                VariableType.Integer => (T)(object)int.Parse(Value),
                VariableType.Boolean => (T)(object)bool.Parse(Value),
                VariableType.Float => (T)(object)float.Parse(Value),
                _ => throw new NotSupportedException($"Variable type {Type} not supported")
            };
        }
        catch (Exception ex)
        {
            return Error.New($"Failed to convert variable '{Name}' value '{Value}' to {typeof(T).Name}: {ex.Message}");
        }
    }
}

/// <summary>
/// Unique identifier for campaign variables.
/// </summary>
public readonly record struct CampaignVariableId(string Name) : IEntityId
{
    public Guid Value => new(Name.PadRight(32, '0')[..32].Select(c => (byte)c).ToArray());
    public bool IsEmpty => string.IsNullOrEmpty(Name);
    public override string ToString() => Name;
}

/// <summary>
/// Types of campaign variables.
/// </summary>
public enum VariableType
{
    String,
    Integer,
    Boolean,
    Float
}
