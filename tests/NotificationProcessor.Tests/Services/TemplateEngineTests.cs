using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NotificationProcessor.Infrastructure.Services;

namespace NotificationProcessor.Tests.Services;

[TestFixture]
public class TemplateEngineTests
{
    private Mock<ILogger<TemplateEngine>> _mockLogger;
    private string _testTemplatePath;
    private TemplateEngine _templateEngine;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TemplateEngine>>();
        _testTemplatePath = Path.Combine(Path.GetTempPath(), "TestTemplates");

        // Create test template directories
        Directory.CreateDirectory(Path.Combine(_testTemplatePath, "email"));
        Directory.CreateDirectory(Path.Combine(_testTemplatePath, "sms"));

        _templateEngine = new TemplateEngine(_mockLogger.Object, _testTemplatePath);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test template directory
        if (Directory.Exists(_testTemplatePath))
        {
            Directory.Delete(_testTemplatePath, true);
        }
    }

    [Test]
    public void Render_WithValidPlaceholders_ReplacesCorrectly()
    {
        // Arrange
        var template = "Hello {{firstName}}, your code is {{otp}}";
        var data = new Dictionary<string, object>
        {
            { "firstName", "John" },
            { "otp", "123456" }
        };

        // Act
        var result = _templateEngine.Render(template, data);

        // Assert
        Assert.That(result, Is.EqualTo("Hello John, your code is 123456"));
    }

    [Test]
    public void Render_WithMissingPlaceholder_KeepsPlaceholder()
    {
        // Arrange
        var template = "Hello {{firstName}}, your code is {{otp}}";
        var data = new Dictionary<string, object>
        {
            { "firstName", "John" }
        };

        // Act
        var result = _templateEngine.Render(template, data);

        // Assert
        Assert.That(result, Is.EqualTo("Hello John, your code is {{otp}}"));
    }

    [Test]
    public void Render_WithEmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var template = "";
        var data = new Dictionary<string, object>();

        // Act
        var result = _templateEngine.Render(template, data);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Render_WithMultipleSamePlaceholders_ReplacesAll()
    {
        // Arrange
        var template = "{{name}} {{name}} {{name}}";
        var data = new Dictionary<string, object>
        {
            { "name", "Test" }
        };

        // Act
        var result = _templateEngine.Render(template, data);

        // Assert
        Assert.That(result, Is.EqualTo("Test Test Test"));
    }

    [Test]
    public async Task LoadTemplateAsync_WithValidHtmlTemplate_LoadsSuccessfully()
    {
        // Arrange
        var templateContent = "<html><body>Hello {{name}}</body></html>";
        var templatePath = Path.Combine(_testTemplatePath, "email", "Test.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _templateEngine.LoadTemplateAsync("Test", "email");

        // Assert
        Assert.That(result, Is.EqualTo(templateContent));
    }

    [Test]
    public async Task LoadTemplateAsync_WithValidTxtTemplate_LoadsSuccessfully()
    {
        // Arrange
        var templateContent = "Hello {{name}}";
        var templatePath = Path.Combine(_testTemplatePath, "sms", "Test.txt");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _templateEngine.LoadTemplateAsync("Test", "sms");

        // Assert
        Assert.That(result, Is.EqualTo(templateContent));
    }

    [Test]
    public void LoadTemplateAsync_WithNonExistentTemplate_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _templateEngine.LoadTemplateAsync("NonExistent", "email"));
    }

    [Test]
    public void LoadTemplateAsync_WithNonExistentChannel_ThrowsArgumentException()
    {
        // "invalid-channel" is not a supported channel, so it should throw ArgumentException
        // (input validation happens before directory existence check)

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _templateEngine.LoadTemplateAsync("Test", "invalid-channel"));
    }

    [Test]
    public void LoadTemplateAsync_WithSupportedChannelButMissingDirectory_ThrowsDirectoryNotFoundException()
    {
        // "inapp" is a supported channel, but if its directory doesn't exist,
        // it should throw DirectoryNotFoundException (after passing validation)

        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _templateEngine.LoadTemplateAsync("Test", "inapp"));
    }

    [Test]
    public void LoadTemplateAsync_WithEmptyTemplateName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _templateEngine.LoadTemplateAsync("", "email"));
    }

    [Test]
    public void LoadTemplateAsync_WithEmptyChannel_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _templateEngine.LoadTemplateAsync("Test", ""));
    }

    [Test]
    public async Task LoadTemplateAsync_EmailChannel_LoadsOnlyHtmlFiles()
    {
        // Arrange
        var templateContent = "<!DOCTYPE html><html><body>Email template</body></html>";
        var templatePath = Path.Combine(_testTemplatePath, "email", "forget-password.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _templateEngine.LoadTemplateAsync("forget-password", "email");

        // Assert
        Assert.That(result, Is.EqualTo(templateContent));
    }

    [Test]
    public async Task LoadTemplateAsync_SmsChannel_LoadsOnlyTxtFiles()
    {
        // Arrange
        var templateContent = "SMS template content";
        var templatePath = Path.Combine(_testTemplatePath, "sms", "forget-password.txt");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _templateEngine.LoadTemplateAsync("forget-password", "sms");

        // Assert
        Assert.That(result, Is.EqualTo(templateContent));
    }

    [Test]
    public async Task LoadTemplateAsync_EmailChannel_ThrowsWhenOnlyTxtExists()
    {
        // Arrange - Create only .txt file for email channel
        var templatePath = Path.Combine(_testTemplatePath, "email", "test-template.txt");
        await File.WriteAllTextAsync(templatePath, "Text content");

        // Act & Assert - Should fail because email requires .html
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _templateEngine.LoadTemplateAsync("test-template", "email"));
    }

    [Test]
    public async Task LoadTemplateAsync_SmsChannel_ThrowsWhenOnlyHtmlExists()
    {
        // Arrange - Create only .html file for sms channel
        var templatePath = Path.Combine(_testTemplatePath, "sms", "test-template.html");
        await File.WriteAllTextAsync(templatePath, "<html>HTML content</html>");

        // Act & Assert - Should fail because sms requires .txt
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _templateEngine.LoadTemplateAsync("test-template", "sms"));
    }

    [Test]
    public void LoadTemplateAsync_UnsupportedChannel_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _templateEngine.LoadTemplateAsync("test", "telegram"));
    }

    [Test]
    public async Task LoadTemplateAsync_KebabCaseTemplateName_LoadsSuccessfully()
    {
        // Arrange
        var emailContent = "<!DOCTYPE html><html><body>Forget password email</body></html>";
        var smsContent = "Forget password SMS";

        var emailPath = Path.Combine(_testTemplatePath, "email", "forget-password.html");
        var smsPath = Path.Combine(_testTemplatePath, "sms", "forget-password.txt");

        await File.WriteAllTextAsync(emailPath, emailContent);
        await File.WriteAllTextAsync(smsPath, smsContent);

        // Act
        var emailResult = await _templateEngine.LoadTemplateAsync("forget-password", "email");
        var smsResult = await _templateEngine.LoadTemplateAsync("forget-password", "sms");

        // Assert
        Assert.That(emailResult, Is.EqualTo(emailContent));
        Assert.That(smsResult, Is.EqualTo(smsContent));
    }

    [Test]
    public async Task LoadTemplateAsync_InAppChannel_LoadsTxtFiles()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testTemplatePath, "inapp"));
        var templateContent = "In-app notification content";
        var templatePath = Path.Combine(_testTemplatePath, "inapp", "notification.txt");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _templateEngine.LoadTemplateAsync("notification", "inapp");

        // Assert
        Assert.That(result, Is.EqualTo(templateContent));
    }

    [Test]
    public async Task LoadTemplateAsync_CaseInsensitiveChannel_LoadsSuccessfully()
    {
        // Arrange
        var templateContent = "Email content";
        var templatePath = Path.Combine(_testTemplatePath, "email", "test.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act - Test with different case variations
        var result1 = await _templateEngine.LoadTemplateAsync("test", "EMAIL");
        var result2 = await _templateEngine.LoadTemplateAsync("test", "Email");
        var result3 = await _templateEngine.LoadTemplateAsync("test", "email");

        // Assert
        Assert.That(result1, Is.EqualTo(templateContent));
        Assert.That(result2, Is.EqualTo(templateContent));
        Assert.That(result3, Is.EqualTo(templateContent));
    }

    [Test]
    public async Task IntegrationTest_ForgetPasswordEmail_LoadsAndRendersCorrectly()
    {
        // Arrange
        var templateContent = "Hi {{firstName}}, your password reset code is {{otp}}.";
        var templatePath = Path.Combine(_testTemplatePath, "email", "forget-password.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var data = new Dictionary<string, object>
        {
            { "firstName", "John" },
            { "otp", "456789" }
        };

        // Act
        var loadedTemplate = await _templateEngine.LoadTemplateAsync("forget-password", "email");
        var renderedTemplate = _templateEngine.Render(loadedTemplate, data);

        // Assert
        Assert.That(renderedTemplate, Is.EqualTo("Hi John, your password reset code is 456789."));
    }

    [Test]
    public async Task IntegrationTest_ForgetPasswordSms_LoadsAndRendersCorrectly()
    {
        // Arrange
        var templateContent = "Hi {{firstName}}, your password reset code is {{otp}}. It expires in 10 minutes.";
        var templatePath = Path.Combine(_testTemplatePath, "sms", "forget-password.txt");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var data = new Dictionary<string, object>
        {
            { "firstName", "Jane" },
            { "otp", "789012" }
        };

        // Act
        var loadedTemplate = await _templateEngine.LoadTemplateAsync("forget-password", "sms");
        var renderedTemplate = _templateEngine.Render(loadedTemplate, data);

        // Assert
        Assert.That(renderedTemplate, Is.EqualTo("Hi Jane, your password reset code is 789012. It expires in 10 minutes."));
    }
}
