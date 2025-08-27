using Fin.Application.Globals.Dtos;
using Fin.Application.Globals.Services;
using Fin.Application.Users.Dtos;
using Fin.Application.Users.Enums;
using Fin.Domain.Global;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Redis;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Users.Services;

public interface IUserCreateService
{
    public Task<ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>> StartCreate(UserStartCreateInput input);
    public Task<ValidationResultDto<DateTime>> ResendConfirmationEmail(string creationToken);
    public Task<ValidationResultDto<bool>> ValidateEmailCode(string creationToken, string emailConfirmationCode);
    public Task<ValidationResultDto<UserDto>> CreateUser(string creationToken, UserUpdateOrCreateInput input);
    public Task<ValidationResultDto<UserDto>> CreateUser(string googleId, string email, UserUpdateOrCreateInput input);
}

public class UserCreateService : IUserCreateService, IAutoTransient
{
    private readonly IRepository<UserCredential> _credentialRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<UserNotificationSettings> _notificationSettingsRepository;
    private readonly IRepository<UserRememberUseSetting> _userRememberUseSettingRepository;
    
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRedisCacheService _cache;
    private readonly IEmailSenderService _emailSender;
    private readonly IConfirmationCodeGenerator _codeGenerator;
    private readonly IUnitOfWork _unitOfWork;

    private readonly CryptoHelper _cryptoHelper;

    public UserCreateService(
        IRepository<UserCredential> credentialRepository,
        IRepository<Tenant> tenantRepository,
        IRepository<User> userRepository,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        IRedisCacheService cache,
        IEmailSenderService emailSender,
        IConfirmationCodeGenerator codeGenerator,
        IRepository<UserNotificationSettings> notificationSettingsRepository,
        IRepository<UserRememberUseSetting> userRememberUseSettingRepository, IUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _dateTimeProvider = dateTimeProvider;
        _cache = cache;
        _emailSender = emailSender;
        _codeGenerator = codeGenerator;
        _notificationSettingsRepository = notificationSettingsRepository;
        _userRememberUseSettingRepository = userRememberUseSettingRepository;
        _unitOfWork = unitOfWork;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;

        var encryptKey = configuration.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = configuration.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ?? "";

        _cryptoHelper = new CryptoHelper(encryptKey, encryptIv);
    }

    public async Task<ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>> StartCreate(UserStartCreateInput input)
    {
        var isValidPass = UserCredential.IsValidPassword(input.Password);
        var isSamePass = input.Password == input.PasswordConfirmation;

        if (!isValidPass)
            return GetResultWithError(UserStartCreateErrorCode.InvalidPassword, "Password do not match requirements");
        if (!isSamePass)
            return GetResultWithError(UserStartCreateErrorCode.NotSamePassword, "Password confirmation do not match");

        var encryptedEmail = _cryptoHelper.Encrypt(input.Email);
        var emailAlreadyInUse = await _credentialRepository.Query().AnyAsync(c => c.EncryptedEmail == encryptedEmail);
        if (emailAlreadyInUse)
            return GetResultWithError(UserStartCreateErrorCode.EmailAlreadyInUse, "Email already in use");

        var creationToken = Guid.NewGuid().ToString();
        var encryptedPassword = _cryptoHelper.Encrypt(input.Password);
        var confirmationCode = _codeGenerator.Generate();
        var sentDateTime = _dateTimeProvider.UtcNow();

        var process = new UserCreateProcessDto
        {
            EncryptedEmail = encryptedEmail,
            EncryptedPassword = encryptedPassword,
            Token = creationToken,
            EmailConfirmationCode = confirmationCode,
            EmailSentDateTime = sentDateTime,
            ValidatedEmail = false
        };
        
        await _cache.SetAsync(GenerateProcessCacheKey(creationToken), process, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = sentDateTime.AddHours(12)
        });

        await _emailSender.SendEmailAsync(input.Email, "Fin - Email Confirmation", $"Your confirmation code is <b>{confirmationCode}</b>");
        
        return new ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>
        {
            Success = true,
            Message = "Success",
            Data = new UserStartCreateOutput
            {
                CreationToken = creationToken,
                Email = input.Email,
                SentEmailDateTime = sentDateTime,
            }
        };
    }
    
    public async Task<ValidationResultDto<DateTime>> ResendConfirmationEmail(string creationToken)
    {
        var process = await _cache.GetAsync<UserCreateProcessDto>(GenerateProcessCacheKey(creationToken));
        if (process == null) 
            throw new UnauthorizedAccessException("Not found process with this creationToken");
        
        var now = _dateTimeProvider.UtcNow();
        
        var timeSinceLastSend = now - process.EmailSentDateTime;
        if (timeSinceLastSend < TimeSpan.FromMinutes(1))
            return new ValidationResultDto<DateTime>
            {
                Data = process.EmailSentDateTime,
                Success = false,
                Message = "Must to wait 1 minute to resend confirmation email",
            };
        
        var email = _cryptoHelper.Decrypt(process.EncryptedEmail);
        var confirmationCode = _codeGenerator.Generate();

        process.EmailConfirmationCode = confirmationCode;
        process.EmailSentDateTime = now;
        process.ValidatedEmail = false;
        await _cache.SetAsync(GenerateProcessCacheKey(creationToken), process, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = now.AddMinutes(20)
        });
        await _emailSender.SendEmailAsync(email, "Fin - Email Confirmation", $"Your confirmation code is <b>{confirmationCode}</b>");
        
        return new ValidationResultDto<DateTime>
        {
            Success = true,
            Data = now,
            Message = "Sent confirmation email",
        };
    }

    public async Task<ValidationResultDto<bool>> ValidateEmailCode(string creationToken, string emailConfirmationCode)
    {
        var process = await _cache.GetAsync<UserCreateProcessDto>(GenerateProcessCacheKey(creationToken));
        if (process == null) 
            throw new UnauthorizedAccessException("Not found process with this creationToken");
        
        process.ValidEmail(emailConfirmationCode);
        await _cache.SetAsync(GenerateProcessCacheKey(creationToken), process, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = _dateTimeProvider.UtcNow().AddMinutes(20)
        });
        
        return new ValidationResultDto<bool>
        {
            Success = process.ValidatedEmail,
            Data = process.ValidatedEmail,
            Message = "Validated email code"
        };
    }

    public async Task<ValidationResultDto<UserDto>> CreateUser(string creationToken, UserUpdateOrCreateInput input)
    {
        var process = await _cache.GetAsync<UserCreateProcessDto>(GenerateProcessCacheKey(creationToken));
        if (process == null) 
            throw new UnauthorizedAccessException("Not found process with this creationToken");
        if (!process.ValidatedEmail)
            throw new UnauthorizedAccessException("Email not validated yet");

        var now = _dateTimeProvider.UtcNow();
        
        var user = new User(input, now);
        var credential = new UserCredential(user.Id, process.EncryptedEmail, process.EncryptedPassword);

        var tenant = new Tenant(now);
        user.Tenants.Add(tenant);
        
        var notificationSetting = new UserNotificationSettings(user.Id, tenant.Id);
        var rememberUseSetting = new UserRememberUseSetting(user.Id, tenant.Id);

        await _unitOfWork.BeginTransactionAsync();

        await _tenantRepository.AddAsync(tenant);
        await _userRepository.AddAsync(user);
        await _credentialRepository.AddAsync(credential);
        await _userRememberUseSettingRepository.AddAsync(rememberUseSetting);
        await _notificationSettingsRepository.AddAsync(notificationSetting);
        await _unitOfWork.CommitAsync();


        await _cache.RemoveAsync(GenerateProcessCacheKey(creationToken));
        
        user.Tenants.First().Users = null;
        user.Credential.User = null;
        
        return new ValidationResultDto<UserDto>
        {
            Success = true,
            Data = new UserDto(user),
            Message = "Created user"
        };
    }

    public async Task<ValidationResultDto<UserDto>> CreateUser(string googleId, string email, UserUpdateOrCreateInput input)
    {
        var encryptedEmail = _cryptoHelper.Encrypt(email);
        if (await _credentialRepository.Query().AnyAsync(c => c.EncryptedEmail == encryptedEmail))
            return new ValidationResultDto<UserDto>
            {
                Success = false,
                Message = "Email already in use"
            };
        
        var now = _dateTimeProvider.UtcNow();
        
        var user = new User(input, now);
        var credential = UserCredential.CreateWithGoogle(user.Id, encryptedEmail, googleId);

        var tenant = new Tenant(now);
        user.Tenants.Add(tenant);

        var notificationSetting = new UserNotificationSettings(user.Id, tenant.Id);
        var rememberUseSetting = new UserRememberUseSetting(user.Id, tenant.Id);

        await _unitOfWork.BeginTransactionAsync();
        await _tenantRepository.AddAsync(tenant);
        await _userRepository.AddAsync(user);
        await _credentialRepository.AddAsync(credential);
        await _userRememberUseSettingRepository.AddAsync(rememberUseSetting);
        await _notificationSettingsRepository.AddAsync(notificationSetting);
        await _unitOfWork.CommitAsync();


        user.Tenants.First().Users = null;
        user.Credential.User = null;
        return new ValidationResultDto<UserDto>
        {
            Success = true,
            Data = new UserDto(user),
            Message = "Created user"
        };
    }
    
    private string GenerateProcessCacheKey(string creationToken) => $"user-create-process-{creationToken}";

    private ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode> GetResultWithError(
        UserStartCreateErrorCode code, string message)
    {
        return new ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>
        {
            Success = false,
            ErrorCode = code,
            Message = message
        };
    }
}