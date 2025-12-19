using CloudTrace.Ingestor.Models;
using Google.Cloud.BigQuery.V2;

namespace CloudTrace.Ingestor.Services;

public class BigQueryWriter
{
    private readonly BigQueryClient _client;
    private readonly string _datasetId;
    private readonly string _tableId;
    private readonly ILogger<BigQueryWriter> _logger;

    public BigQueryWriter(IConfiguration configuration, ILogger<BigQueryWriter> logger)
    {
        _logger = logger;

        var projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");

        _datasetId = configuration["BQ_DATASET"]
            ?? Environment.GetEnvironmentVariable("BQ_DATASET")
            ?? "cloudtrace";

        _tableId = configuration["BQ_LOGS_TABLE"]
            ?? Environment.GetEnvironmentVariable("BQ_LOGS_TABLE")
            ?? "logs";

        _client = BigQueryClient.Create(projectId);
        _logger.LogInformation("BigQuery client initialized for {Project}.{Dataset}.{Table}", projectId, _datasetId, _tableId);
    }

    public async Task InsertAsync(LogEvent log)
    {
        await InsertManyAsync(new List<LogEvent> { log });
    }

    public async Task InsertManyAsync(List<LogEvent> logs)
    {
        if (logs.Count == 0) return;

        var table = _client.GetTable(_datasetId, _tableId);
        var rows = logs.Select(log => new BigQueryInsertRow
        {
            { "ts", log.Ts },
            { "service", log.Service },
            { "severity", log.Severity },
            { "status_code", log.StatusCode },
            { "latency_ms", log.LatencyMs },
            { "error_signature", log.ErrorSignature },
            { "message", log.Message },
            { "trace_id", log.TraceId },
            { "request_path", log.RequestPath },
            { "env", log.Env },
            { "deploy_id", log.DeployId }
        }).ToList();

        await table.InsertRowsAsync(rows);
        _logger.LogInformation("Inserted {Count} row(s) into BigQuery", logs.Count);
    }
}
