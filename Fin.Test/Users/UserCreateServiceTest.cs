using Fin.Application.Globals.Services;
using Fin.Application.Users.Dtos;
using Fin.Application.Users.Enums;
using Fin.Application.Users.Services;
using Fin.Domain.Global;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Redis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.Users;

public class UserCreateServiceTest : TestUtils.BaseTestWithContext
{
    #region StartCreate

    [Fact]
    public async Task StartCreateUser_InvalidPassword()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new UserStartCreateInput()
        {
            Email = TestUtils.Strings.First(),
            Password = "123",
            PasswordConfirmation = "123"
        };

        // Act
        var result = await service.StartCreate(input);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorCode.Should().Be(UserStartCreateErrorCode.InvalidPassword);

        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserCreateProcessDto>(),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Never);
    }

    [Fact]
    public async Task StartCreateUser_NotSamePassword()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new UserStartCreateInput()
        {
            Email = TestUtils.Strings.First(),
            Password = "1@QWqwe@",
            PasswordConfirmation = "123"
        };

        // Act
        var result = await service.StartCreate(input);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorCode.Should().Be(UserStartCreateErrorCode.NotSamePassword);

        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserCreateProcessDto>(),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Never);
    }

    [Fact]
    public async Task StartCreateUser_EmailInUse()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new UserStartCreateInput()
        {
            Email = TestUtils.Strings.First(),
            Password = "1@QWqwe@",
            PasswordConfirmation = "1@QWqwe@"
        };

        var encryptedEmail = resources.CryptoHelper.Encrypt(input.Email);

        var user = new User();
        var credential = new UserCredential(user.Id, encryptedEmail, null)
        {
            User = user
        };
        await resources.CredentialRepository.AddAsync(credential, true);

        // Act
        var result = await service.StartCreate(input);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorCode.Should().Be(UserStartCreateErrorCode.EmailAlreadyInUse);

        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserCreateProcessDto>(),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Never);
    }

    [Fact]
    public async Task StartCreateUser_Success()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new UserStartCreateInput()
        {
            Email = TestUtils.Strings.First(),
            Password = "1@QWqwe@",
            PasswordConfirmation = "1@QWqwe@"
        };

        var encryptedEmail = resources.CryptoHelper.Encrypt(input.Email);
        var encryptedPassword = resources.CryptoHelper.Encrypt(input.Password);

        var confirmationCode = TestUtils.Strings[4];
        resources.FakeCodeGenerator
            .Setup(c => c.Generate())
            .Returns(confirmationCode);

        var sentDateTime = TestUtils.UtcDateTimes[0];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(sentDateTime);

        // Act
        var result = await service.StartCreate(input);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.ErrorCode.Should().NotBeDefined();

        result.Data?.Email.Should().Be(input.Email);
        result.Data?.SentEmailDateTime.Should().Be(sentDateTime);
        result.Data?.CreationToken.Should().NotBeNullOrEmpty();

        resources.FakeCache
            .Verify(c => c.SetAsync(It.Is<string>(k => k.Contains(result.Data.CreationToken)),
                It.Is<UserCreateProcessDto>(u =>
                    u.EncryptedEmail == encryptedEmail && u.EncryptedPassword == encryptedPassword &&
                    u.Token == result.Data.CreationToken && u.EmailConfirmationCode == confirmationCode &&
                    u.EmailSentDateTime == sentDateTime && !u.ValidatedEmail),
                It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
        resources.FakeEmailSender
            .Verify(
                e => e.SendEmailAsync(input.Email, It.IsAny<string>(),
                    It.Is<string>(b => b.Contains(confirmationCode))), Times.Once);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Once);
    }

    #endregion

    #region ResendConfirmationEmail

    [Fact]
    public async Task ResendConfirmationEmail_UnauthorizedCode()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var process = new UserCreateProcessDto
        {
            Token = TestUtils.Strings.First(),
            EmailSentDateTime = TestUtils.UtcDateTimes[0],
        };

        var error = false;

        // Act
        try
        {
            await service.ResendConfirmationEmail(process.Token);
        }
        catch (UnauthorizedAccessException)
        {
            error = true;
        }

        // Assert
        error.Should().BeTrue();

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))), Times.Once);

        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserCreateProcessDto>(),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationEmail_LessThan1Minute()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var process = new UserCreateProcessDto
        {
            Token = TestUtils.Strings.First(),
            EmailSentDateTime = TestUtils.UtcDateTimes[0],
        };

        resources.FakeCache
            .Setup(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))))
            .ReturnsAsync(process);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(process.EmailSentDateTime);

        // Act
        await service.ResendConfirmationEmail(process.Token);

        // Assert
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))), Times.Once);

        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserCreateProcessDto>(),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationEmail_Success()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var now = TestUtils.UtcDateTimes[0];
        var email = TestUtils.Strings.First();

        var process = new UserCreateProcessDto
        {
            Token = TestUtils.Strings.First(),
            EmailSentDateTime = now.AddMinutes(-1),
            EncryptedEmail = resources.CryptoHelper.Encrypt(email),
        };

        resources.FakeCache
            .Setup(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))))
            .ReturnsAsync(process);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);


        var confirmationCode = TestUtils.Strings[4];
        resources.FakeCodeGenerator
            .Setup(c => c.Generate())
            .Returns(confirmationCode);

        // Act
        await service.ResendConfirmationEmail(process.Token);

        // Assert
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))), Times.Once);

        resources.FakeCache
            .Verify(c => c.SetAsync(It.Is<string>(k => k.Contains(process.Token)),
                It.Is<UserCreateProcessDto>(u =>
                    u.EmailConfirmationCode == confirmationCode && u.EmailSentDateTime == now && !u.ValidatedEmail),
                It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
        resources.FakeEmailSender
            .Verify(
                e => e.SendEmailAsync(email, It.IsAny<string>(),
                    It.Is<string>(b => b.Contains(confirmationCode))), Times.Once);

        resources.FakeCodeGenerator
            .Verify(c => c.Generate(), Times.Once);
    }

    #endregion

    #region ValidateEmailCode

    [Fact]
    public async Task ValidateEmailCode_UnauthorizedCode()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var creationToken = TestUtils.Strings.First();
        var emailConfirmationCode = TestUtils.Strings.Last();

        var erro = false;

        // Act
        try
        {
            await service.ValidateEmailCode(creationToken, emailConfirmationCode);
        }
        catch (UnauthorizedAccessException)
        {
            erro = true;
        }

        // Assert
        erro.Should().BeTrue();

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))), Times.Once);
        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserCreateProcessDto>(),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Never());
    }

    [Fact]
    public async Task ValidateEmailCode_InvalidCode()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var creationToken = TestUtils.Strings.First();
        var emailConfirmationCode = TestUtils.Strings.Last();

        var process = new UserCreateProcessDto
        {
            EmailConfirmationCode = TestUtils.Strings[1],
            Token = creationToken,
        };

        resources.FakeCache
            .Setup(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))))
            .ReturnsAsync(process);

        // Act
        var result = await service.ValidateEmailCode(creationToken, emailConfirmationCode);


        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().Be(false);
        result.Message.Should().NotBeNullOrEmpty();
        result.ErrorCode.Should().BeNull();

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))), Times.Once);
        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.Is<string>(k => k.Contains(creationToken)),
                    It.Is<UserCreateProcessDto>(p =>
                        !p.ValidatedEmail && p.Token == creationToken &&
                        p.EmailConfirmationCode == process.EmailConfirmationCode),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
    }

    [Fact]
    public async Task ValidateEmailCode_Success()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var creationToken = TestUtils.Strings.First();
        var emailConfirmationCode = TestUtils.Strings.Last();

        var process = new UserCreateProcessDto
        {
            EmailConfirmationCode = emailConfirmationCode,
            Token = creationToken,
        };

        resources.FakeCache
            .Setup(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))))
            .ReturnsAsync(process);

        // Act
        var result = await service.ValidateEmailCode(creationToken, emailConfirmationCode);


        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(true);
        result.Message.Should().NotBeNullOrEmpty();
        result.ErrorCode.Should().BeNull();

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))), Times.Once);
        resources.FakeCache
            .Verify(
                c => c.SetAsync(It.Is<string>(k => k.Contains(creationToken)),
                    It.Is<UserCreateProcessDto>(p =>
                        p.ValidatedEmail && p.Token == creationToken &&
                        p.EmailConfirmationCode == process.EmailConfirmationCode),
                    It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
    }

    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateUser_InvalidCreationToken()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var erro = false;

        var creationToken = TestUtils.Strings.First();

        // Act
        try
        {
            await service.CreateUser(creationToken, new UserUpdateOrCreateInput());
        }
        catch (UnauthorizedAccessException)
        {
            erro = true;
        }

        // Assert
        erro.Should().BeTrue();

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))), Times.Once);

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        resources.FakeCache
            .Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_NotValidatedEmail()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var creationToken = TestUtils.Strings.First();

        var process = new UserCreateProcessDto
        {
            ValidatedEmail = false,
            Token = creationToken
        };

        resources.FakeCache
            .Setup(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))))
            .ReturnsAsync(process);

        var erro = false;

        // Act
        try
        {
            await service.CreateUser(creationToken, new UserUpdateOrCreateInput());
        }
        catch (UnauthorizedAccessException)
        {
            erro = true;
        }

        // Assert
        erro.Should().BeTrue();

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(creationToken))), Times.Once);

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        resources.FakeCache
            .Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }


    [Fact]
    public async Task CreateUser_Success()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var now = TestUtils.UtcDateTimes[0];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        var process = new UserCreateProcessDto
        {
            ValidatedEmail = true,
            Token = TestUtils.Strings.First(),
            EncryptedEmail = TestUtils.Strings[1],
            EncryptedPassword = TestUtils.Strings[2],
        };
        resources.FakeCache
            .Setup(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))))
            .ReturnsAsync(process);

        var input = new UserUpdateOrCreateInput
        {
            DisplayName = TestUtils.Strings[3],
            LastName = TestUtils.Strings[4],
            FirstName = TestUtils.Strings[5],
            BirthDate = DateOnly.FromDateTime(TestUtils.UtcDateTimes[1]),
            ImagePublicUrl = TestUtils.Strings[6],
        };

        // Act
        var result = await service.CreateUser(process.Token, input);
        
        // Assert
        var tenant = await resources.TenantRepository.Query(false).Include(t => t.Users).FirstAsync();
        var credential = await resources.CredentialRepository.Query(false).FirstAsync();
        var user = await resources.UserRepository.Query(false).FirstAsync();
        var notificationSettings = await resources.UserNotificationSettings.Query(false).FirstAsync();
        var userRemember = await resources.UserRememberUseSettings.Query(false).FirstAsync();

        credential.EncryptedEmail.Should().Be(process.EncryptedEmail);
        credential.EncryptedPassword.Should().Be(process.EncryptedPassword);
        credential.FailLoginAttempts.Should().Be(0);
        credential.UserId.Should().Be(user.Id);
        
        user.IsActivity.Should().BeTrue();
        user.CreatedAt.Should().Be(now);
        user.UpdatedAt.Should().Be(now);
        user.DisplayName.Should().Be(input.DisplayName);
        user.FirstName.Should().Be(input.FirstName);
        user.LastName.Should().Be(input.LastName);
        user.BirthDate.Should().Be(input.BirthDate);
        user.Gender.Should().Be(input.Gender);
        user.ImagePublicUrl.Should().Be(input.ImagePublicUrl);

        tenant.Locale.Should().Be("pt-Br");
        tenant.Timezone.Should().Be("America/Sao_Paulo");
        tenant.Users.First().Id.Should().Be(user.Id);

        notificationSettings.UserId.Should().Be(user.Id);
        userRemember.UserId.Should().Be(user.Id);

        resources.FakeCache
            .Verify(c => c.GetAsync<UserCreateProcessDto>(It.Is<string>(k => k.Contains(process.Token))), Times.Once);
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
        resources.FakeCache
            .Verify(c => c.RemoveAsync(It.Is<string>(k => k.Contains(process.Token))), Times.Once);

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(user.Id);
        result.Data.DisplayName.Should().Be(user.DisplayName);
        result.Data.FirstName.Should().Be(user.FirstName);
        result.Data.LastName.Should().Be(user.LastName);
        result.Data.BirthDate.Should().Be(user.BirthDate);
        result.Data.Gender.Should().Be(user.Gender);
        result.Data.ImagePublicUrl.Should().Be(user.ImagePublicUrl);
        result.Data.IsActivity.Should().Be(user.IsActivity);
        result.Data.IsAdmin.Should().Be(user.IsAdmin);
    }

    #endregion

    #region CreateUser With Google

    [Fact]
    public async Task CreateUser_WithGoogle_Success()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new UserUpdateOrCreateInput
        {
            DisplayName = TestUtils.Strings[3],
            LastName = TestUtils.Strings[4],
            FirstName = TestUtils.Strings[5],
            BirthDate = DateOnly.FromDateTime(TestUtils.UtcDateTimes[1]),
            ImagePublicUrl = TestUtils.Strings[6],
        };

        var googleId = TestUtils.Strings[1];
        var email = TestUtils.Strings[2];

        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var now = TestUtils.UtcDateTimes[0];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        // Act
        var result = await service.CreateUser(googleId, email, input);

        // Assert
        var tenant = await resources.TenantRepository.Query(false).Include(t => t.Users).FirstAsync();
        var credential = await resources.CredentialRepository.Query(false).FirstAsync();
        var user = await resources.UserRepository.Query(false).FirstAsync();
        var notificationSettings = await resources.UserNotificationSettings.Query(false).FirstAsync();
        var userRemember = await resources.UserRememberUseSettings.Query(false).FirstAsync();

        credential.EncryptedEmail.Should().Be(encryptedEmail);
        credential.EncryptedPassword.Should().BeNullOrEmpty();
        credential.GoogleId.Should().Be(googleId);
        credential.FailLoginAttempts.Should().Be(0);
        credential.UserId.Should().Be(user.Id);

        user.IsActivity.Should().BeTrue();
        user.CreatedAt.Should().Be(now);
        user.UpdatedAt.Should().Be(now);
        user.DisplayName.Should().Be(input.DisplayName);
        user.FirstName.Should().Be(input.FirstName);
        user.LastName.Should().Be(input.LastName);
        user.BirthDate.Should().Be(input.BirthDate);
        user.Gender.Should().Be(input.Gender);
        user.ImagePublicUrl.Should().Be(input.ImagePublicUrl);


        tenant.Locale.Should().Be("pt-Br");
        tenant.Timezone.Should().Be("America/Sao_Paulo");
        tenant.Users.First().Id.Should().Be(user.Id);

        notificationSettings.UserId.Should().Be(user.Id);
        userRemember.UserId.Should().Be(user.Id);

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(user.Id);
        result.Data.DisplayName.Should().Be(user.DisplayName);
        result.Data.FirstName.Should().Be(user.FirstName);
        result.Data.LastName.Should().Be(user.LastName);
        result.Data.BirthDate.Should().Be(user.BirthDate);
        result.Data.Gender.Should().Be(user.Gender);
        result.Data.ImagePublicUrl.Should().Be(user.ImagePublicUrl);
        result.Data.IsActivity.Should().Be(user.IsActivity);
        result.Data.IsAdmin.Should().Be(user.IsAdmin);
    }

     [Fact]
    public async Task CreateUser_WithGoogle_EmailAlreadyInUse()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new UserUpdateOrCreateInput
        {
        };

        var googleId = TestUtils.Strings[1];
        var email = TestUtils.Strings[2];
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var userId = TestUtils.Guids[3];
        var user = new User()
        {
            Id = userId,
            Credential = UserCredential.CreateWithGoogle(userId, encryptedEmail, googleId)
        };
        await resources.UserRepository.AddAsync(user, true);

        // Act
        var result = await service.CreateUser(googleId, email, input);

        // Assert
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    #endregion

    private UserCreateService GetService(Resources resources)
    {
        return new UserCreateService(
            resources.CredentialRepository,
            resources.TenantRepository,
            resources.UserRepository,
            DateTimeProvider.Object,
            resources.FakeConfiguration.Object,
            resources.FakeCache.Object,
            resources.FakeEmailSender.Object,
            resources.FakeCodeGenerator.Object,
            resources.UserNotificationSettings,
            resources.UserRememberUseSettings,
            UnitOfWork
        );
    }

    private Resources GetResources()
    {
        var resources = new Resources
        {
            CredentialRepository = GetRepository<UserCredential>(),
            UserRepository = GetRepository<User>(),
            TenantRepository = GetRepository<Tenant>(),
            UserNotificationSettings = GetRepository<UserNotificationSettings>(),
            UserRememberUseSettings = GetRepository<UserRememberUseSetting>(),
            FakeCache = new Mock<IRedisCacheService>(),
            FakeEmailSender = new Mock<IEmailSenderService>(),
            FakeConfiguration = new Mock<IConfiguration>(),
            FakeCodeGenerator = new Mock<IConfirmationCodeGenerator>(),
        };

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value)
            .Returns("1234567890qwerty1234567890qwerty");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value)
            .Returns("1234567890qwerty");

        var encryptKey =
            resources.FakeConfiguration.Object.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = resources.FakeConfiguration.Object.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ??
                        "";

        resources.CryptoHelper = new CryptoHelper(encryptKey, encryptIv);

        return resources;
    }

    private class Resources
    {
        public IRepository<UserCredential> CredentialRepository { get; init; }
        public IRepository<User> UserRepository { get; init; }
        public IRepository<Tenant> TenantRepository { get; init; }
        public IRepository<UserNotificationSettings> UserNotificationSettings { get; init; }
        public IRepository<UserRememberUseSetting> UserRememberUseSettings { get; init; }
        public Mock<IRedisCacheService> FakeCache { get; init; }
        public Mock<IConfiguration> FakeConfiguration { get; init; }
        public Mock<IEmailSenderService> FakeEmailSender { get; init; }
        public Mock<IConfirmationCodeGenerator> FakeCodeGenerator { get; init; }
        public CryptoHelper CryptoHelper { get; set; }
    }
}