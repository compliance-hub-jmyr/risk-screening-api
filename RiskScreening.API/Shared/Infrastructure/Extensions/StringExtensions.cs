using System.Text.RegularExpressions;

namespace RiskScreening.API.Shared.Infrastructure.Extensions;

/// <summary>
///     Provides string extension methods for naming convention conversions
///     used across persistence and HTTP routing configurations.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    ///     Converts a PascalCase or camelCase string to snake_case.
    ///     Used for EF Core column and table naming conventions.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>The string in snake_case format.</returns>
    /// <example>
    ///     <code>
    ///         "CreatedAt".ToSnakeCase()   // → "created_at"
    ///         "RiskScore".ToSnakeCase()   // → "risk_score"
    ///     </code>
    /// </example>
    public static string ToSnakeCase(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return new string(Convert(text.GetEnumerator()).ToArray());

        static IEnumerable<char> Convert(CharEnumerator e)
        {
            if (!e.MoveNext()) yield break;
            yield return char.ToLower(e.Current);
            while (e.MoveNext())
                if (char.IsUpper(e.Current))
                {
                    yield return '_';
                    yield return char.ToLower(e.Current);
                }
                else
                {
                    yield return e.Current;
                }
        }
    }

    /// <summary>
    ///     Converts a PascalCase or camelCase string to a kebab-case.
    ///     Used for HTTP route naming conventions in controllers.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>The string in kebab-case format.</returns>
    /// <example>
    ///     <code>
    ///         "RiskScreening".ToKebabCase()   // → "risk-screening"
    ///         "SupplierProfile".ToKebabCase() // → "supplier-profile"
    ///     </code>
    /// </example>
    public static string ToKebabCase(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return KebabCaseRegex().Replace(text, "-$1").Trim().ToLower();
    }

    /// <summary>
    ///     Converts an English noun to its plural form.
    ///     Handles the most common English pluralization rules.
    ///     Used for EF Core table naming conventions.
    /// </summary>
    /// <param name="text">The string to pluralize.</param>
    /// <returns>The pluralized string.</returns>
    /// <example>
    ///     <code>
    ///         "Supplier".ToPlural()   // → "Suppliers"
    ///         "Category".ToPlural()   // → "Categories"
    ///         "Address".ToPlural()    // → "Addresses"
    ///     </code>
    /// </example>
    public static string ToPlural(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        if (text.EndsWith("ch") || text.EndsWith("sh") ||
            text.EndsWith("x") || text.EndsWith("z") || text.EndsWith("s"))
            return text + "es";

        if (text.EndsWith("y") && text.Length > 1 && !"aeiou".Contains(text[^2]))
            return text[..^1] + "ies";

        return text + "s";
    }

    /// <summary>
    ///     Compiled regex that matches uppercase transitions for kebab-case conversion.
    /// </summary>
    [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", RegexOptions.Compiled)]
    private static partial Regex KebabCaseRegex();
}
