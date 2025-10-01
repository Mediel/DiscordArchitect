using DiscordArchitect.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

/// <summary>
/// Contains unit tests for the ConfigurationValidator class.
/// </summary>
/// <remarks>These tests verify that the ConfigurationValidator correctly identifies valid and invalid
/// configuration scenarios, ensuring proper validation of Discord bot settings before application startup.</remarks>
public class ConfigurationValidatorTests
{
    private static IConfiguration CreateConfiguration(Dictionary<string, string?> configData)
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(configData);
        return configBuilder.Build();
    }

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsValidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:ServerId"] = "123456789012345678",
            ["Discord:SourceCategoryName"] = "Template",
            ["Discord:CreateRolePerCategory"] = "true",
            ["Discord:EveryoneAccessToNewCategory"] = "false",
            ["Discord:SyncChannelsToCategory"] = "true"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingToken_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:ServerId"] = "123456789012345678",
            ["Discord:SourceCategoryName"] = "Template"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:Token is required");
    }

    [Fact]
    public void Validate_WithEmptyToken_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "",
            ["Discord:ServerId"] = "123456789012345678",
            ["Discord:SourceCategoryName"] = "Template"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:Token is required");
    }

    [Fact]
    public void Validate_WithShortToken_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "short",
            ["Discord:ServerId"] = "123456789012345678",
            ["Discord:SourceCategoryName"] = "Template"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:Token appears to be invalid");
    }

    [Fact]
    public void Validate_WithMissingServerId_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:SourceCategoryName"] = "Template"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:ServerId is required");
    }

    [Fact]
    public void Validate_WithInvalidServerId_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:ServerId"] = "invalid",
            ["Discord:SourceCategoryName"] = "Template"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:ServerId must be a valid Discord guild ID");
    }

    [Fact]
    public void Validate_WithZeroServerId_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:ServerId"] = "0",
            ["Discord:SourceCategoryName"] = "Template"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:ServerId must be a valid Discord guild ID");
    }

    [Fact]
    public void Validate_WithMissingSourceCategoryName_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:ServerId"] = "123456789012345678"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Discord:SourceCategoryName is required");
    }

    [Fact]
    public void Validate_WithInvalidBooleanValues_ReturnsInvalidResult()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:ServerId"] = "123456789012345678",
            ["Discord:SourceCategoryName"] = "Template",
            ["Discord:CreateRolePerCategory"] = "maybe",
            ["Discord:EveryoneAccessToNewCategory"] = "yes",
            ["Discord:SyncChannelsToCategory"] = "1"
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.Contains("CreateRolePerCategory must be 'true' or 'false'"));
        result.Errors.Should().Contain(e => e.Contains("EveryoneAccessToNewCategory must be 'true' or 'false'"));
        result.Errors.Should().Contain(e => e.Contains("SyncChannelsToCategory must be 'true' or 'false'"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Discord:Token"] = "",
            ["Discord:ServerId"] = "invalid",
            ["Discord:SourceCategoryName"] = ""
        };

        var config = CreateConfiguration(configData);
        var validator = new ConfigurationValidator(config);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.ErrorMessage.Should().Contain("Discord:Token is required");
        result.ErrorMessage.Should().Contain("Discord:ServerId must be a valid Discord guild ID");
        result.ErrorMessage.Should().Contain("Discord:SourceCategoryName is required");
    }

    [Fact]
    public void ValidationResult_ErrorMessage_CombinesAllErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var result = new ValidationResult(false, errors);

        // Act
        var errorMessage = result.ErrorMessage;

        // Assert
        errorMessage.Should().Be("Error 1\r\nError 2\r\nError 3");
    }
}

