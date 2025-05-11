using Fin.Domain.Global;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Authentications.Consts;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Enums;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.Redis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.Authentications;

public class AuthenticationTokenServiceTest : TestUtils.BaseTestWithContext
{
    #region Login

    [Fact(DisplayName = "Login with success")]
    public async Task Login_Success()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var email = TestUtils.Strings.First();
        var pass = TestUtils.Strings.Last();

        var encryptedEmail = resources.CryptoHelper.Encrypt(email);
        var encryptedPass = resources.CryptoHelper.Encrypt(pass);

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtKeyConfigKey).Value)
            .Returns("super-ultra-mega-power-nano-mili-cent-supe-secret-batman-key");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenExpireInMinutesConfigKey).Value)
            .Returns("120");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value)
            .Returns(TestUtils.Strings[3]);
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value)
            .Returns(TestUtils.Strings[4]);

        DateTimeProvider.Setup(d => d.UtcNow())
            .Returns(DateTime.UtcNow);

        var now = DateTimeProvider.Object.UtcNow();

        var user = new User(new UserUpdateOrCreateInput
        {
            DisplayName = TestUtils.Strings[1],
        }, now)
        {
            Id = TestUtils.Guids[0],
            Tenants = [new Tenant(now)]
        };
        var credential = new UserCredential(user.Id, encryptedEmail, encryptedPass);
        credential.Id = TestUtils.Guids[1];

        await resources.UseRepository.AddAsync(user);
        await resources.CredentialRepository.AddAsync(credential);
        await Context.SaveChangesAsync();

        // Act
        var result = await service.Login(new LoginInput
        {
            Email = email,
            Password = pass
        });

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Theory(DisplayName = "Login with fail")]
    [InlineData(LoginErrorCode.EmailNotFound, "noexist@mail.com", "exists", 0, true)]
    [InlineData(LoginErrorCode.MaxAttemptsReached, "exists@mail.com", "exists", 5, true)]
    [InlineData(LoginErrorCode.InactivatedUser, "exists@mail.com", "exists", 0, false)]
    [InlineData(LoginErrorCode.InvalidPassword, "exists@mail.com", "error", 0, true)]
    [InlineData(LoginErrorCode.DoNotHasPassword, "exists@mail.com", null, 0, true)]
    public async Task Login_Fail(LoginErrorCode code, string credentialEmail, string credentialPass,
        int previusesAttempts, bool activatedUser)
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var encryptedEmail = resources.CryptoHelper.Encrypt(credentialEmail);
        var encryptedPass =
            string.IsNullOrEmpty(credentialPass) ? null : resources.CryptoHelper.Encrypt(credentialPass);

        DateTimeProvider.Setup(d => d.UtcNow())
            .Returns(DateTime.UtcNow);

        var now = DateTimeProvider.Object.UtcNow();

        var user = new User(new UserUpdateOrCreateInput
        {
            DisplayName = TestUtils.Strings[1],
        }, now)
        {
            Id = TestUtils.Guids[0],
            Tenants = [new Tenant(now)]
        };
        var credential = new UserCredential(user.Id, encryptedEmail, encryptedPass);
        credential.Id = TestUtils.Guids[1];
        credential.User = user;

        if (!activatedUser)
            user.ToggleActivity();
        ;

        for (var i = 0; i < previusesAttempts; i++)
        {
            credential.TestCredentials(encryptedEmail, "");
        }

        await resources.UseRepository.AddAsync(user);
        await resources.CredentialRepository.AddAsync(credential);
        await Context.SaveChangesAsync();

        // Act
        var result = await service.Login(new LoginInput
        {
            Email = "exists@mail.com",
            Password = "exists"
        });

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        result.Token.Should().BeNull();
        result.RefreshToken.Should().BeNull();

        var credentialFromDb = await resources.CredentialRepository.Query().FirstOrDefaultAsync();
        if (code == LoginErrorCode.InvalidPassword)
            credentialFromDb.FailLoginAttempts.Should().Be(previusesAttempts + 1);
    }

    #endregion

    #region RefreshToken

    [Fact(DisplayName = "Refresh token with success")]
    public async Task RefreshToken_Success()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var refreshToken = TestUtils.Strings.First();
        var userId = TestUtils.Guids[0];

        resources.FakeRedis
            .Setup(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"))
            .ReturnsAsync(userId);

        var now = DateTime.UtcNow;
        DateTimeProvider.Setup(d => d.UtcNow())
            .Returns(now);

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtKeyConfigKey).Value)
            .Returns("super-ultra-mega-power-nano-mili-cent-supe-secret-batman-key");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenExpireInMinutesConfigKey).Value)
            .Returns("120");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value)
            .Returns(TestUtils.Strings[3]);
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value)
            .Returns(TestUtils.Strings[4]);

        var user = new User(new UserUpdateOrCreateInput
        {
            DisplayName = TestUtils.Strings[1],
        }, now)
        {
            Id = userId,
            Tenants = new List<Tenant>
            {
                new(now)
            }
        };
        await resources.UseRepository.AddAsync(user, true);

        // Act
        var result = await service.RefreshToken(refreshToken);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        resources.FakeRedis
            .Verify(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"), Times.Once);
        resources.FakeRedis
            .Verify(r => r.RemoveAsync($"refresh-token-{refreshToken}"), Times.Once);
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);

        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value, Times.Once);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value, Times.Once);
    }

    [Fact(DisplayName = "Refresh token with fail, inactivated user")]
    public async Task RefreshToken_InactivatedUser()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var refreshToken = TestUtils.Strings.First();
        var userId = TestUtils.Guids[0];

        resources.FakeRedis
            .Setup(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"))
            .ReturnsAsync(userId);

        var now = DateTime.UtcNow;

        var user = new User(new UserUpdateOrCreateInput
        {
            DisplayName = TestUtils.Strings[1],
        }, now)
        {
            Id = userId,
            Tenants = new List<Tenant>
            {
                new(now)
            }
        };
        user.ToggleActivity();
        await resources.UseRepository.AddAsync(user, true);

        // Act
        var result = await service.RefreshToken(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(LoginErrorCode.InactivatedUser);
        result.Token.Should().BeNull();
        result.RefreshToken.Should().BeNull();

        resources.FakeRedis
            .Verify(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"), Times.Once);
        resources.FakeRedis
            .Verify(r => r.RemoveAsync($"refresh-token-{refreshToken}"), Times.Never);
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value, Times.Never);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value, Times.Never);
    }

    [Fact(DisplayName = "Refresh token with fail, no user")]
    public async Task RefreshToken_NoUser()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var refreshToken = TestUtils.Strings.First();
        var userId = TestUtils.Guids[0];

        resources.FakeRedis
            .Setup(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"))
            .ReturnsAsync(userId);

        // Act
        var result = await service.RefreshToken(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(LoginErrorCode.InvalidRefreshToken);
        result.Token.Should().BeNull();
        result.RefreshToken.Should().BeNull();

        resources.FakeRedis
            .Verify(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"), Times.Once);
        resources.FakeRedis
            .Verify(r => r.RemoveAsync($"refresh-token-{refreshToken}"), Times.Never);
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value, Times.Never);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value, Times.Never);
    }

    [Fact(DisplayName = "Refresh token with fail, no refresh token")]
    public async Task RefreshToken_NoRefreshToken()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var refreshToken = TestUtils.Strings.First();

        // Act
        var result = await service.RefreshToken(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(LoginErrorCode.InvalidRefreshToken);
        result.Token.Should().BeNull();
        result.RefreshToken.Should().BeNull();

        resources.FakeRedis
            .Verify(r => r.GetAsync<Guid>($"refresh-token-{refreshToken}"), Times.Once);
        resources.FakeRedis
            .Verify(r => r.RemoveAsync($"refresh-token-{refreshToken}"), Times.Never);
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value, Times.Never);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value, Times.Never);
    }

    #endregion

    #region Logout

    [Fact(DisplayName = "Logout")]
    public async Task Logout()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var token = TestUtils.Strings.First();
        var refreshToken = TestUtils.Strings.Last();

        resources.FakeRedis.Setup(r => r.GetAsync<string>($"token-refresh-{token}"))
            .ReturnsAsync(refreshToken);
        resources.FakeConfiguration.Setup(c => c.GetSection(AuthenticationConsts.TokenExpireInMinutesConfigKey).Value)
            .Returns("120");

        // Act
        await service.Logout(token);

        // Assert
        resources.FakeRedis.Verify(r => r.GetAsync<string>($"token-refresh-{token}"), Times.Once);
        resources.FakeRedis.Verify(r => r.RemoveAsync($"token-refresh-{token}"), Times.Once);
        resources.FakeRedis.Verify(r => r.RemoveAsync($"refresh-token-{refreshToken}"), Times.Once);
        resources.FakeRedis.Verify(
            r => r.SetAsync($"logged-out-{token}", true, It.IsAny<DistributedCacheEntryOptions>()), Times.Once);

        resources.FakeConfiguration.Verify(c => c.GetSection(AuthenticationConsts.TokenExpireInMinutesConfigKey).Value,
            Times.Once);

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
    }

    #endregion

    #region GenerateTokenAsync

    [Fact(DisplayName = "Generate token async")]
    public async Task GenerateTokenAsync()
    {
        // Arrange
        var resources = CreateMockResources();
        var service = CreateService(resources);

        var now = DateTime.UtcNow;

        DateTimeProvider.Setup(d => d.UtcNow())
            .Returns(now);

        var user = new UserDto()
        {
            DisplayName = TestUtils.Strings[1],
            Tenants = { new Tenant(now) },
            Id = TestUtils.Guids[0]
        };

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtKeyConfigKey).Value)
            .Returns("super-ultra-mega-power-nano-mili-cent-supe-secret-batman-key");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenExpireInMinutesConfigKey).Value)
            .Returns("120");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value)
            .Returns(TestUtils.Strings[3]);
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value)
            .Returns(TestUtils.Strings[4]);

        // Act
        var result = await service.GenerateTokenAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Success.Should().BeTrue();

        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value, Times.Once);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value, Times.Once);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenJwtKeyConfigKey).Value, Times.Once);
        resources.FakeConfiguration
            .Verify(c => c.GetSection(AuthenticationConsts.TokenExpireInMinutesConfigKey).Value, Times.Once);

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
    }

    #endregion

    #region Arranges

    private AuthenticationTokenService CreateService(MockResources resources)
    {
        return new AuthenticationTokenService(
            resources.CredentialRepository,
            resources.UseRepository,
            resources.FakeConfiguration.Object,
            resources.FakeRedis.Object,
            DateTimeProvider.Object
        );
    }

    private MockResources CreateMockResources()
    {
        var key = "11111111111111111111111111111111";
        var iv = "1111111111111111";

        var resources = new MockResources
        {
            CredentialRepository = base.GetRepository<UserCredential>(),
            UseRepository = base.GetRepository<User>(),
            FakeRedis = new Mock<IRedisCacheService>(),
            FakeConfiguration = new Mock<IConfiguration>(),
            CryptoHelper = new CryptoHelper(key, iv)
        };

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value)
            .Returns(key);
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value)
            .Returns(iv);

        return resources;
    }

    private class MockResources
    {
        public IRepository<UserCredential> CredentialRepository { get; set; }
        public IRepository<User> UseRepository { get; set; }
        public Mock<IRedisCacheService> FakeRedis { get; set; }
        public Mock<IConfiguration> FakeConfiguration { get; set; }
        public CryptoHelper CryptoHelper { get; set; }
    }

    #endregion
}