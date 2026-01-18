using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;
using NotificationProcessor.Infrastructure.Services;

namespace NotificationProcessor.Tests.Services;

[TestFixture]
public class NotificationProcessorServiceTests
{
    private Mock<ILogger<NotificationProcessorService>> _mockLogger;
    private Mock<ITemplateEngine> _mockTemplateEngine;
    private Mock<IEmailSender> _mockEmailSender;
    private Mock<ISmsSender> _mockSmsSender;
    private Mock<INotificationRepository> _mockRepository;
    private NotificationProcessorService _processorService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<NotificationProcessorService>>();
        _mockTemplateEngine = new Mock<ITemplateEngine>();
        _mockEmailSender = new Mock<IEmailSender>();
        _mockSmsSender = new Mock<ISmsSender>();
        _mockRepository = new Mock<INotificationRepository>();

        _processorService = new NotificationProcessorService(
            _mockLogger.Object,
            _mockTemplateEngine.Object,
            _mockEmailSender.Object,
            _mockSmsSender.Object,
            _mockRepository.Object
        );
    }

    [Test]
    public async Task ProcessNotificationAsync_WithValidEmailNotification_SendsSuccessfully()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "email",
            Recipient = "test@example.com",
            RetryCount = 0,
            Payload = new Dictionary<string, object>
            {
                { "firstName", "John" },
                { "otp", "123456" },
                { "subject", "Welcome!" }
            }
        };

        var templateContent = "<html>Hello {{firstName}}, OTP: {{otp}}</html>";
        var renderedContent = "<html>Hello John, OTP: 123456</html>";

        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("forget-password", "email"))
            .ReturnsAsync(templateContent);

        _mockTemplateEngine
            .Setup(x => x.Render(templateContent, notification.Payload))
            .Returns(renderedContent);

        _mockEmailSender
            .Setup(x => x.SendEmailAsync("test@example.com", "Welcome!", renderedContent))
            .ReturnsAsync(true);

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.True);
        _mockRepository.Verify(x =>
            x.MarkAsDeliveredAsync(notification.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithValidSmsNotification_SendsSuccessfully()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "sms",
            Recipient = "+1234567890",
            RetryCount = 0,
            Payload = new Dictionary<string, object>
            {
                { "firstName", "John" },
                { "otp", "123456" }
            }
        };

        var templateContent = "Hello {{firstName}}, OTP: {{otp}}";
        var renderedContent = "Hello John, OTP: 123456";

        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("UserWelcome", "sms"))
            .ReturnsAsync(templateContent);

        _mockTemplateEngine
            .Setup(x => x.Render(templateContent, notification.Payload))
            .Returns(renderedContent);

        _mockSmsSender
            .Setup(x => x.SendSmsAsync("+1234567890", renderedContent))
            .ReturnsAsync(true);

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.True);
        _mockRepository.Verify(x =>
            x.MarkAsDeliveredAsync(notification.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithNonExistentTemplate_MarksAsFailed()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "NonExistent",
            Channel = "email",
            Recipient = "test@example.com",
            RetryCount = 0,
            Payload = new Dictionary<string, object>()
        };

        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("NonExistent", "email"))
            .ThrowsAsync(new FileNotFoundException("Template not found"));

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x =>
            x.MarkAsFailedAsync(notification.Id, 0), Times.Once);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithInvalidChannel_MarksAsFailed()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "invalid-channel",
            Recipient = "test@example.com",
            RetryCount = 0,
            Payload = new Dictionary<string, object>()
        };

        var templateContent = "Test";
        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("UserWelcome", "invalid-channel"))
            .ReturnsAsync(templateContent);

        _mockTemplateEngine
            .Setup(x => x.Render(templateContent, notification.Payload))
            .Returns(templateContent);

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x =>
            x.MarkAsFailedAsync(notification.Id, 0), Times.Once);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithMaxRetriesExceeded_MarksAsFailed()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "email",
            Recipient = "test@example.com",
            RetryCount = 5,
            Payload = new Dictionary<string, object>()
        };

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x =>
            x.MarkAsFailedAsync(notification.Id, 5), Times.Once);
        _mockTemplateEngine.Verify(x =>
            x.LoadTemplateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ProcessNotificationAsync_WhenSendFails_UpdatesRetryCount()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "email",
            Recipient = "test@example.com",
            RetryCount = 0,
            Payload = new Dictionary<string, object>
            {
                { "subject", "Test" }
            }
        };

        var templateContent = "Test";
        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("UserWelcome", "email"))
            .ReturnsAsync(templateContent);

        _mockTemplateEngine
            .Setup(x => x.Render(templateContent, notification.Payload))
            .Returns(templateContent);

        _mockEmailSender
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x =>
            x.UpdateRetryCountAsync(notification.Id, 1), Times.Once);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithNullNotification_ReturnsFalse()
    {
        // Act
        var result = await _processorService.ProcessNotificationAsync(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithEmailWithoutSubject_UsesDefaultSubject()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "email",
            Recipient = "test@example.com",
            RetryCount = 0,
            Payload = new Dictionary<string, object>
            {
                { "firstName", "John" }
            }
        };

        var templateContent = "Hello {{firstName}}";
        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("UserWelcome", "email"))
            .ReturnsAsync(templateContent);

        _mockTemplateEngine
            .Setup(x => x.Render(templateContent, notification.Payload))
            .Returns("Hello John");

        _mockEmailSender
            .Setup(x => x.SendEmailAsync("test@example.com", "Notification", "Hello John"))
            .ReturnsAsync(true);

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.True);
        _mockEmailSender.Verify(x =>
            x.SendEmailAsync("test@example.com", "Notification", "Hello John"), Times.Once);
    }

    [Test]
    public async Task ProcessNotificationAsync_WithInAppChannel_ReturnsFailure()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Template = "UserWelcome",
            Channel = "inapp",
            Recipient = "user123",
            RetryCount = 0,
            Payload = new Dictionary<string, object>()
        };

        var templateContent = "Test";
        _mockTemplateEngine
            .Setup(x => x.LoadTemplateAsync("UserWelcome", "inapp"))
            .ReturnsAsync(templateContent);

        _mockTemplateEngine
            .Setup(x => x.Render(templateContent, notification.Payload))
            .Returns(templateContent);

        // Act
        var result = await _processorService.ProcessNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x =>
            x.UpdateRetryCountAsync(notification.Id, 1), Times.Once);
    }
}
