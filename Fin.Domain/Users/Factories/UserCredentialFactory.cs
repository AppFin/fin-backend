using Fin.Domain.Users.Entities;

namespace Fin.Domain.Users.Factories;

public enum UserCredentialFactoryType
{
    Password = 0,
    Google = 1
}

public static class UserCredentialFactory
{
    public static UserCredential Create(Guid userId, string encryptedEmail, string value, UserCredentialFactoryType type)
    {
        var credential = new UserCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedEmail = encryptedEmail
        };

        switch (type)
        {
            case UserCredentialFactoryType.Password:
                credential.EncryptedPassword = value;
                break;
            case UserCredentialFactoryType.Google:
                credential.GoogleId = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        
        return credential;
    }
}