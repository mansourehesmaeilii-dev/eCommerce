using System.Text;
using System.Text.RegularExpressions;

namespace eCommerce.Server.Application.Helpers;

public static partial class SlugHelper
{
    public static string Generate(string text)
    {
        var slug = text.ToLowerInvariant();
        slug = slug.Normalize(NormalizationForm.FormD);
        slug = NonAsciiRegex().Replace(slug, "");
        slug = slug.Normalize(NormalizationForm.FormC);
        slug = NonWordRegex().Replace(slug, "-");
        slug = MultipleDashRegex().Replace(slug, "-");
        return slug.Trim('-');
    }

    [GeneratedRegex(@"[^\u0000-\u007F]")]
    private static partial Regex NonAsciiRegex();

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex NonWordRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleDashRegex();
}
