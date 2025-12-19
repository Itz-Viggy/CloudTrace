using Google.Cloud.BigQuery.V2;
using CloudTrace.Detector.Models;

namespace CloudTrace.Detector.Services;

public class AnomalyDetector
{
    private readonly BigQueryClient _client;
    private readonly string _projectId;
    private readonly string _dataset;
    private readonly ILogger<AnomalyDetector> _logger;

    public AnomalyDetector(IConfiguration configuration, ILogger<AnomalyDetector> logger)
    {
        _logger = logger;
        
        _projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");
            
        _dataset = configuration["BQ_DATASET"]
            ?? Environment.GetEnvironmentVariable("BQ_DATASET")
            ?? "cloudtrace";

        _client = BigQueryClient.Create(_projectId);
        _logger.LogInformation("AnomalyDetector initialized for {Project}.{Dataset}", _projectId, _dataset);
    }

    public async Task<List<Anomaly>> DetectErrorSpikesAsync()
    {
        var query = $@"
            WITH recent_window AS (
              SELECT
                service,
                COUNT(*) AS total_requests,
                COUNTIF(severity = 'ERROR') AS error_count,
                TIMESTAMP_TRUNC(MIN(ts), MINUTE) AS window_start,
                TIMESTAMP_TRUNC(MAX(ts), MINUTE) AS window_end
              FROM `{_projectId}.{_dataset}.logs`
              WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 5 MINUTE)
              GROUP BY service
            ),
            baseline_window AS (
              SELECT
                service,
                COUNT(*) AS total_requests,
                COUNTIF(severity = 'ERROR') AS error_count,
                SAFE_DIVIDE(COUNTIF(severity = 'ERROR'), COUNT(*)) AS error_rate
              FROM `{_projectId}.{_dataset}.logs`
              WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 60 MINUTE)
                AND ts < TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 5 MINUTE)
              GROUP BY service
            )
            SELECT
              r.service,
              r.window_start,
              r.window_end,
              r.error_count,
              r.total_requests,
              SAFE_DIVIDE(r.error_count, r.total_requests) AS current_error_rate,
              COALESCE(b.error_rate, 0) AS baseline_error_rate,
              'ERROR_SPIKE' AS anomaly_type
            FROM recent_window r
            LEFT JOIN baseline_window b ON r.service = b.service
            WHERE 
              r.error_count >= 3
              AND (
                SAFE_DIVIDE(r.error_count, r.total_requests) >= COALESCE(b.error_rate, 0.01) * 2
                OR (COALESCE(b.error_rate, 0) < 0.05 AND SAFE_DIVIDE(r.error_count, r.total_requests) >= 0.15)
              )
            ORDER BY r.error_count DESC";

        return await ExecuteAnomalyQueryAsync(query);
    }

    public async Task<int> GetLogCountAsync()
    {
        var query = $"SELECT COUNT(*) as cnt FROM `{_projectId}.{_dataset}.logs`";
        var result = await _client.ExecuteQueryAsync(query, parameters: null);
        
        foreach (var row in result)
        {
            return (int)(long)row["cnt"];
        }
        return 0;
    }

    private async Task<List<Anomaly>> ExecuteAnomalyQueryAsync(string query)
    {
        var anomalies = new List<Anomaly>();
        
        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters: null);
            
            foreach (var row in result)
            {
                var anomaly = new Anomaly
                {
                    Service = row["service"]?.ToString() ?? "unknown",
                    WindowStart = row["window_start"] is DateTime dt ? dt : DateTime.UtcNow,
                    WindowEnd = row["window_end"] as DateTime?,
                    ErrorCount = row["error_count"] != null ? (int)(long)row["error_count"] : 0,
                    TotalRequests = row["total_requests"] != null ? (int)(long)row["total_requests"] : 0,
                    CurrentErrorRate = row["current_error_rate"] != null ? (double)row["current_error_rate"] : 0,
                    BaselineErrorRate = row["baseline_error_rate"] != null ? (double)row["baseline_error_rate"] : 0,
                    AnomalyType = row["anomaly_type"]?.ToString() ?? "ERROR_SPIKE"
                };
                
                anomalies.Add(anomaly);
                _logger.LogInformation("Detected anomaly: {Type} in {Service} with {ErrorCount} errors", 
                    anomaly.AnomalyType, anomaly.Service, anomaly.ErrorCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute anomaly detection query");
        }
        
        return anomalies;
    }
}
