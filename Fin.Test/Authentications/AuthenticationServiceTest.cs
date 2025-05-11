using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Services;
using Fin.Application.Users.Services;
using Fin.Domain.Global;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Authentications.Consts;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Redis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.Authentications;

public class AuthenticationServiceTest: TestUtils.BaseTestWithContext
{
    #region SendResetPasswordEmail

    [Fact]
    public async Task SendResetPasswordEmail_ExistEmail()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var email = TestUtils.Strings.First();
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var intput = new SendResetPasswordEmailInput
        {
            Email = email
        };

        var user = new User();
        var credential = new UserCredential(user.Id, encryptedEmail, null)
        {
            User = user
        };
        user.Credential = credential;
        user.ToggleActivity();
        await resources.CredentialRepository.AddAsync(credential, true);

        // Act
        await service.SendResetPasswordEmail(intput);

        // Assert
        credential = await resources.CredentialRepository.Query(false).FirstAsync(); 
        credential.ResetToken.Should().NotBeNullOrEmpty();
        
        resources.FakeCache
            .Verify(c => c.SetAsync($"reset-token-{credential.ResetToken}", credential.UserId, It.IsAny<DistributedCacheEntryOptions>()), Times.Once);;
        
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.Is<string>(b => b.Contains(credential.ResetToken))), Times.Once);
    }
    
    [Fact]
    public async Task SendResetPasswordEmail_InactivatedUser()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var email = TestUtils.Strings.First();
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var intput = new SendResetPasswordEmailInput
        {
            Email = email
        };

        var user = new User();
        var credential = new UserCredential(user.Id, encryptedEmail, null)
        {
            User = user
        };
        await resources.CredentialRepository.AddAsync(credential, true);

        // Act
        await service.SendResetPasswordEmail(intput);

        // Assert
        credential = await resources.CredentialRepository.Query(false).FirstAsync(); 
        credential.ResetToken.Should().BeNullOrEmpty();
        
        resources.FakeCache
            .Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);;
        
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task SendResetPasswordEmail_EmailNoExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var intput = new SendResetPasswordEmailInput
        {
            Email = TestUtils.Strings.First()
        };

        // Act
        await service.SendResetPasswordEmail(intput);

        // Assert
        resources.FakeCache
            .Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);;
        
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion
    
    private AuthenticationService GetService(Resources resources)
    {
        return new AuthenticationService(
            resources.CredentialRepository,
            resources.FakeCache.Object,
            resources.FakeEmailSender.Object,
            resources.FakeConfiguration.Object,
            resources.FakeTokenService.Object,
            resources.FakeUserCreateService.Object
            );
    }
    
    private Resources GetResources()
    {
        var resources =  new Resources
        {
            CredentialRepository = GetRepository<UserCredential>(),
            FakeCache = new Mock<IRedisCacheService>(),
            FakeEmailSender = new Mock<IEmailSenderService>(),
            FakeUserCreateService = new Mock<IUserCreateService>(),
            FakeTokenService = new Mock<IAuthenticationTokenService>(),
            FakeConfiguration = new Mock<IConfiguration>(),
        };

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value)
            .Returns("1234567890qwerty1234567890qwerty");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value)
            .Returns("1234567890qwerty");
        
        var encryptKey = resources.FakeConfiguration.Object.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = resources.FakeConfiguration.Object.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value ?? "";

        resources.CryptoHelper = new CryptoHelper(encryptKey, encryptIv);
        
        return resources;
    }
    
    private class Resources
    {
        public IRepository<UserCredential> CredentialRepository { get; set; }
        public Mock<IRedisCacheService> FakeCache { get; set; }
        public Mock<IEmailSenderService> FakeEmailSender { get; set; }
        public Mock<IUserCreateService> FakeUserCreateService { get; set; }
        public Mock<IAuthenticationTokenService> FakeTokenService { get; set; }
        public Mock<IConfiguration> FakeConfiguration { get; set; }
        public CryptoHelper CryptoHelper { get; set; }
    }
}