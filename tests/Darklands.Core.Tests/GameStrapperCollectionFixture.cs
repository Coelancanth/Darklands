using Xunit;

namespace Darklands.Core.Tests;

/// <summary>
/// xUnit collection definition to prevent parallel execution of tests that share GameStrapper static state.
/// All tests marked with [Collection("GameStrapperCollection")] will run sequentially.
/// </summary>
[CollectionDefinition("GameStrapperCollection")]
public class GameStrapperCollectionFixture
{
    // This class is never instantiated - it's just a marker for xUnit
}