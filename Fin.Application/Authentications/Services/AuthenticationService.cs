using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
using Fin.Application.Authentications.Utils;
using Fin.Application.Emails;
using Fin.Application.Globals.Dtos;
using Fin.Application.Users.Services;
using Fin.Domain.Global;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Enums;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Constants;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Authentications.Services;

public interface IAuthenticationService
{
    public Task SendResetPasswordEmail(SendResetPasswordEmailInput input);
    public Task<ValidationResultDto<bool, ResetPasswordErrorCode>> ResetPassword(ResetPasswordInput input);

    public Task<LoginOutput> Login(LoginInput input);
    public Task<LoginWithGoogleOutput> LoginOrSingInWithGoogle(LoginWithGoogleInput input);
    public Task<LoginOutput> RefreshToken(string refreshToken);
    public Task Logout(string token);
}

public class AuthenticationService : IAuthenticationService, IAutoTransient
{
    private readonly IRepository<UserCredential> _credentialRepository;
    private readonly IRedisCacheService _cache;
    private readonly IEmailSenderService _emailSender;
    private readonly IUserCreateService _userCreateService;
    private readonly IAuthenticationTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IEmailTemplateService _emailTemplateService;

    private readonly CryptoHelper _cryptoHelper;

    public AuthenticationService(
        IRepository<UserCredential> credentialRepository,
        IRedisCacheService cache,
        IEmailSenderService emailSender,
        IConfiguration configuration,
        IAuthenticationTokenService tokenService,
        IUserCreateService userCreateService,
        IEmailTemplateService emailTemplateService)
    {
        _credentialRepository = credentialRepository;
        _cache = cache;
        _emailSender = emailSender;
        _configuration = configuration;
        _tokenService = tokenService;
        _userCreateService = userCreateService;
        _emailTemplateService = emailTemplateService;

        var encryptKey = configuration.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = configuration.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ?? "";

        _cryptoHelper = new CryptoHelper(encryptKey, encryptIv);
    }

    public async Task SendResetPasswordEmail(SendResetPasswordEmailInput input)
    {
        var encryptedEmail = _cryptoHelper.Encrypt(input.Email);
        var credential = _credentialRepository
            .Include(c => c.User)
            .FirstOrDefault(c => c.EncryptedEmail == encryptedEmail);

        if (credential == null || !credential.User.IsActivity)
            return;

        var token = $"{Guid.NewGuid()}-{Guid.NewGuid()}";
        var tokenLifeTimeInHours = 6;

        credential.ResetToken = token;
        await _cache.SetAsync(GetResetTokenCacheKey(token), credential.UserId, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(tokenLifeTimeInHours)
        });
        await _credentialRepository.UpdateAsync(credential, true);

        var frontUrl = _configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
        var logoIconUrl = $"{frontUrl}/icons/fin.png";
        var resetLink = $"{frontUrl}/authentication/reset-password?token={token}";

        var parameters = new Dictionary<string, string>();
        parameters.Add("appName", AppConstants.AppName);
        parameters.Add("linkLifeTime", tokenLifeTimeInHours.ToString());
        parameters.Add("resetLink", resetLink);
        parameters.Add("logoIconUrl", logoIconUrl);

        var subject = _emailTemplateService.Get("ResetPassword_Subject", parameters);
        var plainBody = _emailTemplateService.Get("ResetPassword_Plain", parameters);
        var htmlBody = _emailTemplateService.Get("ResetPassword_HTML", parameters);

        await _emailSender.SendEmailAsync(new SendEmailDto
        {
            Subject = subject,
            ToEmail = input.Email,
            PlainBody =  plainBody,
            HtmlBody = htmlBody,
            ToName = credential.User.DisplayName
        });
    }

    public async Task<ValidationResultDto<bool, ResetPasswordErrorCode>> ResetPassword(ResetPasswordInput input)
    {
        var isValidPass = UserCredential.IsValidPassword(input.Password);
        var isSamePass = input.Password == input.PasswordConfirmation;

        if (!isValidPass)
            return new ValidationResultDto<bool, ResetPasswordErrorCode>
            {
                Message = "Invalid password",
                ErrorCode = ResetPasswordErrorCode.InvalidPassword
            };

        if (!isSamePass)
            return new ValidationResultDto<bool, ResetPasswordErrorCode>
            {
                Message = "Not same password",
                ErrorCode = ResetPasswordErrorCode.NotSamePassword
            };

        var credential = await _credentialRepository.FirstOrDefaultAsync(c => c.ResetToken == input.ResetToken);
        if (credential == null)
            return new ValidationResultDto<bool, ResetPasswordErrorCode>
            {
                Message = "Invalid reset token",
                ErrorCode = ResetPasswordErrorCode.InvalidToken
            };

        var userId = await _cache.GetAsync<Guid>(GetResetTokenCacheKey(input.ResetToken));
        if (userId == Guid.Empty || userId != credential.UserId)
            return new ValidationResultDto<bool, ResetPasswordErrorCode>
            {
                Message = userId == Guid.Empty ? "Reset token has expired" : "Invalid reset token",
                ErrorCode = userId == Guid.Empty
                    ? ResetPasswordErrorCode.ExpiredToken
                    : ResetPasswordErrorCode.InvalidToken
            };

        var encryptedPassword = _cryptoHelper.Encrypt(input.Password);
        credential.ResetPassword(encryptedPassword, input.ResetToken);
        await _credentialRepository.UpdateAsync(credential, true);

        return new ValidationResultDto<bool, ResetPasswordErrorCode>
        {
            Success = true,
            Data = true,
            Message = "Success"
        };
    }

    public Task<LoginOutput> Login(LoginInput input)
    {
        return _tokenService.Login(input);
    }

    public async Task<LoginWithGoogleOutput> LoginOrSingInWithGoogle(LoginWithGoogleInput input)
    {
        var encryptedEmail = _cryptoHelper.Encrypt(input.Email);

        var credential = _credentialRepository
            .Include(c => c.User)
            .ThenInclude(c => c.Tenants)
            .FirstOrDefault(c => c.EncryptedEmail == encryptedEmail);

        UserDto user;
        var mustToCreateUser = false;
        if (credential != null)
        {
            user = new UserDto(credential.User);
            if (!user.IsActivity)
                return new LoginWithGoogleOutput { ErrorCode = LoginErrorCode.InactivatedUser };

            if (!credential.HasGoogle)
            {
                credential.GoogleId = input.GoogleId;
                await _credentialRepository.UpdateAsync(credential, true);
            }
            else if (credential.GoogleId != input.GoogleId)
                return new LoginWithGoogleOutput { ErrorCode = LoginErrorCode.DifferentGoogleAccountLinked };
        }
        else
        {
            mustToCreateUser = true;

            var createResult = await _userCreateService.CreateUser(input.GoogleId, input.Email,
                new UserUpdateOrCreateInput
                {
                    DisplayName = input.DisplayName,
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    ImagePublicUrl = input.PictureUrl
                });

            if (!createResult.Success || createResult.Data == null)
                return new LoginWithGoogleOutput { ErrorCode = LoginErrorCode.CantCreateUser };

            user = createResult.Data;
        }

        var result = await _tokenService.GenerateTokenAsync(user);

        return new LoginWithGoogleOutput
        {
            Success = result.Success,
            Token = result.Token,
            RefreshToken = result.RefreshToken,
            ErrorCode = result.ErrorCode,
            MustToCreateUser = result.Success && mustToCreateUser,
        };
    }

    public Task<LoginOutput> RefreshToken(string refreshToken)
    {
        return _tokenService.RefreshToken(refreshToken);
    }

    public Task Logout(string token)
    {
        return _tokenService.Logout(token);
    }

    private static string GetResetTokenCacheKey(string token) => $"reset-token-{token}";
}