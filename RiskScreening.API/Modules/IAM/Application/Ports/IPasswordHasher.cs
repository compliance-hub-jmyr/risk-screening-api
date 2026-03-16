namespace RiskScreening.API.Modules.IAM.Application.Ports;

/// <summary>
///     Port for password hashing and verification.
///     Implemented in Infrastructure using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    string Hash(string plainText);

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash.
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="hash"></param>
    /// <returns></returns>
    bool Verify(string plainText, string hash);
}