using Google.Cloud.BigQuery.V2;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace CloudTrace.Api.Repositories;

public class BigQueryRepository
{
    private readonly BigQueryClient _client;
    private readonly string _projectId;
    private readonly string _dataset;

    public BigQueryRepository(IConfiguration configuration)
    {
        _projectId = configuration["GCP_PROJECT_ID"] 
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID is not configured.");
        
        _dataset = configuration["BQ_DATASET"] 
            ?? Environment.GetEnvironmentVariable("BQ_DATASET") 
            ?? "cloudtrace";

        _client = BigQueryClient.Create(_projectId);
    }

    public async Task<object> GetOverviewMetricsAsync()
    {
        var sql = $@"
            SELECT 
                COUNT(*) as total_logs,
                COUNTIF(severity = 'ERROR') as total_errors,
                SAFE_DIVIDE(COUNTIF(severity = 'ERROR'), COUNT(*)) as error_rate,
                AVG(latency_ms) as avg_latency
            FROM `{_projectId}.{_dataset}.logs`
            WHERE ts > TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 1 HOUR)";

        var result = await _client.ExecuteQueryAsync(sql, null);
        var row = result.FirstOrDefault();

        return new
        {
            total_logs = row?["total_logs"],
            total_errors = row?["total_errors"],
            error_rate = row?["error_rate"],
            avg_latency = row?["avg_latency"]
        };
    }
}
