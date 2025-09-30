using FluentAssertions;
using Sigma.Domain.ValueObjects;
using Xunit;

namespace Sigma.Domain.Tests.ValueObjects;

public class MessageReactionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var key = "thumbs_up";
        var count = 5;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Should().NotBeNull();
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(count);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? key = null;
        var count = 5;

        // Act & Assert
        var action = () => new MessageReaction(key!, count);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Fact]
    public void Constructor_WithEmptyKey_ShouldCreateInstance()
    {
        // Arrange
        var key = string.Empty;
        var count = 5;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().BeEmpty();
        reaction.Count.Should().Be(count);
    }

    [Fact]
    public void Constructor_WithNegativeCount_ShouldSetCountToZero()
    {
        // Arrange
        var key = "thumbs_up";
        var count = -10;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithZeroCount_ShouldCreateInstance()
    {
        // Arrange
        var key = "thumbs_up";
        var count = 0;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithMaxIntCount_ShouldCreateInstance()
    {
        // Arrange
        var key = "thumbs_up";
        var count = int.MaxValue;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var key = "thumbs_up";
        var count = 5;

        var reaction1 = new MessageReaction(key, count);
        var reaction2 = new MessageReaction(key, count);

        // Act & Assert
        reaction1.Should().Be(reaction2);
        reaction1.Equals(reaction2).Should().BeTrue();
        (reaction1 == reaction2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentKey_ShouldReturnFalse()
    {
        // Arrange
        var reaction1 = new MessageReaction("thumbs_up", 5);
        var reaction2 = new MessageReaction("thumbs_down", 5);

        // Act & Assert
        reaction1.Should().NotBe(reaction2);
        reaction1.Equals(reaction2).Should().BeFalse();
        (reaction1 == reaction2).Should().BeFalse();
        (reaction1 != reaction2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCount_ShouldReturnFalse()
    {
        // Arrange
        var reaction1 = new MessageReaction("thumbs_up", 5);
        var reaction2 = new MessageReaction("thumbs_up", 10);

        // Act & Assert
        reaction1.Should().NotBe(reaction2);
        reaction1.Equals(reaction2).Should().BeFalse();
        (reaction1 == reaction2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var reaction = new MessageReaction("thumbs_up", 5);

        // Act & Assert
        reaction.Equals(null).Should().BeFalse();
        (reaction == null).Should().BeFalse();
        (null == reaction).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var reaction = new MessageReaction("thumbs_up", 5);
        var other = "not a reaction";

        // Act & Assert
        reaction.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var key = "thumbs_up";
        var count = 5;

        var reaction1 = new MessageReaction(key, count);
        var reaction2 = new MessageReaction(key, count);

        // Act & Assert
        reaction1.GetHashCode().Should().Be(reaction2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var reaction1 = new MessageReaction("thumbs_up", 5);
        var reaction2 = new MessageReaction("thumbs_down", 10);

        // Act & Assert
        reaction1.GetHashCode().Should().NotBe(reaction2.GetHashCode());
    }

    [Theory]
    [InlineData("like", 1)]
    [InlineData("love", 10)]
    [InlineData("haha", 100)]
    [InlineData("wow", 1000)]
    [InlineData("sad", 10000)]
    [InlineData("angry", 100000)]
    public void Constructor_WithVariousValidInputs_ShouldCreateValidInstances(string key, int count)
    {
        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(count);
    }

    [Theory]
    [InlineData("key", -1, 0)]
    [InlineData("key", -100, 0)]
    [InlineData("key", int.MinValue, 0)]
    public void Constructor_WithVariousNegativeCounts_ShouldSetCountToZero(string key, int inputCount, int expectedCount)
    {
        // Act
        var reaction = new MessageReaction(key, inputCount);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(expectedCount);
    }

    [Fact]
    public void Constructor_WithLongKey_ShouldCreateInstance()
    {
        // Arrange
        var key = new string('x', 10000);
        var count = 5;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(count);
    }

    [Fact]
    public void Constructor_WithSpecialCharactersInKey_ShouldCreateInstance()
    {
        // Arrange
        var key = "👍😀🎉~!@#$%^&*()_+-=[]{}|;':\",./<>?";
        var count = 5;

        // Act
        var reaction = new MessageReaction(key, count);

        // Assert
        reaction.Key.Should().Be(key);
        reaction.Count.Should().Be(count);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly()
    {
        // Arrange
        var reaction = new MessageReaction("thumbs_up", 5);

        // Act & Assert
        reaction.GetType().GetProperty("Key")!.GetSetMethod(true)!.IsPrivate.Should().BeTrue();
        reaction.GetType().GetProperty("Count")!.GetSetMethod(true)!.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void Equals_ComparingNegativeCountConvertedToZero_ShouldBeEqual()
    {
        // Arrange
        var reaction1 = new MessageReaction("key", -5);
        var reaction2 = new MessageReaction("key", -10);
        var reaction3 = new MessageReaction("key", 0);

        // Act & Assert
        reaction1.Should().Be(reaction2);
        reaction1.Should().Be(reaction3);
        reaction2.Should().Be(reaction3);
    }

    [Fact]
    public void GetHashCode_WithNegativeCountConvertedToZero_ShouldBeSame()
    {
        // Arrange
        var reaction1 = new MessageReaction("key", -5);
        var reaction2 = new MessageReaction("key", 0);

        // Act & Assert
        reaction1.GetHashCode().Should().Be(reaction2.GetHashCode());
    }
}