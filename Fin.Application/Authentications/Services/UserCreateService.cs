using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
using Fin.Application.Dtos;
using Fin.Domain.Global;
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
    public Task<ValidationResultDto<DateTime, UserCreateResendConfirmationErrorCode>> ResendConfirmationEmail(string creationToken);
}

public class UserCreateService : IUserCreateService, IAutoTransient
{
    private readonly IRepository<UserCredential> _credentialRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRedisCacheService _cache;
    private readonly IEmailSenderService _emailSender;

    private readonly CryptoHelper _cryptoHelper;

    public UserCreateService(
        IRepository<UserCredential> credentialRepository,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        IRedisCacheService cache,
        IEmailSenderService emailSender)
    {
        _credentialRepository = credentialRepository;
        _dateTimeProvider = dateTimeProvider;
        _cache = cache;
        _emailSender = emailSender;

        var encryptKey = configuration["AppSettings:Authentication:Encrypt:Key"] ?? "";
        var encryptIv = configuration["AppSettings:Authentication:Encrypt:Iv"] ?? "";

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
        var startDateTime = _dateTimeProvider.UtcNow();

        var process = new UserCreateProcess
        {
            EncryptedEmail = encryptedEmail,
            EncryptedPassword = encryptedPassword,
            Token = creationToken,
            EmailConfirmationCode = confirmationCode,
            StarDateTime = startDateTime,
        };
        
        await _cache.SetAsync(creationToken, process, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = startDateTime.AddMinutes(20)
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
            }
        };
    }

    public async Task<ValidationResultDto<DateTime, UserCreateResendConfirmationErrorCode>> ResendConfirmationEmail(string creationToken)
    {
        var process = await _cache.GetAsync<UserCreateProcess>(creationToken);
        if (process == null) return new ValidationResultDto<DateTime, UserCreateResendConfirmationErrorCode>
        {
            Success = false,
            Message = "Not found create process",
            ErrorCode = UserCreateResendConfirmationErrorCode.NotFoundProcess
        };
        
        var timeSinceLastSend = _dateTimeProvider.UtcNow() - process.StarDateTime;
        if (timeSinceLastSend < TimeSpan.FromMinutes(1))
            return new ValidationResultDto<DateTime, UserCreateResendConfirmationErrorCode>
            {
                Data = process.StarDateTime,
                Success = false,
                Message = "Must to wait 1 minute to resend confirmation email",
                ErrorCode = UserCreateResendConfirmationErrorCode.LimitedTime
            };
        
        var email = _cryptoHelper.Decrypt(process.EncryptedEmail);
        var confirmationCode = GenerateConfirmationCode();
        var startDateTime = _dateTimeProvider.UtcNow();
        
        process.EmailConfirmationCode = confirmationCode;
        process.StarDateTime = startDateTime;
        await _cache.SetAsync(creationToken, process, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = startDateTime.AddMinutes(20)
        });
        await _emailSender.SendEmailAsync(email, "Fin - Email Confirmation", $"Your confirmation code is <b>{confirmationCode}</b>");
        
        return new ValidationResultDto<DateTime, UserCreateResendConfirmationErrorCode>
        {
            Success = true,
            Data = startDateTime,
            Message = "Sent confirmation email",
        };
    }
    
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