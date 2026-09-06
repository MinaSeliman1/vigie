using System.Security.Cryptography;
using System.Text;

namespace Vigie.Application.Auth;

public static class InvitationToken
{
    public static (string Token, string Hash) Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (token, Hash(token));
    }

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();
}
