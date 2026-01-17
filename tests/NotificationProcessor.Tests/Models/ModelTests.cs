using NotificationProcessor.Core.Models;
using NUnit.Framework;

namespace NotificationProcessor.Tests.Models;

[TestFixture]
public class ModelTests
{
    [Test]
    public void SmtpConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new SmtpConfiguration();

        // Assert
        Assert.That(config.Host, Is.EqualTo(string.Empty));
        Assert.That(config.Port, Is.EqualTo(0));
        Assert.That(config.Username, Is.EqualTo(string.Empty));
        Assert.That(config.Password, Is.EqualTo(string.Empty));
        Assert.That(config.FromEmail, Is.EqualTo(string.Empty));
        Assert.That(config.FromName, Is.EqualTo(string.Empty));
        Assert.That(config.EnableSsl, Is.True);
    }

    [Test]
    public void TwilioConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new TwilioConfiguration();

        // Assert
        Assert.That(config.AccountSid, Is.EqualTo(string.Empty));
        Assert.That(config.AuthToken, Is.EqualTo(string.Empty));
        Assert.That(config.FromPhoneNumber, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NotificationConfigRequest_DefaultValues_AreSet()
    {
        // Arrange & Act
        var request = new NotificationConfigRequest();

        // Assert
        Assert.That(request.NotificationType, Is.EqualTo(string.Empty));
        Assert.That(request.RequestId, Is.Not.Null);
        Assert.That(request.RequestId, Is.Not.Empty);
        Assert.That(request.RequestedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void NotificationConfigResponse_DefaultValues_AreSet()
    {
        // Arrange & Act
        var response = new NotificationConfigResponse();

        // Assert
        Assert.That(response.RequestId, Is.EqualTo(string.Empty));
        Assert.That(response.NotificationType, Is.EqualTo(string.Empty));
        Assert.That(response.Success, Is.False);
        Assert.That(response.Timestamp, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void SmtpConfiguration_CanSetProperties()
    {
        // Arrange & Act
        var config = new SmtpConfiguration
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "user@gmail.com",
            Password = "password123",
            FromEmail = "noreply@example.com",
            FromName = "Test System",
            EnableSsl = true
        };

        // Assert
        Assert.That(config.Host, Is.EqualTo("smtp.gmail.com"));
        Assert.That(config.Port, Is.EqualTo(587));
        Assert.That(config.Username, Is.EqualTo("user@gmail.com"));
        Assert.That(config.Password, Is.EqualTo("password123"));
        Assert.That(config.FromEmail, Is.EqualTo("noreply@example.com"));
        Assert.That(config.FromName, Is.EqualTo("Test System"));
        Assert.That(config.EnableSsl, Is.True);
    }

    [Test]
    public void TwilioConfiguration_CanSetProperties()
    {
        // Arrange & Act
        var config = new TwilioConfiguration
        {
            AccountSid = "AC1234567890",
            AuthToken = "token123",
            FromPhoneNumber = "+19876543210"
        };

        // Assert
        Assert.That(config.AccountSid, Is.EqualTo("AC1234567890"));
        Assert.That(config.AuthToken, Is.EqualTo("token123"));
        Assert.That(config.FromPhoneNumber, Is.EqualTo("+19876543210"));
    }
}
