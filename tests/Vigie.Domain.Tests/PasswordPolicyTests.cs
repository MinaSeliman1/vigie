using Vigie.Application.Auth;

namespace Vigie.Domain.Tests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("court1A", false)]
    [InlineData("motdepasseassezlong", false)]
    [InlineData("Motdepasseassezlong", false)]
    [InlineData("Motdepasse123", true)]
    public void Strong_password_policy_is_explicit(string password, bool expected)
        => Assert.Equal(expected, PasswordPolicy.IsStrong(password));
}
