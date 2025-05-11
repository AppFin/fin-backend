using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Application.Globals.Services;

public interface IRandomGenerator
{
    int Next(int maxValue);
}

public class RandomGenerator : IRandomGenerator, IAutoTransient
{
    private readonly Random _random = new();

    public int Next(int maxValue)
    {
        return _random.Next(maxValue);
    }
}