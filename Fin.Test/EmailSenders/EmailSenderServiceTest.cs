using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.EmailSenders.Constants;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.EmailSenders.MailSender;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.EmailSenders;

public class EmailSenderServiceTest
{
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IMailSenderClient> _mailSenderClientMock = new();
    private readonly Mock<IConfigurationSection> _mailServiceSectionMock = new();

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
            Subject = "Assunto com Ã§Ã£Ã£o",
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

    #endregion

    #region Helper Methods

    private EmailSenderService GetService()
    {
        return new EmailSenderService(_configurationMock.Object, _mailSenderClientMock.Object);
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