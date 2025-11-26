using System.Net;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.EmailSenders.MailSender;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Fin.Test.EmailSenders;

public class MailSenderClientTest
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<MailSenderClient>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public MailSenderClientTest()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<MailSenderClient>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.mailersend.com/v1/")
        };
    }

    #region SendEmailAsync

    [Fact]
    public async Task SendEmailAsync_ShouldReturnTrue_WhenEmailSentSuccessfully()
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        SetupHttpResponse(HttpStatusCode.OK);

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
        VerifyHttpRequestSent();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldIncludeFromInformation_WhenConfigured()
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req);

        // Act
        await client.SendEmailAsync(dto);

        // Assert
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("from@test.com");
        content.Should().Contain("Test Sender");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldReturnFalse_WhenHttpRequestFails()
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        SetupHttpResponse(HttpStatusCode.BadRequest);

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldReturnFalse_WhenHttpRequestThrowsException()
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowException_WhenApiKeyIsNull()
    {
        // Arrange
        SetupConfiguration(null, "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowException_WhenApiKeyIsEmpty()
    {
        // Arrange
        SetupConfiguration("", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldIncludeXRequestedWithHeader()
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req);

        // Act
        await client.SendEmailAsync(dto);

        // Assert
        capturedRequest!.Headers.Should().ContainKey("X-Requested-With");
        capturedRequest.Headers.GetValues("X-Requested-With").First().Should().Be("XMLHttpRequest");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldHandleCancellation()
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await client.SendEmailAsync(dto, cts.Token);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    public async Task SendEmailAsync_ShouldReturnTrue_ForSuccessStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        SetupHttpResponse(statusCode);

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendEmailAsync_ShouldReturnFalse_ForErrorStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        SetupConfiguration("test-api-key", "from@test.com", "Test Sender");
        var client = GetClient();
        var dto = GetValidSendEmailDto();

        SetupHttpResponse(statusCode);

        // Act
        var result = await client.SendEmailAsync(dto);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private MailSenderClient GetClient()
    {
        return new MailSenderClient(_httpClient, _configurationMock.Object, _loggerMock.Object);
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

    private void SetupConfiguration(string? apiKey, string fromEmail, string fromName)
    {
        _configurationMock
            .Setup(c => c[MailSenderConstants.MailSenderApiKeyConfigurationKey])
            .Returns(apiKey);

        _configurationMock
            .Setup(c => c[MailSenderConstants.MailSenderAddressConfigurationKey])
            .Returns(fromEmail);

        _configurationMock
            .Setup(c => c[MailSenderConstants.MailSenderNameConfigurationKey])
            .Returns(fromName);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void VerifyHttpRequestSent()
    {
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    private void VerifyLoggerError()
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}