using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vigie.Domain;

namespace Vigie.Api.Auth;

public interface ITransactionalEmailSender
{
    Task<bool> SendPasswordResetAsync(Employee employee, string resetLink, CancellationToken cancellationToken);
}

public sealed class ResendTransactionalEmailSender(HttpClient httpClient, IConfiguration configuration) : ITransactionalEmailSender
{
    private readonly string? apiKey = configuration["Resend:ApiKey"];
    private readonly string from = configuration["Resend:From"] ?? "Vigie <noreply@vigie.app>";

    public async Task<bool> SendPasswordResetAsync(Employee employee, string resetLink, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            from,
            to = new[] { employee.Email },
            subject = "Réinitialiser votre mot de passe Vigie",
            html = $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(employee.Name)},</p><p>Utilisez ce lien pour choisir un nouveau mot de passe Vigie. Il expire dans 30 minutes et ne peut servir qu'une seule fois.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(resetLink)}\">Réinitialiser mon mot de passe</a></p><p>Si vous n'êtes pas à l'origine de cette demande, vous pouvez ignorer ce courriel.</p>"
        });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return true;
        return false;
    }
}
