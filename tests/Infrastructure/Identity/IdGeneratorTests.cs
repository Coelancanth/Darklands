using FluentAssertions;
using Xunit;
using Darklands.Core.Infrastructure.Identity;
using Darklands.Core.Domain.Determinism;
using Darklands.Core.Domain.Common;
using System.Collections.Generic;

namespace Darklands.Core.Tests.Infrastructure.Identity;

public class IdGeneratorTests
{
    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void GuidIdGenerator_NewGuid_GeneratesUniqueGuids()
    {
        var generator = new GuidIdGenerator();
        var guids = new HashSet<Guid>();

        // Generate 1000 GUIDs and ensure they're all unique
        for (int i = 0; i < 1000; i++)
        {
            var guid = generator.NewGuid();
            guids.Add(guid).Should().BeTrue($"GUID {guid} should be unique");
        }

        guids.Should().HaveCount(1000, "All generated GUIDs should be unique");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void GuidIdGenerator_NewStringId_GeneratesUniqueStrings()
    {
        var generator = new GuidIdGenerator();
        var strings = new HashSet<string>();

        // Generate 1000 string IDs and ensure they're all unique
        for (int i = 0; i < 1000; i++)
        {
            var stringId = generator.NewStringId(26);
            strings.Add(stringId).Should().BeTrue($"String ID {stringId} should be unique");
        }

        strings.Should().HaveCount(1000, "All generated string IDs should be unique");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void GuidIdGenerator_NewStringId_WithSpecifiedLength_GeneratesCorrectLength()
    {
        var generator = new GuidIdGenerator();

        var shortId = generator.NewStringId(10);
        var mediumId = generator.NewStringId(26);
        var longId = generator.NewStringId(50);

        shortId.Should().HaveLength(10);
        mediumId.Should().HaveLength(26);
        longId.Should().HaveLength(50);
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void GuidIdGenerator_NewStringId_WithInvalidLength_ThrowsException()
    {
        var generator = new GuidIdGenerator();

        var act = () => generator.NewStringId(0);
        act.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => generator.NewStringId(-1);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void GuidIdGenerator_ImplementsIStableIdGenerator()
    {
        var generator = new GuidIdGenerator();

        generator.Should().BeAssignableTo<IStableIdGenerator>();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_WithSameSeed_GeneratesSameSequence()
    {
        const ulong seed = 12345UL;
        var random1 = new DeterministicRandom(seed);
        var random2 = new DeterministicRandom(seed);

        var generator1 = new DeterministicIdGenerator(random1);
        var generator2 = new DeterministicIdGenerator(random2);

        // Generate 10 GUIDs from each generator
        var guids1 = new List<Guid>();
        var guids2 = new List<Guid>();

        for (int i = 0; i < 10; i++)
        {
            guids1.Add(generator1.NewGuid());
            guids2.Add(generator2.NewGuid());
        }

        // Should be identical sequences
        guids1.Should().Equal(guids2, "Same seed should produce same GUID sequence");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_WithSameSeed_GeneratesSameStringSequence()
    {
        const ulong seed = 54321UL;
        var random1 = new DeterministicRandom(seed);
        var random2 = new DeterministicRandom(seed);

        var generator1 = new DeterministicIdGenerator(random1);
        var generator2 = new DeterministicIdGenerator(random2);

        // Generate 10 string IDs from each generator
        var strings1 = new List<string>();
        var strings2 = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            strings1.Add(generator1.NewStringId(26));
            strings2.Add(generator2.NewStringId(26));
        }

        // Should be identical sequences
        strings1.Should().Equal(strings2, "Same seed should produce same string ID sequence");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_WithDifferentSeeds_GeneratesDifferentSequences()
    {
        const ulong seed1 = 11111UL;
        const ulong seed2 = 22222UL;

        var random1 = new DeterministicRandom(seed1);
        var random2 = new DeterministicRandom(seed2);

        var generator1 = new DeterministicIdGenerator(random1);
        var generator2 = new DeterministicIdGenerator(random2);

        // Generate 10 GUIDs from each generator
        var guids1 = new List<Guid>();
        var guids2 = new List<Guid>();

        for (int i = 0; i < 10; i++)
        {
            guids1.Add(generator1.NewGuid());
            guids2.Add(generator2.NewGuid());
        }

        // Should be different sequences
        guids1.Should().NotEqual(guids2, "Different seeds should produce different GUID sequences");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_NewStringId_WithSpecifiedLength_GeneratesCorrectLength()
    {
        var random = new DeterministicRandom(12345UL);
        var generator = new DeterministicIdGenerator(random);

        var shortId = generator.NewStringId(10);
        var mediumId = generator.NewStringId(26);
        var longId = generator.NewStringId(50);

        shortId.Should().HaveLength(10);
        mediumId.Should().HaveLength(26);
        longId.Should().HaveLength(50);
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_NewStringId_WithInvalidLength_ThrowsException()
    {
        var random = new DeterministicRandom(12345UL);
        var generator = new DeterministicIdGenerator(random);

        var act = () => generator.NewStringId(0);
        act.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => generator.NewStringId(-1);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_WithNullRandom_ThrowsArgumentNullException()
    {
        var act = () => new DeterministicIdGenerator(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_ImplementsIStableIdGenerator()
    {
        var random = new DeterministicRandom(12345UL);
        var generator = new DeterministicIdGenerator(random);

        generator.Should().BeAssignableTo<IStableIdGenerator>();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_GeneratesValidGuidVersionAndVariant()
    {
        var random = new DeterministicRandom(12345UL);
        var generator = new DeterministicIdGenerator(random);

        for (int i = 0; i < 100; i++)
        {
            var guid = generator.NewGuid();
            var bytes = guid.ToByteArray();

            // Check version (4) in the correct position
            var version = (bytes[6] & 0xF0) >> 4;
            version.Should().Be(4, "Generated GUID should be version 4");

            // Check variant (10 pattern) in the correct position  
            var variant = (bytes[8] & 0xC0) >> 6;
            variant.Should().Be(2, "Generated GUID should have variant 10 (binary)");
        }
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void DeterministicIdGenerator_StringIds_ContainOnlyBase62Characters()
    {
        var random = new DeterministicRandom(12345UL);
        var generator = new DeterministicIdGenerator(random);
        const string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        for (int i = 0; i < 100; i++)
        {
            var stringId = generator.NewStringId(26);

            foreach (char c in stringId)
            {
                validChars.Should().Contain(c.ToString(), $"Generated string ID should only contain base62 characters, found: {c}");
            }
        }
    }
}
