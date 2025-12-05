using System.Reflection;

namespace Fin.Domain.Global.Decorators;

[AttributeUsage(AttributeTargets.Field)]
public class FrontTranslateKeyAttribute(string translateKey): Attribute
{
    public string TranslateKey { get; } = translateKey; 
}


public static class FrontTranslateKeyExtension
{
    public static string GetTranslateKey<T>(this T valeu, bool throwIfNotFoundMessage = true)
    {
        var type = valeu.GetType();
        var memberInfo = type.GetMember(valeu.ToString() ?? string.Empty);

        if (memberInfo.Length > 0)
        {
            var attribute = memberInfo[0].GetCustomAttribute<FrontTranslateKeyAttribute>();
            if (attribute != null)
            {
                return attribute.TranslateKey;
            }
        }
        
        return throwIfNotFoundMessage 
            ? throw new ArgumentException($"Cannot get translate key {valeu}")
            : string.Empty;
    }
}