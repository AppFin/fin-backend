using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
using Fin.Application.Dtos;
using Fin.Domain.Global;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.IRepositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Authentications.Services;

public interface IUserCreateService
{
    public Task<ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>> StartCreate(
        UserStartCreateInput input);
    
    public Task<ValidationResultDto<DateTime>> ResendConfirmationEmail(string creationToken);
    public Task<ValidationResultDto<bool>> ValidateEmailCode(string creationToken, string emailConfirmationCode);
    
    public Task<ValidationResultDto<UserOutput>> CreateUser(string creationToken, UserUpdateOrCreateDto input);
}

public class UserCreateService : IUserCreateService, IAutoTransient
{
    private readonly IRepository<UserCredential> _credentialRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRedisCacheService _cache;
    private readonly IEmailSenderService _emailSender;

    private readonly CryptoHelper _cryptoHelper;

    public UserCreateService(
        IRepository<UserCredential> credentialRepository,
        IRepository<Tenant> tenantRepository,
        IRepository<User> userRepository,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        IRedisCacheService cache,
        IEmailSenderService emailSender
        )
    {
        _credentialRepository = credentialRepository;
        _dateTimeProvider = dateTimeProvider;
        _cache = cache;
        _emailSender = emailSender;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;

        var encryptKey = configuration.GetSection("ApiSettings:Authentication:Encrypt:Key").Value ?? "";
        var encryptIv = configuration.GetSection("ApiSettings:Authentication:Encrypt:Iv").Value ?? "";

        _cryptoHelper = new CryptoHelper(encryptKey, encryptIv);
    }

    public async Task<ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>> StartCreate(
        UserStartCreateInput input)
    {
        var isValidPass = UserCredential.IsValidPassword(input.Password);
        var isSamePass = input.Password == input.PasswordConfirmation;

        if (!isValidPass)
            return GetResultWithError(UserStartCreateErrorCode.InvalidPassword,
                "Password do not match requirements");
        if (!isSamePass)
            return GetResultWithError(UserStartCreateErrorCode.NotSamePassword,
                "Password confirmation do not match");

        var encryptedEmail = _cryptoHelper.Encrypt(input.Email);
        var emailAlreadyInUse = await _credentialRepository.Query().AnyAsync(c => c.EncryptedEmail == encryptedEmail);
        if (emailAlreadyInUse)
            return GetResultWithError(UserStartCreateErrorCode.EmailAlreadyInUse, "Email already in use");

        var creationToken = Guid.NewGuid().ToString();
        var encryptedPassword = _cryptoHelper.Encrypt(input.Password);
        var confirmationCode = GenerateConfirmationCode();
        var sentDateTime = _dateTimeProvider.UtcNow();

        var process = new UserCreateProcess
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
            AbsoluteExpiration = sentDateTime.AddMinutes(20)
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
                SentEmaiDateTime = sentDateTime,
            }
        };
    }
    
    public async Task<ValidationResultDto<DateTime>> ResendConfirmationEmail(string creationToken)
    {
        var process = await _cache.GetAsync<UserCreateProcess>(GenerateProcessCacheKey(creationToken));
        if (process == null) 
            throw new UnauthorizedAccessException("Not found process with this creationToken");
        
        var timeSinceLastSend = _dateTimeProvider.UtcNow() - process.EmailSentDateTime;
        if (timeSinceLastSend < TimeSpan.FromMinutes(1))
            return new ValidationResultDto<DateTime>
            {
                Data = process.EmailSentDateTime,
                Success = false,
                Message = "Must to wait 1 minute to resend confirmation email",
            };
        
        var email = _cryptoHelper.Decrypt(process.EncryptedEmail);
        var confirmationCode = GenerateConfirmationCode();
        var sentDateTime = _dateTimeProvider.UtcNow();
        
        process.EmailConfirmationCode = confirmationCode;
        process.EmailSentDateTime = sentDateTime;
        process.ValidatedEmail = false;
        await _cache.SetAsync(GenerateProcessCacheKey(creationToken), process, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = sentDateTime.AddMinutes(20)
        });
        await _emailSender.SendEmailAsync(email, "Fin - Email Confirmation", $"Your confirmation code is <b>{confirmationCode}</b>");
        
        return new ValidationResultDto<DateTime>
        {
            Success = true,
            Data = sentDateTime,
            Message = "Sent confirmation email",
        };
    }

    public async Task<ValidationResultDto<bool>> ValidateEmailCode(string creationToken, string emailConfirmationCode)
    {
        var process = await _cache.GetAsync<UserCreateProcess>(GenerateProcessCacheKey(creationToken));
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

    public async Task<ValidationResultDto<UserOutput>> CreateUser(string creationToken, UserUpdateOrCreateDto input)
    {
        var process = await _cache.GetAsync<UserCreateProcess>(GenerateProcessCacheKey(creationToken));
        if (process == null) 
            throw new UnauthorizedAccessException("Not found process with this creationToken");
        if (!process.ValidatedEmail)
            throw new UnauthorizedAccessException("Email not validated yet");

        var now = _dateTimeProvider.UtcNow();
        
        var user = new User(input, now);
        var credential = new UserCredential(user.Id, process.EncryptedEmail, process.EncryptedPassword);

        var tenant = new Tenant(now, "pt-Br", "America/Sao_Paulo");
        user.Tenants.Add(tenant);
        
        await _tenantRepository.AddAsync(tenant);
        await _userRepository.AddAsync(user);
        await _credentialRepository.AddAsync(credential);
        
        await _credentialRepository.SaveChangesAsync();
        
        await _cache.RemoveAsync(GenerateProcessCacheKey(creationToken));
        
        return new ValidationResultDto<UserOutput>
        {
            Success = true,
            Data = new UserOutput(user),
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

    private static string GenerateConfirmationCode()
    {
        var random = new Random();
        var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var length = 6;

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = characters[random.Next(characters.Length)];
        }

        return new string(result);
    }
}