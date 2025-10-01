using DiscordArchitect.Options;
using DiscordArchitect.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

public class VerificationServiceTests
{
    private readonly Mock<ILogger<VerificationService>> _mockLogger;
    private readonly VerificationService _verificationService;

    public VerificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<VerificationService>>();
        _verificationService = new VerificationService(_mockLogger.Object);
    }

    [Fact]
    public void VerificationService_Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        _verificationService.Should().NotBeNull();
    }

    [Fact]
    public void VerificationResult_Constructor_ShouldSetProperties()
    {
        // Arrange
        var findings = new List<VerificationFinding>
        {
            new(VerificationType.Success, "Test", "Message", "Description")
        };
        var recommendations = new List<string> { "Test recommendation" };
        var summary = "Test summary";

        // Act
        var result = new VerificationResult(findings, recommendations, summary);

        // Assert
        result.Findings.Should().BeEquivalentTo(findings);
        result.Recommendations.Should().BeEquivalentTo(recommendations);
        result.Summary.Should().Be(summary);
    }

    [Fact]
    public void VerificationFinding_Constructor_ShouldSetProperties()
    {
        // Arrange
        var type = VerificationType.Warning;
        var category = "TestCategory";
        var message = "Test message";
        var description = "Test description";

        // Act
        var finding = new VerificationFinding(type, category, message, description);

        // Assert
        finding.Type.Should().Be(type);
        finding.Category.Should().Be(category);
        finding.Message.Should().Be(message);
        finding.Description.Should().Be(description);
    }

    [Fact]
    public void VerificationType_Enum_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)VerificationType.Success).Should().Be(0);
        ((int)VerificationType.Warning).Should().Be(1);
        ((int)VerificationType.Error).Should().Be(2);
        ((int)VerificationType.Info).Should().Be(3);
    }
}
