using Vigie.Application.Auth;

namespace Vigie.Domain.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verifies_the_original_password_without_storing_it()
    {
        const string password = "Une phrase secrète solide!";
        var encoded = PasswordHasher.Hash(password);

        Assert.True(PasswordHasher.Verify(password, encoded));
        Assert.False(PasswordHasher.Verify("mauvais mot de passe", encoded));
        Assert.DoesNotContain(password, encoded, StringComparison.Ordinal);
    }

    [Fact]
    public void Uses_a_unique_salt_for_each_hash()
    {
        var first = PasswordHasher.Hash("vigie-demo");
        var second = PasswordHasher.Hash("vigie-demo");

        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("vigie-demo", first));
        Assert.True(PasswordHasher.Verify("vigie-demo", second));
    }
}
