using CloudTrace.Detector.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<AnomalyDetector>();
builder.Services.AddSingleton<FirestoreWriter>();
builder.Services.AddSingleton<IncidentPublisher>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/", () => "CloudTrace Detector Service");

// Manual trigger endpoint for testing and Cloud Scheduler
app.MapPost("/run", async (
    ILogger<Program> logger,
    AnomalyDetector detector,
    FirestoreWriter firestoreWriter,
    IncidentPublisher publisher) =>
{
    logger.LogInformation("Detector run triggered");
    
    var results = new
    {
        timestamp = DateTime.UtcNow,
        anomalies_detected = 0,
        incidents_created = new List<string>()
    };
    
    try
    {
        // Step 1: Detect anomalies from BigQuery
        var anomalies = await detector.DetectErrorSpikesAsync();
        logger.LogInformation("Found {Count} anomalies", anomalies.Count);
        
        var incidentsCreated = new List<string>();
        
        foreach (var anomaly in anomalies)
        {
            // Step 2: Create incident in Firestore (idempotent)
            var incidentId = await firestoreWriter.CreateIncidentAsync(anomaly);
            
            // Step 3: Publish to Pub/Sub for Analyzer
            await publisher.PublishIncidentAsync(incidentId, anomaly.Service, anomaly.DetermineSeverity());
            
            incidentsCreated.Add(incidentId);
        }
        
        return Results.Ok(new
        {
            timestamp = DateTime.UtcNow,
            anomalies_detected = anomalies.Count,
            incidents_created = incidentsCreated
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Detector run failed");
        return Results.StatusCode(500);
    }
});

// GET endpoint for Cloud Scheduler (some schedulers prefer GET)
app.MapGet("/run", async (
    ILogger<Program> logger,
    AnomalyDetector detector,
    FirestoreWriter firestoreWriter,
    IncidentPublisher publisher) =>
{
    logger.LogInformation("Detector run triggered via GET");
    
    try
    {
        var anomalies = await detector.DetectErrorSpikesAsync();
        var incidentsCreated = new List<string>();
        
        foreach (var anomaly in anomalies)
        {
            var incidentId = await firestoreWriter.CreateIncidentAsync(anomaly);
            await publisher.PublishIncidentAsync(incidentId, anomaly.Service, anomaly.DetermineSeverity());
            incidentsCreated.Add(incidentId);
        }
        
        return Results.Ok(new
        {
            timestamp = DateTime.UtcNow,
            anomalies_detected = anomalies.Count,
            incidents_created = incidentsCreated
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Detector run failed");
        return Results.StatusCode(500);
    }
});

// Debug endpoint to check log count
app.MapGet("/debug/count", async (AnomalyDetector detector) =>
{
    var count = await detector.GetLogCountAsync();
    return Results.Ok(new { log_count = count });
});

app.Run();
