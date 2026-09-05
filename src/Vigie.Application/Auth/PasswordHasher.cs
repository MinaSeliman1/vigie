using System.Security.Cryptography;

namespace Vigie.Application.Auth;

/// <summary>
/// Hachage local des mots de passe applicatifs. Le format conserve la version,
/// le coût, le sel et le résultat pour permettre une évolution contrôlée.
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "v1$pbkdf2-sha512";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations);
        return string.Join('$', Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encodedHash)) return false;
        var parts = encodedHash.Split('$');
        if (parts.Length != 5 || $"{parts[0]}${parts[1]}" != Prefix ||
            !int.TryParse(parts[2], out var iterations) || iterations < 100_000) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, HashSize);
}
