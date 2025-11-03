using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
using Fin.Application.Authentications.Services;
using Fin.Application.Globals.Dtos;
using Fin.Application.Users.Services;
using Fin.Domain.Global;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Domain.Users.Factories;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Enums;
using Fin.Infrastructure.Constants;
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
        var credential = UserCredentialFactory.Create(user.Id, encryptedEmail, null, UserCredentialFactoryType.Password);
        credential.User = user;
        
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
        var credential = UserCredentialFactory.Create(user.Id, encryptedEmail, null, UserCredentialFactoryType.Password);
        credential.User = user;
        
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

    #region ResetPassword

    [Fact]
    public async Task ResetPassword_InvalidPassword()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = new ResetPasswordInput
        {
            ResetToken = TestUtils.Strings.First(),
            Password = "123",
            PasswordConfirmation = "123"
        };

        // Act
        var result = await service.ResetPassword(input);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ResetPasswordErrorCode.InvalidPassword);
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeFalse();
        
        resources.FakeCache
            .Verify(c => c.GetAsync<Guid>(It.IsAny<string>()), Times.Never);;
    }
    
    [Fact]
    public async Task ResetPassword_NotSamePassword()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = new ResetPasswordInput
        {
            ResetToken = TestUtils.Strings.First(),
            Password = "12QAqa!#$",
            PasswordConfirmation = "123"
        };

        // Act
        var result = await service.ResetPassword(input);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ResetPasswordErrorCode.NotSamePassword);
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeFalse();
        
        resources.FakeCache
            .Verify(c => c.GetAsync<Guid>(It.IsAny<string>()), Times.Never);;
    }
    
    [Fact]
    public async Task ResetPassword_NoCredential()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = new ResetPasswordInput
        {
            ResetToken = TestUtils.Strings.First(),
            Password = "12QAqa!#$",
            PasswordConfirmation = "12QAqa!#$"
        };

        // Act
        var result = await service.ResetPassword(input);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ResetPasswordErrorCode.InvalidToken);
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeFalse();
        
        resources.FakeCache
            .Verify(c => c.GetAsync<Guid>(It.IsAny<string>()), Times.Never);;
    }
    
    [Fact]
    public async Task ResetPassword_TokenExpired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = new ResetPasswordInput
        {
            ResetToken = TestUtils.Strings.First(),
            Password = "12QAqa!#$",
            PasswordConfirmation = "12QAqa!#$"
        };

        var user = new User
        {
            Id = TestUtils.Guids[0],
            Credential = UserCredentialFactory.Create(TestUtils.Guids[0], "123", "123",  UserCredentialFactoryType.Password)
        };
        user.Credential.ResetToken = input.ResetToken;
        user.Credential.User = user;
        await resources.CredentialRepository.AddAsync(user.Credential, true);
        
        // Act
        var result = await service.ResetPassword(input);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ResetPasswordErrorCode.ExpiredToken);
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeFalse();
        
        resources.FakeCache
            .Verify(c => c.GetAsync<Guid>(It.Is<string>(f => f.Contains(input.ResetToken))), Times.Once);;
    }
    
    [Fact]
    public async Task ResetPassword_InvalidToken()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = new ResetPasswordInput
        {
            ResetToken = TestUtils.Strings.First(),
            Password = "12QAqa!#$",
            PasswordConfirmation = "12QAqa!#$"
        };

        var user = new User
        {
            Id = TestUtils.Guids[0],
            Credential = UserCredentialFactory.Create(TestUtils.Guids[0], "123", "123",  UserCredentialFactoryType.Password)
        };
        user.Credential.ResetToken = input.ResetToken;
        user.Credential.User = user;
        await resources.CredentialRepository.AddAsync(user.Credential, true);
        
        resources.FakeCache
            .Setup(c => c.GetAsync<Guid>(It.Is<string>(f => f.Contains(input.ResetToken))))
            .ReturnsAsync(TestUtils.Guids[1]);
        
        // Act
        var result = await service.ResetPassword(input);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ResetPasswordErrorCode.InvalidToken);
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeFalse();
        
        resources.FakeCache
            .Verify(c => c.GetAsync<Guid>(It.Is<string>(f => f.Contains(input.ResetToken))), Times.Once);;
    }
    
    [Fact]
    public async Task ResetPassword_Success()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = new ResetPasswordInput
        {
            ResetToken = TestUtils.Strings.First(),
            Password = "12QAqa!#$",
            PasswordConfirmation = "12QAqa!#$"
        };

        var user = new User
        {
            Id = TestUtils.Guids[0],
            Credential = UserCredentialFactory.Create(TestUtils.Guids[0], "123", "123",  UserCredentialFactoryType.Password)
        };
        user.Credential.ResetToken = input.ResetToken;
        user.Credential.User = user;
        await resources.CredentialRepository.AddAsync(user.Credential, true);
        
        resources.FakeCache
            .Setup(c => c.GetAsync<Guid>(It.Is<string>(f => f.Contains(input.ResetToken))))
            .ReturnsAsync(TestUtils.Guids[0]);
        
        
        
        // Act
        var result = await service.ResetPassword(input);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorCode.Should().Be(null);
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeTrue();
        
        resources.FakeCache
            .Verify(c => c.GetAsync<Guid>(It.Is<string>(f => f.Contains(input.ResetToken))), Times.Once);;
        
        var encryptedPassword = resources.CryptoHelper.Encrypt(input.Password);
        var dbCredential = await resources.CredentialRepository.Query(false).FirstAsync();
        dbCredential.ResetToken.Should().BeNullOrEmpty();
        dbCredential.EncryptedPassword.Should().Be(encryptedPassword);
    }

    #endregion

    [Fact]
    public async Task Login()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new LoginInput
        {
            Email = TestUtils.Strings.First(),
            Password = TestUtils.Strings.Last()
        };

        var output = new LoginOutput
        {
            Success = true,
            Token = TestUtils.Strings[1],
            RefreshToken = TestUtils.Strings[2],
        };
        
        resources.FakeTokenService
            .Setup(t => t.Login(input))
            .ReturnsAsync(output);
        
        // Act
        var result = await service.Login(input);
        
        // Assert
        result.Should().BeEquivalentTo(output);
        resources.FakeTokenService
            .Verify(t => t.Login(input), Times.Once);
    }

    #region LoginOrSingInWithGoogle

    [Fact]
    public async Task LoginOrSingInWithGoogle_InactivatedUser()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var email = TestUtils.Strings.First();
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var input = new LoginWithGoogleInput
        {
            Email = email,
        };
        
        var user = new User();
        var credential = UserCredentialFactory.Create(user.Id, encryptedEmail, null, UserCredentialFactoryType.Password);
        credential.User = user;                                                                         
        
        await resources.CredentialRepository.AddAsync(credential, true);

        // Act
        var result = await service.LoginOrSingInWithGoogle(input);

        // Assert
        result.Success.Should().BeFalse();
        result.MustToCreateUser.Should().BeFalse();
        result.ErrorCode.Should().Be(LoginErrorCode.InactivatedUser);
        result.Token.Should().BeNullOrEmpty();
        result.RefreshToken.Should().BeNullOrEmpty();

        resources.FakeUserCreateService
            .Verify(u => u.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserUpdateOrCreateInput>()), Times.Never);
        resources.FakeTokenService
            .Verify(u => u.GenerateTokenAsync(It.IsAny<UserDto>()), Times.Never);
    }
    
    [Fact]
    public async Task LoginOrSingInWithGoogle_DifferentGoogleId()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var email = TestUtils.Strings.First();
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var input = new LoginWithGoogleInput
        {
            Email = email,
        };
        
        var user = new User();
        var credential = UserCredentialFactory.Create(user.Id, encryptedEmail, TestUtils.Strings[1], UserCredentialFactoryType.Google);
        credential.User = user;
        user.Credential = credential;
        user.ToggleActivity();
        await resources.CredentialRepository.AddAsync(credential, true);

        // Act
        var result = await service.LoginOrSingInWithGoogle(input);

        // Assert
        result.Success.Should().BeFalse();
        result.MustToCreateUser.Should().BeFalse();
        result.ErrorCode.Should().Be(LoginErrorCode.DifferentGoogleAccountLinked);
        result.Token.Should().BeNullOrEmpty();
        result.RefreshToken.Should().BeNullOrEmpty();

        resources.FakeUserCreateService
            .Verify(u => u.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserUpdateOrCreateInput>()), Times.Never);
        resources.FakeTokenService
            .Verify(u => u.GenerateTokenAsync(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task LoginOrSingInWithGoogle_CantCreateUser()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new LoginWithGoogleInput
        {
            Email = TestUtils.Strings.First(),
            GoogleId = TestUtils.Strings[1],
            DisplayName = TestUtils.Strings[2],
        };

        resources.FakeUserCreateService
            .Setup(u => u.CreateUser(input.GoogleId, input.Email,
                It.Is<UserUpdateOrCreateInput>(u => u.DisplayName == input.DisplayName)))
            .ReturnsAsync(new ValidationResultDto<UserDto>());

        // Act
        var result = await service.LoginOrSingInWithGoogle(input);

        // Assert
        result.Success.Should().BeFalse();
        result.MustToCreateUser.Should().BeFalse();
        result.ErrorCode.Should().Be(LoginErrorCode.CantCreateUser);
        result.Token.Should().BeNullOrEmpty();
        result.RefreshToken.Should().BeNullOrEmpty();

        resources.FakeUserCreateService
            .Verify(u => u.CreateUser(input.GoogleId, input.Email,
                It.Is<UserUpdateOrCreateInput>(u => u.DisplayName == input.DisplayName)), Times.Once);
        resources.FakeTokenService
            .Verify(u => u.GenerateTokenAsync(It.IsAny<UserDto>()), Times.Never);
    }
    
    [Fact]
    public async Task LoginOrSingInWithGoogle_Success_CreatingUser()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new LoginWithGoogleInput
        {
            Email = TestUtils.Strings.First(),
            GoogleId = TestUtils.Strings[1],
            DisplayName = TestUtils.Strings[2],
        };

        var user = new UserDto();
        
        resources.FakeUserCreateService
            .Setup(u => u.CreateUser(input.GoogleId, input.Email,
                It.Is<UserUpdateOrCreateInput>(u => u.DisplayName == input.DisplayName)))
            .ReturnsAsync(new ValidationResultDto<UserDto>
            {
                Success = true,
                Data = user
            });

        var output = new LoginOutput
        {
            Success = true,
            Token = TestUtils.Strings[1],
            RefreshToken = TestUtils.Strings[2],
        };
        
        resources.FakeTokenService
            .Setup(t => t.GenerateTokenAsync(user))
            .ReturnsAsync(output);
        
        // Act
        var result = await service.LoginOrSingInWithGoogle(input);

        // Assert
        result.Success.Should().Be(output.Success);;
        result.MustToCreateUser.Should().BeTrue();
        result.ErrorCode.Should().Be(output.ErrorCode);
        result.Token.Should().Be(output.Token);;
        result.RefreshToken.Should().Be(output.RefreshToken);

        resources.FakeUserCreateService
            .Verify(u => u.CreateUser(input.GoogleId, input.Email,
                It.Is<UserUpdateOrCreateInput>(u => u.DisplayName == input.DisplayName)), Times.Once);
        resources.FakeTokenService
            .Verify(u => u.GenerateTokenAsync(user), Times.Once);
    }
    
    [Fact]
    public async Task LoginOrSingInWithGoogle_Success_Not_CreatingUser_NeverGoogle()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var email = TestUtils.Strings.First();
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        
        var input = new LoginWithGoogleInput
        {
            Email = email,
            GoogleId = TestUtils.Strings[1],
        };

        var user = new User
        {
            Id = TestUtils.Guids[0]
        };
        var credential = UserCredentialFactory.Create(user.Id, encryptedEmail, null, UserCredentialFactoryType.Password);
        credential.User = user;
        
        user.Credential = credential;
        user.ToggleActivity();
        await resources.CredentialRepository.AddAsync(credential, true);
        
        var output = new LoginOutput
        {
            Success = true,
            Token = TestUtils.Strings[1],
            RefreshToken = TestUtils.Strings[2],
        };
        resources.FakeTokenService
            .Setup(t => t.GenerateTokenAsync(It.Is<UserDto>(u =>  u.Id == user.Id)))
            .ReturnsAsync(output);
        
        // Act
        var result = await service.LoginOrSingInWithGoogle(input);

        // Assert
        result.Success.Should().Be(output.Success);;
        result.MustToCreateUser.Should().BeFalse();
        result.ErrorCode.Should().Be(output.ErrorCode);
        result.Token.Should().Be(output.Token);;
        result.RefreshToken.Should().Be(output.RefreshToken);

        resources.FakeUserCreateService
            .Verify(u => u.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserUpdateOrCreateInput>()), Times.Never);
        resources.FakeTokenService
            .Verify(u => u.GenerateTokenAsync(It.Is<UserDto>(u =>  u.Id == user.Id)), Times.Once);
        
        var dbCredential = await resources.CredentialRepository.Query(false).FirstAsync();
        dbCredential.GoogleId.Should().Be(input.GoogleId);
    }
    
        [Fact]
    public async Task LoginOrSingInWithGoogle_Success_Not_CreatingUser_AlreadyGoogle()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var email = TestUtils.Strings.First();
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        
        var input = new LoginWithGoogleInput
        {
            Email = email,
            GoogleId = TestUtils.Strings[1],
        };

        var user = new User
        {
            Id = TestUtils.Guids[0],
        };
        var credential = UserCredentialFactory.Create(user.Id, encryptedEmail, null, UserCredentialFactoryType.Google);
        credential.User = user;
        credential.GoogleId = input.GoogleId;
        
        user.Credential = credential;
        user.ToggleActivity();
        await resources.CredentialRepository.AddAsync(credential, true);
        
        var output = new LoginOutput
        {
            Success = true,
            Token = TestUtils.Strings[1],
            RefreshToken = TestUtils.Strings[2],
        };
        resources.FakeTokenService
            .Setup(t => t.GenerateTokenAsync(It.Is<UserDto>(u =>  u.Id == user.Id)))
            .ReturnsAsync(output);
        
        // Act
        var result = await service.LoginOrSingInWithGoogle(input);

        // Assert
        result.Success.Should().Be(output.Success);;
        result.MustToCreateUser.Should().BeFalse();
        result.ErrorCode.Should().Be(output.ErrorCode);
        result.Token.Should().Be(output.Token);;
        result.RefreshToken.Should().Be(output.RefreshToken);

        resources.FakeUserCreateService
            .Verify(u => u.CreateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserUpdateOrCreateInput>()), Times.Never);
        resources.FakeTokenService
            .Verify(u => u.GenerateTokenAsync(It.Is<UserDto>(u =>  u.Id == user.Id)), Times.Once);
        
        var dbCredential = await resources.CredentialRepository.Query(false).FirstAsync();
        dbCredential.GoogleId.Should().Be(input.GoogleId);
    }
    
    #endregion
    
    [Fact]
    public async Task RefreshToken()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var token = TestUtils.Strings.First();
        var output = new LoginOutput
        {
            Success = true,
            Token = TestUtils.Strings[1],
            RefreshToken = TestUtils.Strings[2],
        };
        
        resources.FakeTokenService
            .Setup(t => t.RefreshToken(token))
            .ReturnsAsync(output);
        
        // Act
        var result = await service.RefreshToken(token);
        
        // Assert
        result.Should().BeEquivalentTo(output);
        resources.FakeTokenService
            .Verify(t => t.RefreshToken(token), Times.Once);
    }
    
    [Fact]
    public async Task Logout()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var token = TestUtils.Strings.First();
        
        // Act
        await service.Logout(token);
        
        // Assert
        resources.FakeTokenService
            .Verify(t => t.Logout(token), Times.Once);
    }
    
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
            .Setup(c => c.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value)
            .Returns("1234567890qwerty1234567890qwerty");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value)
            .Returns("1234567890qwerty");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AppConstants.FrontUrlConfigKey).Value)
            .Returns("http://localhost:4200");
        
        var encryptKey = resources.FakeConfiguration.Object.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = resources.FakeConfiguration.Object.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ?? "";

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