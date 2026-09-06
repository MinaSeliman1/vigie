namespace Vigie.Application.Auth;

public static class PasswordPolicy
{
    public static bool IsStrong(string? password)
        => !string.IsNullOrWhiteSpace(password) && password.Length >= 12 &&
           password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
}
