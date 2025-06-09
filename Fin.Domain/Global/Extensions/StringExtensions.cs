using System.Globalization;
using System.Text;

namespace Fin.Domain.Global.Extensions;

public static class StringExtensions
{
    public static string NormalizeForComparison(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        text = text.ToLowerInvariant();

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}