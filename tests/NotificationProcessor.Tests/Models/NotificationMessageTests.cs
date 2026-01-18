using System.Text.Json;
using NUnit.Framework;
using NotificationProcessor.Core.Models;

namespace NotificationProcessor.Tests.Models;

[TestFixture]
public class NotificationMessageTests
{
    [Test]
    public void NotificationMessage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var notification = new NotificationMessage();

        // Assert
        Assert.That(notification.Id, Is.EqualTo(string.Empty));
        Assert.That(notification.Template, Is.EqualTo(string.Empty));
        Assert.That(notification.Channel, Is.EqualTo(string.Empty));
        Assert.That(notification.Recipient, Is.EqualTo(string.Empty));
        Assert.That(notification.RetryCount, Is.EqualTo(0));
        Assert.That(notification.Payload, Is.Not.Null);
        Assert.That(notification.Payload.Count, Is.EqualTo(0));
    }

    [Test]
    public void NotificationMessage_Deserialization_WorksCorrectly()
    {
        // Arrange
        var json = @"{
            ""id"": ""123e4567-e89b-12d3-a456-426614174000"",
            ""template"": ""UserWelcome"",
            ""channel"": ""email"",
            ""retryCount"": 0,
            ""recipient"": ""test@example.com"",
            ""payload"": {
                ""firstName"": ""John"",
                ""otp"": ""456789""
            },
            ""requestedAt"": ""2026-01-14T12:00:00Z""
        }";

        // Act
        var notification = JsonSerializer.Deserialize<NotificationMessage>(json);

        // Assert
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification!.Id, Is.EqualTo("123e4567-e89b-12d3-a456-426614174000"));
        Assert.That(notification.Template, Is.EqualTo("UserWelcome"));
        Assert.That(notification.Channel, Is.EqualTo("email"));
        Assert.That(notification.RetryCount, Is.EqualTo(0));
        Assert.That(notification.Recipient, Is.EqualTo("test@example.com"));
        Assert.That(notification.Payload.ContainsKey("firstName"), Is.True);
        Assert.That(notification.Payload["firstName"].ToString(), Is.EqualTo("John"));
    }

    [Test]
    public void NotificationMessage_Serialization_WorksCorrectly()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            Id = "test-id",
            Template = "UserWelcome",
            Channel = "email",
            RetryCount = 1,
            Recipient = "test@example.com",
            Payload = new Dictionary<string, object>
            {
                { "firstName", "Jane" },
                { "otp", "123456" }
            },
            RequestedAt = DateTime.Parse("2026-01-14T12:00:00Z").ToUniversalTime()
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationMessage>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Id, Is.EqualTo(notification.Id));
        Assert.That(deserialized.Template, Is.EqualTo(notification.Template));
        Assert.That(deserialized.Channel, Is.EqualTo(notification.Channel));
        Assert.That(deserialized.RetryCount, Is.EqualTo(notification.RetryCount));
        Assert.That(deserialized.Recipient, Is.EqualTo(notification.Recipient));
    }

    [Test]
    public void NotificationMessage_WithSmsChannel_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""id"": ""sms-test-id"",
            ""template"": ""UserWelcome"",
            ""channel"": ""sms"",
            ""retryCount"": 0,
            ""recipient"": ""+1234567890"",
            ""payload"": {
                ""firstName"": ""Bob"",
                ""otp"": ""987654""
            },
            ""requestedAt"": ""2026-01-14T12:00:00Z""
        }";

        // Act
        var notification = JsonSerializer.Deserialize<NotificationMessage>(json);

        // Assert
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification!.Channel, Is.EqualTo("sms"));
        Assert.That(notification.Recipient, Is.EqualTo("+1234567890"));
    }

    [Test]
    public void NotificationChannel_Constants_HaveCorrectValues()
    {
        // Assert
        Assert.That(NotificationChannel.Email, Is.EqualTo("email"));
        Assert.That(NotificationChannel.Sms, Is.EqualTo("sms"));
        Assert.That(NotificationChannel.InApp, Is.EqualTo("inapp"));
    }

    [Test]
    public void NotificationStatus_Constants_HaveCorrectValues()
    {
        // Assert
        Assert.That(NotificationStatus.Pending, Is.EqualTo("pending"));
        Assert.That(NotificationStatus.Sent, Is.EqualTo("sent"));
        Assert.That(NotificationStatus.Delivered, Is.EqualTo("delivered"));
        Assert.That(NotificationStatus.Failed, Is.EqualTo("failed"));
    }
}
