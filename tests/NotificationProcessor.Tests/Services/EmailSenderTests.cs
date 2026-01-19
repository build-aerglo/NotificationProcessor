using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NotificationProcessor.Core.Models;
using NotificationProcessor.Infrastructure.Services;

namespace NotificationProcessor.Tests.Services;

[TestFixture]
public class EmailSenderTests
{
    private Mock<ILogger<EmailSender>> _mockLogger;
    private AzureEmailConfiguration _validConfig;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<EmailSender>>();
        _validConfig = new AzureEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey123==",
            SenderAddress = "DoNotReply@test.com"
        };
    }

    [Test]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EmailSender(_mockLogger.Object, null!));
    }

    [Test]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new AzureEmailConfiguration
        {
            ConnectionString = "",
            SenderAddress = "DoNotReply@test.com"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new EmailSender(_mockLogger.Object, invalidConfig));
        Assert.That(ex.Message, Does.Contain("Connection String"));
    }

    [Test]
    public void Constructor_WithEmptySenderAddress_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new AzureEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey123==",
            SenderAddress = ""
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new EmailSender(_mockLogger.Object, invalidConfig));
        Assert.That(ex.Message, Does.Contain("Sender Address"));
    }

    [Test]
    public async Task SendEmailAsync_WithEmptyRecipient_ReturnsFalseAndLogsError()
    {
        // Arrange
        var emailSender = new EmailSender(_mockLogger.Object, _validConfig);

        // Act
        var result = await emailSender.SendEmailAsync("", "Test Subject", "<html>Test Body</html>");

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recipient email address cannot be empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_WithEmptySubject_LogsWarning()
    {
        // Arrange
        var emailSender = new EmailSender(_mockLogger.Object, _validConfig);

        // Act
        // Note: This will fail to send because we don't have a real Azure connection,
        // but we can verify the warning is logged
        await emailSender.SendEmailAsync("test@example.com", "", "<html>Test Body</html>");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email subject is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void Constructor_WithValidConfiguration_CreatesInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() =>
            new EmailSender(_mockLogger.Object, _validConfig));
    }
}
