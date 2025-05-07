using Fin.Infrastructure.Authentications.Dtos;

namespace Fin.Application.Authentications.Dtos;

public class LoginWithGoogleOutput: LoginOutput
{
    public bool MustToCreateUser { get; set; } = false;
}