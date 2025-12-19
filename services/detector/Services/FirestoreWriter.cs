using Google.Cloud.Firestore;
using CloudTrace.Detector.Models;

namespace CloudTrace.Detector.Services;

public class FirestoreWriter
{
    private readonly FirestoreDb _db;
    private readonly ILogger<FirestoreWriter> _logger;
    private const string INCIDENTS_COLLECTION = "incidents";

    public FirestoreWriter(IConfiguration configuration, ILogger<FirestoreWriter> logger)
    {
        _logger = logger;
        
        var projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");

        _db = FirestoreDb.Create(projectId);
        _logger.LogInformation("FirestoreWriter initialized for project {Project}", projectId);
    }

    public async Task<bool> IncidentExistsAsync(string incidentId)
    {
        var docRef = _db.Collection(INCIDENTS_COLLECTION).Document(incidentId);
        var snapshot = await docRef.GetSnapshotAsync();
        return snapshot.Exists;
    }

    public async Task<string> CreateIncidentAsync(Anomaly anomaly)
    {
        var incidentId = anomaly.GenerateIncidentId();
        
        // Check if incident already exists (idempotency)
        if (await IncidentExistsAsync(incidentId))
        {
            _logger.LogInformation("Incident {IncidentId} already exists, skipping", incidentId);
            return incidentId;
        }
        
        var docRef = _db.Collection(INCIDENTS_COLLECTION).Document(incidentId);
        
        var incident = new Dictionary<string, object>
        {
            { "id", incidentId },
            { "status", "OPEN" },
            { "severity", anomaly.DetermineSeverity() },
            { "service", anomaly.Service },
            { "start_ts", Timestamp.FromDateTime(anomaly.WindowStart.ToUniversalTime()) },
            { "end_ts", anomaly.WindowEnd.HasValue 
                ? Timestamp.FromDateTime(anomaly.WindowEnd.Value.ToUniversalTime()) 
                : (object)FieldValue.ServerTimestamp },
            { "impacted_services", new List<string> { anomaly.Service } },
            { "error_count", anomaly.ErrorCount },
            { "baseline_rate", anomaly.BaselineErrorRate },
            { "current_rate", anomaly.CurrentErrorRate },
            { "anomaly_type", anomaly.AnomalyType },
            { "ai_status", "PENDING" },
            { "ai_summary", null! },
            { "ai_root_cause", null! },
            { "ai_steps", new List<string>() },
            { "confidence", 0.0 },
            { "created_at", FieldValue.ServerTimestamp },
            { "updated_at", FieldValue.ServerTimestamp }
        };
        
        await docRef.SetAsync(incident);
        _logger.LogInformation("Created incident {IncidentId} for {Service} ({Severity})", 
            incidentId, anomaly.Service, anomaly.DetermineSeverity());
        
        return incidentId;
    }
}
