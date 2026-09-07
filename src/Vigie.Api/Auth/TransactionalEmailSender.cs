using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vigie.Domain;

namespace Vigie.Api.Auth;

public interface ITransactionalEmailSender
{
    Task<bool> SendPasswordResetAsync(Employee employee, string resetLink, CancellationToken cancellationToken);
    Task<bool> SendInvitationAsync(string email, string name, string role, string invitationLink, CancellationToken cancellationToken);
    Task<bool> SendAssignmentAsync(Employee employee, Site site, Shift shift, CancellationToken cancellationToken);
    Task<bool> SendSwapDecisionAsync(Employee employee, Site site, Shift shift, string title, string body, CancellationToken cancellationToken);
}

public sealed class ResendTransactionalEmailSender(HttpClient httpClient, IConfiguration configuration) : ITransactionalEmailSender
{
    private readonly string? apiKey = configuration["Resend:ApiKey"];
    private readonly string from = configuration["Resend:From"] ?? "Vigie <noreply@vigie.app>";

    public async Task<bool> SendPasswordResetAsync(Employee employee, string resetLink, CancellationToken cancellationToken)
    {
        return await SendAsync(employee.Email, "Réinitialiser votre mot de passe Vigie", $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(employee.Name)},</p><p>Utilisez ce lien pour choisir un nouveau mot de passe Vigie. Il expire dans 30 minutes et ne peut servir qu'une seule fois.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(resetLink)}\">Réinitialiser mon mot de passe</a></p><p>Si vous n'êtes pas à l'origine de cette demande, vous pouvez ignorer ce courriel.</p>", cancellationToken);
    }

    public Task<bool> SendInvitationAsync(string email, string name, string role, string invitationLink, CancellationToken cancellationToken)
        => SendAsync(email, "Vous êtes invité à rejoindre Vigie", $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(name)},</p><p>Vous avez été invité à rejoindre une équipe Vigie comme <strong>{System.Net.WebUtility.HtmlEncode(role)}</strong>.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(invitationLink)}\">Activer mon accès Vigie</a></p><p>Le lien expire dans 7 jours et ne peut servir qu'une seule fois.</p>", cancellationToken);

    public Task<bool> SendAssignmentAsync(Employee employee, Site site, Shift shift, CancellationToken cancellationToken)
        => SendAsync(employee.Email, "Un nouveau quart vous est assigné dans Vigie", $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(employee.Name)},</p><p>Un quart vous a été assigné à <strong>{System.Net.WebUtility.HtmlEncode(site.Name)}</strong> le <strong>{FormatShiftDate(shift)}</strong>.</p><p>Consultez votre calendrier Vigie pour les détails et les demandes de remplacement.</p>", cancellationToken);

    public Task<bool> SendSwapDecisionAsync(Employee employee, Site site, Shift shift, string title, string body, CancellationToken cancellationToken)
        => SendAsync(employee.Email, $"{title} — Vigie", $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(employee.Name)},</p><p>{System.Net.WebUtility.HtmlEncode(body)}</p><p>Quart concerné : <strong>{System.Net.WebUtility.HtmlEncode(site.Name)}</strong>, le <strong>{FormatShiftDate(shift)}</strong>.</p><p>Consultez votre calendrier Vigie pour les détails.</p>", cancellationToken);

    private static string FormatShiftDate(Shift shift)
        => shift.StartUtc.ToString("dddd d MMMM 'à' HH:mm", CultureInfo.GetCultureInfo("fr-CA"));

    private async Task<bool> SendAsync(string recipient, string subject, string html, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return false;
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { from, to = new[] { recipient }, subject, html });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
