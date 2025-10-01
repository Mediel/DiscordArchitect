using DiscordArchitect.Services;
using FluentAssertions;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

/// <summary>
/// Contains unit tests for the CreatedResources record.
/// </summary>
/// <remarks>These tests verify that the CreatedResources record correctly stores
/// information about resources created during test mode operations.</remarks>
public class CategoryClonerTrackingTests
{

    [Fact]
    public void CreatedResources_Constructor_SetsProperties()
    {
        // Arrange
        var categoryId = 123456789012345678UL;
        var channels = new List<ulong> { 111111111111111111UL, 222222222222222222UL };
        var roleId = 333333333333333333UL;

        // Act
        var result = new CreatedResources(categoryId, channels, roleId);

        // Assert
        result.CategoryId.Should().Be(categoryId);
        result.Channels.Should().BeEquivalentTo(channels);
        result.RoleId.Should().Be(roleId);
    }

    [Fact]
    public void CreatedResources_WithNullValues_SetsNullProperties()
    {
        // Arrange
        var channels = new List<ulong> { 111111111111111111UL };

        // Act
        var result = new CreatedResources(null, channels, null);

        // Assert
        result.CategoryId.Should().BeNull();
        result.Channels.Should().BeEquivalentTo(channels);
        result.RoleId.Should().BeNull();
    }

    [Fact]
    public void CreatedResources_WithEmptyChannels_SetsEmptyList()
    {
        // Arrange
        var categoryId = 123456789012345678UL;
        var channels = new List<ulong>();

        // Act
        var result = new CreatedResources(categoryId, channels, null);

        // Assert
        result.CategoryId.Should().Be(categoryId);
        result.Channels.Should().BeEmpty();
        result.RoleId.Should().BeNull();
    }
}
