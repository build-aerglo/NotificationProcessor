using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationProcessor.Infrastructure.Services;
using NUnit.Framework;

namespace NotificationProcessor.Tests.Services;

[TestFixture]
public class NotificationConfigServiceTests
{
    private Mock<ILogger<NotificationConfigService>> _loggerMock = null!;
    private IConfiguration _configuration = null!;
    private NotificationConfigService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<NotificationConfigService>>();

        // Build configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.test.com",
            ["Smtp:Port"] = "587",
            ["Smtp:Username"] = "testuser",
            ["Smtp:Password"] = "testpass",
            ["Smtp:FromEmail"] = "test@test.com",
            ["Smtp:FromName"] = "Test Sender",
            ["Smtp:EnableSsl"] = "true",
            ["Twilio:AccountSid"] = "AC123456789",
            ["Twilio:AuthToken"] = "auth_token_123",
            ["Twilio:FromPhoneNumber"] = "+1234567890"
        });
        _configuration = configBuilder.Build();

        _service = new NotificationConfigService(_configuration, _loggerMock.Object);
    }

    [Test]
    public void GetSmtpConfiguration_ReturnsValidConfiguration()
    {
        // Act
        var result = _service.GetSmtpConfiguration();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Host, Is.EqualTo("smtp.test.com"));
        Assert.That(result.Port, Is.EqualTo(587));
        Assert.That(result.Username, Is.EqualTo("testuser"));
        Assert.That(result.Password, Is.EqualTo("testpass"));
        Assert.That(result.FromEmail, Is.EqualTo("test@test.com"));
        Assert.That(result.FromName, Is.EqualTo("Test Sender"));
        Assert.That(result.EnableSsl, Is.True);
    }

    [Test]
    public void GetTwilioConfiguration_ReturnsValidConfiguration()
    {
        // Act
        var result = _service.GetTwilioConfiguration();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccountSid, Is.EqualTo("AC123456789"));
        Assert.That(result.AuthToken, Is.EqualTo("auth_token_123"));
        Assert.That(result.FromPhoneNumber, Is.EqualTo("+1234567890"));
    }

    [Test]
    public void GetConfiguration_WithEmailType_ReturnsSmtpConfig()
    {
        // Act
        var result = _service.GetConfiguration("email");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.NotificationType, Is.EqualTo("email"));
        Assert.That(result.Configuration, Is.Not.Null);
    }

    [Test]
    public void GetConfiguration_WithSmsType_ReturnsTwilioConfig()
    {
        // Act
        var result = _service.GetConfiguration("sms");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.NotificationType, Is.EqualTo("sms"));
        Assert.That(result.Configuration, Is.Not.Null);
    }

    [Test]
    public void GetConfiguration_WithInvalidType_ReturnsError()
    {
        // Act
        var result = _service.GetConfiguration("invalid");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Null);
        Assert.That(result.ErrorMessage, Does.Contain("Unknown notification type"));
    }

    [Test]
    [TestCase("email")]
    [TestCase("smtp")]
    public void GetConfiguration_WithEmailVariations_ReturnsSmtpConfig(string notificationType)
    {
        // Act
        var result = _service.GetConfiguration(notificationType);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Configuration, Is.Not.Null);
    }

    [Test]
    [TestCase("sms")]
    [TestCase("twilio")]
    public void GetConfiguration_WithSmsVariations_ReturnsTwilioConfig(string notificationType)
    {
        // Act
        var result = _service.GetConfiguration(notificationType);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Configuration, Is.Not.Null);
    }
}
