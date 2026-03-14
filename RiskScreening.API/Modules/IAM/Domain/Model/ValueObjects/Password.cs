using System.Text.RegularExpressions;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Represents a hashed password. Never stores plain text.
///     Use <see cref="FromPlainText"/> to validate and create from a raw password.
/// </summary>
public partial record Password : ValueObject
{
    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
    
    private static readonly Regex StrongPassword = MyRegex();

    /// <summary>BCrypt hash — never plain text.</summary>
    public string Hash { get; }

    /// <summary>Creates a Password from an already-hashed value (e.g., loaded from DB).</summary>
    private Password(string hash) => Hash = hash;

    /// <summary>
    ///     Validates the plain text password strength, then stores only the hash.
    ///     Call this only when creating or changing a password.
    /// </summary>
    public static Password FromPlainText(string plainText, Func<string, string> hasher)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new InvalidValueException("Password", null, "must not be blank",
                ErrorCodes.InvalidPassword, ErrorCodes.InvalidPasswordCode);

        if (!StrongPassword.IsMatch(plainText))
            throw new InvalidValueException("Password", null,
                "must be at least 8 characters and include uppercase, lowercase and a digit",
                ErrorCodes.InvalidPassword, ErrorCodes.InvalidPasswordCode); // 1103

        return new Password(hasher(plainText));
    }

    /// <summary>Creates a Password directly from a stored hash (no validation).</summary>
    public static Password FromHash(string hash) => new(hash);

    public override string ToString() => "***";
}
