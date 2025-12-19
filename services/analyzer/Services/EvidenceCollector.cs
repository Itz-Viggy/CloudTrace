using Google.Cloud.BigQuery.V2;
using CloudTrace.Analyzer.Models;

namespace CloudTrace.Analyzer.Services;

public class EvidenceCollector
{
    private readonly BigQueryClient _client;
    private readonly string _projectId;
    private readonly string _dataset;
    private readonly ILogger<EvidenceCollector> _logger;

    public EvidenceCollector(IConfiguration configuration, ILogger<EvidenceCollector> logger)
    {
        _logger = logger;
        
        _projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");
            
        _dataset = configuration["BQ_DATASET"]
            ?? Environment.GetEnvironmentVariable("BQ_DATASET")
            ?? "cloudtrace";

        _client = BigQueryClient.Create(_projectId);
        _logger.LogInformation("EvidenceCollector initialized for {Project}.{Dataset}", _projectId, _dataset);
    }

    public async Task<List<ErrorSample>> GetTopErrorsAsync(string service, DateTime startTs, DateTime endTs)
    {
        var query = $@"
            SELECT 
                message,
                error_signature,
                status_code,
                request_path,
                COUNT(*) as count
            FROM `{_projectId}.{_dataset}.logs`
            WHERE service = @service
                AND ts >= @startTs
                AND ts <= @endTs
                AND severity = 'ERROR'
            GROUP BY message, error_signature, status_code, request_path
            ORDER BY count DESC
            LIMIT 10";

        var parameters = new[]
        {
            new BigQueryParameter("service", BigQueryDbType.String, service),
            new BigQueryParameter("startTs", BigQueryDbType.Timestamp, startTs),
            new BigQueryParameter("endTs", BigQueryDbType.Timestamp, endTs)
        };

        var errors = new List<ErrorSample>();
        
        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters);
            
            foreach (var row in result)
            {
                errors.Add(new ErrorSample
                {
                    Message = row["message"]?.ToString() ?? "",
                    ErrorSignature = row["error_signature"]?.ToString(),
                    StatusCode = row["status_code"] != null ? (int?)(long)row["status_code"] : null,
                    RequestPath = row["request_path"]?.ToString(),
                    Count = row["count"] != null ? (int)(long)row["count"] : 0
                });
            }
            
            _logger.LogInformation("Found {Count} unique error patterns for {Service}", errors.Count, service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch top errors for {Service}", service);
        }
        
        return errors;
    }

    public async Task<LatencyStats?> GetLatencyStatsAsync(string service, DateTime startTs, DateTime endTs)
    {
        var query = $@"
            SELECT 
                AVG(latency_ms) as avg_latency,
                APPROX_QUANTILES(latency_ms, 100)[OFFSET(50)] as p50,
                APPROX_QUANTILES(latency_ms, 100)[OFFSET(95)] as p95,
                APPROX_QUANTILES(latency_ms, 100)[OFFSET(99)] as p99
            FROM `{_projectId}.{_dataset}.logs`
            WHERE service = @service
                AND ts >= @startTs
                AND ts <= @endTs
                AND latency_ms IS NOT NULL";

        var parameters = new[]
        {
            new BigQueryParameter("service", BigQueryDbType.String, service),
            new BigQueryParameter("startTs", BigQueryDbType.Timestamp, startTs),
            new BigQueryParameter("endTs", BigQueryDbType.Timestamp, endTs)
        };

        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters);
            
            foreach (var row in result)
            {
                return new LatencyStats
                {
                    AvgLatencyMs = row["avg_latency"] != null ? (double)row["avg_latency"] : 0,
                    P50LatencyMs = row["p50"] != null ? (int)(long)row["p50"] : 0,
                    P95LatencyMs = row["p95"] != null ? (int)(long)row["p95"] : 0,
                    P99LatencyMs = row["p99"] != null ? (int)(long)row["p99"] : 0
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch latency stats for {Service}", service);
        }
        
        return null;
    }
}
