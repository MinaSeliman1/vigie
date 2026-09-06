using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Vigie.Api.Infrastructure;

/// <summary>
/// Compteurs HTTP en mémoire, exposés au format Prometheus sans donnée personnelle.
/// </summary>
public sealed class RequestMetrics
{
    private readonly ConcurrentDictionary<int, long> responsesByStatus = new();
    private long totalRequests;
    private long failedRequests;
    private long durationSumMilliseconds;

    public void Record(int statusCode, long durationMilliseconds)
    {
        Interlocked.Increment(ref totalRequests);
        if (statusCode >= 500) Interlocked.Increment(ref failedRequests);
        Interlocked.Add(ref durationSumMilliseconds, durationMilliseconds);
        responsesByStatus.AddOrUpdate(statusCode, 1, (_, count) => count + 1);
    }

    public string ToPrometheus()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# HELP vigie_http_requests_total Nombre total de requêtes HTTP.");
        builder.AppendLine("# TYPE vigie_http_requests_total counter");
        builder.Append("vigie_http_requests_total ").AppendLine(Volatile.Read(ref totalRequests).ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("# HELP vigie_http_requests_failed_total Nombre de réponses HTTP en erreur serveur.");
        builder.AppendLine("# TYPE vigie_http_requests_failed_total counter");
        builder.Append("vigie_http_requests_failed_total ").AppendLine(Volatile.Read(ref failedRequests).ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("# HELP vigie_http_request_duration_ms Somme des durées de requêtes HTTP en millisecondes.");
        builder.AppendLine("# TYPE vigie_http_request_duration_ms summary");
        builder.Append("vigie_http_request_duration_ms_sum ").AppendLine(Volatile.Read(ref durationSumMilliseconds).ToString(CultureInfo.InvariantCulture));
        builder.Append("vigie_http_request_duration_ms_count ").AppendLine(Volatile.Read(ref totalRequests).ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("# HELP vigie_http_responses_total Réponses HTTP par code de statut.");
        builder.AppendLine("# TYPE vigie_http_responses_total counter");
        foreach (var response in responsesByStatus.OrderBy(item => item.Key))
        {
            builder.Append("vigie_http_responses_total{status=\"")
                .Append(response.Key.ToString(CultureInfo.InvariantCulture))
                .Append("\"} ")
                .AppendLine(response.Value.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
