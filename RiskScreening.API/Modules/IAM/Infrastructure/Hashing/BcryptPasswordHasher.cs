using RiskScreening.API.Modules.IAM.Application.Ports;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Hashing;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainText)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainText);
    }

    public bool Verify(string plainText, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(plainText, hash);
    }
}