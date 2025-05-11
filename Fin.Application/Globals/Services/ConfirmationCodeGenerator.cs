using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Application.Globals.Services;

public interface IConfirmationCodeGenerator
{
    public string Generate();
}


public class ConfirmationCodeGenerator(IRandomGenerator random) : IConfirmationCodeGenerator, IAutoTransient
{
    public string Generate()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const int length = 6;

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = characters[random.Next(characters.Length)];
        }

        return new string(result);
    }
}