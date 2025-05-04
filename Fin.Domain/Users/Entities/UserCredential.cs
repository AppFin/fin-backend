using System.Text.RegularExpressions;
using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.Users.Entities;

public class UserCredential : IEntity
{
    public Guid Id { get; set; }

    public string EncryptedEmail { get; private set; }
    public string EncryptedPassword { get; private  set; }

    public string GoogleId { get; set; }

    public string ResetToken { get; private set; } = "";
    private int FailLoginAttempts { get;  set; }
    
    public Guid UserId { get; private  set; }
    public virtual User User { get; set; }
    
    public bool HasGoogle => !string.IsNullOrEmpty(GoogleId);
    public bool ExceededAttempts => FailLoginAttempts > 5;
    
    public UserCredential()
    {
    }

    public UserCredential(Guid userId, string encryptedEmail)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EncryptedEmail = encryptedEmail;
    }
    
    public UserCredential(Guid userId, string encryptedEmail, string encryptedPassword) 
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EncryptedEmail = encryptedEmail;
        EncryptedPassword = encryptedPassword;
    }
    
    public bool ResetPassword(string newPasswordEncrypted, string resetToken)
    {
        if (resetToken != ResetToken) return false;

        EncryptedPassword = newPasswordEncrypted;
        ResetFailLoginAttempts();
        
        ResetToken = ""; 
        
        if (!User.IsActivity)
            User.ToggleActivity();
        
        return true;
    }

    public bool TestCredentials(string emailOrPhoneEncrypted, string passwordEncrypted)
    {
        if (ExceededAttempts || !User.IsActivity)
            return false;
        
        var isValid =  EncryptedEmail == emailOrPhoneEncrypted && EncryptedPassword == passwordEncrypted;

        if (isValid)
            ResetFailLoginAttempts();
        else
            IncrementFailLoginAttempts();
        
        return isValid;
    }

    public void ResetFailLoginAttempts()
    {
        FailLoginAttempts = 0;   
    }
    
    public void IncrementFailLoginAttempts()
    {
        FailLoginAttempts++;
    }
    
    public static bool IsValidPassword(string password)
    {
        if (password.Length < 5)
            return false;

        var temMinuscula = Regex.IsMatch(password, "[a-z]");
        var temMaiuscula = Regex.IsMatch(password, "[A-Z]");
        var temNumero = Regex.IsMatch(password, "[0-9]");
        var temEspecial = Regex.IsMatch(password, "[^a-zA-Z0-9]");

        return temMinuscula && temMaiuscula && temNumero && temEspecial;
    }
}