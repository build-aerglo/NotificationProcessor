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
    public void LoadTemplateAsync_WithNonExistentChannel_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _templateEngine.LoadTemplateAsync("Test", "invalid-channel"));
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
}
