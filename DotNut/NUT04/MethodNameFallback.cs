using System.Globalization;

namespace DotNut;

public static class MethodNameFallback
{
    /// <summary>
    /// The method identifier a mint may use. NUT-04 restricts it to lowercase ASCII
    /// alphanumerics, hyphens and underscores, and it must be non-empty.
    /// </summary>
    public static bool IsValidMethod(string? method) =>
        !string.IsNullOrEmpty(method)
        && method.All(c =>
            c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'
        );

    /// <summary>
    /// The name to show for a payment method. Uses what the mint sent, and otherwise derives one
    /// from the method identifier by turning <c>_</c> and <c>-</c> into spaces and title-casing
    /// each word, as NUT-04 prescribes: <c>bolt11</c> becomes <c>Bolt11</c>, <c>apple-pay</c>
    /// becomes <c>Apple Pay</c>.
    /// </summary>
    public static string DisplayNameFor(string method, string? methodName)
    {
        if (!string.IsNullOrEmpty(methodName))
        {
            return methodName;
        }

        var words = method.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(
            ' ',
            words.Select(word =>
                CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant())
            )
        );
    }
}
