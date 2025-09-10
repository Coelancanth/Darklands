using Xunit;

namespace Darklands.Core.Tests.Infrastructure.DependencyInjection;

/// <summary>
/// Test collection to ensure GameStrapper-dependent tests run sequentially.
/// Prevents race conditions and ServiceProvider disposal issues when tests run in parallel.
/// </summary>
[CollectionDefinition("GameStrapper", DisableParallelization = true)]
public class GameStrapperCollection : ICollectionFixture<GameStrapperCollection>
{
    // This class is used only to define the test collection
    // Tests in this collection will run sequentially, not in parallel
}
