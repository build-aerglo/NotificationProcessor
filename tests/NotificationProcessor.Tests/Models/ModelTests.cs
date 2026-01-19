using NotificationProcessor.Core.Models;
using NUnit.Framework;

namespace NotificationProcessor.Tests.Models;

[TestFixture]
public class ModelTests
{
    [Test]
    public void AzureEmailConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new AzureEmailConfiguration();

        // Assert
        Assert.That(config.ConnectionString, Is.EqualTo(string.Empty));
        Assert.That(config.SenderAddress, Is.EqualTo(string.Empty));
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
    public void AzureEmailConfiguration_CanSetProperties()
    {
        // Arrange & Act
        var config = new AzureEmailConfiguration
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey123==",
            SenderAddress = "DoNotReply@test.com"
        };

        // Assert
        Assert.That(config.ConnectionString, Is.EqualTo("endpoint=https://test.communication.azure.com/;accesskey=testkey123=="));
        Assert.That(config.SenderAddress, Is.EqualTo("DoNotReply@test.com"));
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
