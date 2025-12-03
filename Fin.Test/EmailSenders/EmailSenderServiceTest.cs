using Fin.Application.Emails;
using Fin.Infrastructure.EmailSenders.Constants;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.EmailSenders.MailKit;
using Fin.Infrastructure.EmailSenders.MailSender;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.EmailSenders;

public class EmailSenderServiceTest
{
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IMailSenderClient> _mailSenderClientMock = new();
    private readonly Mock<IMailKitClient> _mailKitClientMock = new();
    private readonly Mock<IConfigurationSection> _mailServiceSectionMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();

    #region SendEmailAsync - MailSender

    [Fact]
    public async Task SendEmailAsync_ShouldUseMailSender_WhenConfiguredAsMailSender()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
        _mailSenderClientMock.Verify(m => m.SendEmailAsync(dto, default), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldReturnTrue_WhenMailSenderSucceeds()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldReturnFalse_WhenMailSenderFails()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPassCancellationToken_WhenUsingMailSender()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();
        var cts = new CancellationTokenSource();

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, cts.Token))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto, cts.Token);

        // Assert
        _mailSenderClientMock.Verify(m => m.SendEmailAsync(dto, cts.Token), Times.Once);
    }

    #endregion
    
    #region SendEmailAsync - Different Email Types

    [Fact]
    public async Task SendEmailAsync_ShouldHandle_SimpleEmail()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            Subject = "Simple Subject",
            PlainBody = "Simple plain text",
            HtmlBody = "<p>Simple HTML</p>"
        };

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldHandle_EmailWithLongContent()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            Subject = "Long Email Subject",
            PlainBody = new string('A', 5000),
            HtmlBody = $"<p>{new string('B', 5000)}</p>"
        };

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldHandle_EmailWithSpecialCharacters()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            Subject = "Special <>&\" Characters",
            PlainBody = "Body with special chars: <>&\"'",
            HtmlBody = "<p>HTML with special: &lt;&gt;&amp;&quot;</p>"
        };

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldHandle_EmailWithUnicodeCharacters()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Usuário Tëst",
            Subject = "Assunto com ção",
            PlainBody = "Corpo com ãéíóú",
            HtmlBody = "<p>HTML com émojis 😀🎉</p>"
        };

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        var result = await service.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region SendEmailAsync - Template Integration

    [Fact]
    public async Task SendEmailAsync_ShouldNotUseTemplate_WhenBaseTemplatesNameIsNull()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();
        dto.BaseTemplatesName = null;

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        _emailTemplateServiceMock.Verify(
            e => e.Get(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldNotUseTemplate_WhenBaseTemplatesNameIsEmpty()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();
        dto.BaseTemplatesName = "";

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(dto, default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        _emailTemplateServiceMock.Verify(
            e => e.Get(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPopulateHtmlBody_WhenUsingTemplate()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            TemplateProperties = new Dictionary<string, string>
            {
                { "userName", "John" }
            }
        };

        var expectedHtml = "<html><body>Welcome, John!</body></html>";
        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeHTML", dto.TemplateProperties))
            .Returns(expectedHtml);

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.HtmlBody.Should().Be(expectedHtml);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeHTML", dto.TemplateProperties),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPopulatePlainBody_WhenUsingTemplate()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            TemplateProperties = new Dictionary<string, string>
            {
                { "userName", "John" }
            }
        };

        var expectedPlain = "Welcome, John!";
        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomePlain", dto.TemplateProperties))
            .Returns(expectedPlain);

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.PlainBody.Should().Be(expectedPlain);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomePlain", dto.TemplateProperties),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPopulateSubject_WhenUsingTemplate()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            TemplateProperties = new Dictionary<string, string>
            {
                { "userName", "John" }
            }
        };

        var expectedSubject = "Welcome to Our Service, John!";
        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeSubject", dto.TemplateProperties))
            .Returns(expectedSubject);

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.Subject.Should().Be(expectedSubject);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeSubject", dto.TemplateProperties),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPopulateAllFields_WhenUsingTemplate()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            TemplateProperties = new Dictionary<string, string>
            {
                { "userName", "John" },
                { "companyName", "Fin" }
            }
        };

        var expectedHtml = "<html><body>Welcome, John from Fin!</body></html>";
        var expectedPlain = "Welcome, John from Fin!";
        var expectedSubject = "Welcome to Fin, John!";

        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeHTML", dto.TemplateProperties))
            .Returns(expectedHtml);
        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomePlain", dto.TemplateProperties))
            .Returns(expectedPlain);
        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeSubject", dto.TemplateProperties))
            .Returns(expectedSubject);

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.HtmlBody.Should().Be(expectedHtml);
        dto.PlainBody.Should().Be(expectedPlain);
        dto.Subject.Should().Be(expectedSubject);
        
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeHTML", dto.TemplateProperties),
            Times.Once
        );
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomePlain", dto.TemplateProperties),
            Times.Once
        );
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeSubject", dto.TemplateProperties),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldNotOverrideHtmlBody_WhenAlreadyProvided()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var existingHtml = "<html><body>Existing HTML</body></html>";
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            HtmlBody = existingHtml,
            TemplateProperties = new Dictionary<string, string>()
        };

        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeHTML", dto.TemplateProperties))
            .Returns("<html><body>Template HTML</body></html>");

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.HtmlBody.Should().Be(existingHtml);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeHTML", It.IsAny<Dictionary<string, string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldNotOverridePlainBody_WhenAlreadyProvided()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var existingPlain = "Existing Plain Text";
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            PlainBody = existingPlain,
            TemplateProperties = new Dictionary<string, string>()
        };

        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomePlain", dto.TemplateProperties))
            .Returns("Template Plain Text");

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.PlainBody.Should().Be(existingPlain);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomePlain", It.IsAny<Dictionary<string, string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldNotOverrideSubject_WhenAlreadyProvided()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var existingSubject = "Existing Subject";
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            Subject = existingSubject,
            TemplateProperties = new Dictionary<string, string>()
        };

        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeSubject", dto.TemplateProperties))
            .Returns("Template Subject");

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.Subject.Should().Be(existingSubject);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeSubject", It.IsAny<Dictionary<string, string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldInitializeTemplateProperties_WhenNull()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            TemplateProperties = null
        };

        _emailTemplateServiceMock
            .Setup(e => e.Get(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns("Template Content");

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.TemplateProperties.Should().NotBeNull();
        dto.TemplateProperties.Should().BeEmpty();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldUseEmptyDictionary_WhenTemplatePropertiesNotProvided()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome"
        };

        var expectedHtml = "<html><body>Welcome!</body></html>";
        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeHTML", It.IsAny<Dictionary<string, string>>()))
            .Returns(expectedHtml);

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), default))
            .ReturnsAsync(true);

        // Act
        await service.SendEmailAsync(dto);

        // Assert
        dto.HtmlBody.Should().Be(expectedHtml);
        _emailTemplateServiceMock.Verify(
            e => e.Get("WelcomeHTML", It.Is<Dictionary<string, string>>(d => d != null && d.Count == 0)),
            Times.Once
        );
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task SendEmailAsync_ShouldPropagateException_WhenMailSenderThrows()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = GetValidSendEmailDto();

        _mailSenderClientMock
            .Setup(m => m.SendEmailAsync(It.IsAny<SendEmailDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        Func<Task> act = async () => await service.SendEmailAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPropagateException_WhenTemplateServiceThrows()
    {
        // Arrange
        SetupMailService(MailServicesConst.MailSender);
        var service = GetService();
        var dto = new SendEmailDto
        {
            ToEmail = "user@test.com",
            ToName = "Test User",
            BaseTemplatesName = "Welcome",
            TemplateProperties = new Dictionary<string, string>()
        };

        _emailTemplateServiceMock
            .Setup(e => e.Get("WelcomeHTML", It.IsAny<Dictionary<string, string>>()))
            .Throws(new InvalidOperationException("Template not found"));

        // Act
        Func<Task> act = async () => await service.SendEmailAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Template not found");
    }

    #endregion

    #region Helper Methods

    private EmailSenderService GetService()
    {
        return new EmailSenderService(_configurationMock.Object, _mailSenderClientMock.Object, _mailKitClientMock.Object, _emailTemplateServiceMock.Object);
    }

    private SendEmailDto GetValidSendEmailDto()
    {
        return new SendEmailDto
        {
            ToEmail = "recipient@test.com",
            ToName = "Recipient Name",
            Subject = "Test Email",
            PlainBody = "This is a test email",
            HtmlBody = "<p>This is a test email</p>"
        };
    }

    private void SetupMailService(string mailService)
    {
        _mailServiceSectionMock.Setup(s => s.Value).Returns(mailService);
        _configurationMock
            .Setup(c => c.GetSection(MailServicesConst.MailServiceConfigurationKey))
            .Returns(_mailServiceSectionMock.Object);
    }

    #endregion
}