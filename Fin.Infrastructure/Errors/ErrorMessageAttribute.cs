using System.Reflection;

namespace Fin.Infrastructure.Errors;

[AttributeUsage(AttributeTargets.Field)]
public class ErrorMessageAttribute(string message) : Attribute
{
    public string Message { get; } = message;
}

public static class ErrorMessageExtension
{
    public static string GetErrorMessage<TEnum>(this TEnum enumValue, bool throwIfNotFoundMessage = true) where TEnum : Enum
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString());
        
        if (memberInfo.Length > 0)
        {
            var attribute = memberInfo[0].GetCustomAttribute<ErrorMessageAttribute>();
            if (attribute != null)
            {
                return attribute.Message;
            }
        }

        return throwIfNotFoundMessage
            ? throw new ArgumentException($"Error message not found for {enumValue}")
            : string.Empty;
    }

}