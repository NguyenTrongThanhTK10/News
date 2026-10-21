using System.Globalization;
using System.Text;

namespace News.Domain;

public static class SearchNormalizer
{
    public static string Normalize(string value) => new string(value.ToLowerInvariant().Replace('đ', 'd')
        .Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
        .ToArray()).Normalize(NormalizationForm.FormC);
}
