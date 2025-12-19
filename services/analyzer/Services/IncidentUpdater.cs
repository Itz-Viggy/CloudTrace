using Google.Cloud.Firestore;
using CloudTrace.Analyzer.Models;

namespace CloudTrace.Analyzer.Services;

public class IncidentUpdater
{
    private readonly FirestoreDb _db;
    private readonly ILogger<IncidentUpdater> _logger;
    private const string INCIDENTS_COLLECTION = "incidents";

    public IncidentUpdater(IConfiguration configuration, ILogger<IncidentUpdater> logger)
    {
        _logger = logger;
        
        var projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");

        _db = FirestoreDb.Create(projectId);
        _logger.LogInformation("IncidentUpdater initialized for project {Project}", projectId);
    }

    public async Task<IncidentEvidence?> GetIncidentAsync(string incidentId)
    {
        var docRef = _db.Collection(INCIDENTS_COLLECTION).Document(incidentId);
        var snapshot = await docRef.GetSnapshotAsync();
        
        if (!snapshot.Exists)
        {
            _logger.LogWarning("Incident {IncidentId} not found", incidentId);
            return null;
        }

        var dict = snapshot.ToDictionary();
        
        return new IncidentEvidence
        {
            IncidentId = incidentId,
            Service = dict.GetValueOrDefault("service")?.ToString() ?? "unknown",
            Severity = dict.GetValueOrDefault("severity")?.ToString() ?? "WARNING",
            StartTs = dict.GetValueOrDefault("start_ts") is Timestamp ts 
                ? ts.ToDateTime() 
                : DateTime.UtcNow.AddMinutes(-5),
            EndTs = dict.GetValueOrDefault("end_ts") is Timestamp endTs 
                ? endTs.ToDateTime() 
                : DateTime.UtcNow,
            ErrorCount = dict.GetValueOrDefault("error_count") is long ec ? (int)ec : 0,
            CurrentRate = dict.GetValueOrDefault("current_rate") is double cr ? cr : 0,
            BaselineRate = dict.GetValueOrDefault("baseline_rate") is double br ? br : 0,
            AnomalyType = dict.GetValueOrDefault("anomaly_type")?.ToString() ?? "ERROR_SPIKE"
        };
    }

    public async Task UpdateWithAiResultsAsync(string incidentId, AiAnalysisResult result)
    {
        var docRef = _db.Collection(INCIDENTS_COLLECTION).Document(incidentId);
        
        var updates = new Dictionary<string, object>
        {
            { "ai_status", "COMPLETED" },
            { "ai_summary", result.Summary },
            { "ai_root_cause", result.RootCause },
            { "ai_steps", result.MitigationSteps },
            { "confidence", result.Confidence },
            { "debugging_queries", result.DebuggingQueries },
            { "updated_at", FieldValue.ServerTimestamp }
        };
        
        await docRef.UpdateAsync(updates);
        _logger.LogInformation("Updated incident {IncidentId} with AI analysis (confidence: {Confidence})", 
            incidentId, result.Confidence);
    }

    public async Task MarkAiFailedAsync(string incidentId, string rawResponse)
    {
        var docRef = _db.Collection(INCIDENTS_COLLECTION).Document(incidentId);
        
        var updates = new Dictionary<string, object>
        {
            { "ai_status", "FAILED" },
            { "ai_raw_response", rawResponse },
            { "updated_at", FieldValue.ServerTimestamp }
        };
        
        await docRef.UpdateAsync(updates);
        _logger.LogWarning("Marked incident {IncidentId} AI analysis as FAILED", incidentId);
    }
}
