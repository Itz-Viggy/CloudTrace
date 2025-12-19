using CloudTrace.Analyzer.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<EvidenceCollector>();
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<GeminiClient>();
builder.Services.AddSingleton<IncidentUpdater>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/", () => "CloudTrace Analyzer Service");

// Pub/Sub push handler
app.MapPost("/pubsub/incidents", async (
    HttpContext context,
    ILogger<Program> logger,
    EvidenceCollector evidenceCollector,
    PromptBuilder promptBuilder,
    GeminiClient geminiClient,
    IncidentUpdater incidentUpdater) =>
{
    // Parse Pub/Sub push message
    var request = await context.Request.ReadFromJsonAsync<PubSubPushRequest>();
    if (request?.Message?.Data == null)
    {
        return Results.BadRequest("Invalid Pub/Sub message format.");
    }

    var base64Data = request.Message.Data;
    var rawData = Convert.FromBase64String(base64Data);
    var messageString = System.Text.Encoding.UTF8.GetString(rawData);

    logger.LogInformation("Received incident event: {Message}", messageString);

    // Parse incident payload
    IncidentPayload? payload;
    try
    {
        payload = JsonSerializer.Deserialize<IncidentPayload>(messageString, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        logger.LogWarning("Failed to parse incident payload");
        return Results.BadRequest("Invalid incident payload");
    }

    if (string.IsNullOrEmpty(payload?.IncidentId))
    {
        return Results.BadRequest("Missing incident_id");
    }

    // Process the incident
    try
    {
        await ProcessIncidentAsync(
            payload.IncidentId,
            logger,
            evidenceCollector,
            promptBuilder,
            geminiClient,
            incidentUpdater);
        
        return Results.Ok(new { incident_id = payload.IncidentId, status = "analyzed" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process incident {IncidentId}", payload.IncidentId);
        return Results.StatusCode(500);
    }
});

// Manual trigger for testing
app.MapPost("/analyze/{incidentId}", async (
    string incidentId,
    ILogger<Program> logger,
    EvidenceCollector evidenceCollector,
    PromptBuilder promptBuilder,
    GeminiClient geminiClient,
    IncidentUpdater incidentUpdater) =>
{
    try
    {
        await ProcessIncidentAsync(
            incidentId,
            logger,
            evidenceCollector,
            promptBuilder,
            geminiClient,
            incidentUpdater);
        
        return Results.Ok(new { incident_id = incidentId, status = "analyzed" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to analyze incident {IncidentId}", incidentId);
        return Results.StatusCode(500);
    }
});

app.Run();

// Helper method for processing incidents
async Task ProcessIncidentAsync(
    string incidentId,
    ILogger logger,
    EvidenceCollector evidenceCollector,
    PromptBuilder promptBuilder,
    GeminiClient geminiClient,
    IncidentUpdater incidentUpdater)
{
    logger.LogInformation("Processing incident {IncidentId}", incidentId);
    
    // Step 1: Fetch incident from Firestore
    var incident = await incidentUpdater.GetIncidentAsync(incidentId);
    if (incident == null)
    {
        throw new InvalidOperationException($"Incident {incidentId} not found");
    }
    
    // Step 2: Collect evidence from BigQuery
    var endTs = incident.EndTs ?? DateTime.UtcNow;
    incident.TopErrors = await evidenceCollector.GetTopErrorsAsync(
        incident.Service, 
        incident.StartTs, 
        endTs);
    incident.LatencyStats = await evidenceCollector.GetLatencyStatsAsync(
        incident.Service, 
        incident.StartTs, 
        endTs);
    
    logger.LogInformation("Collected evidence: {ErrorCount} error patterns, latency data: {HasLatency}",
        incident.TopErrors.Count, incident.LatencyStats != null);
    
    // Step 3: Build prompt
    var prompt = promptBuilder.BuildAnalysisPrompt(incident);
    
    // Step 4: Call Gemini
    var response = await geminiClient.GenerateContentAsync(prompt);
    
    // Step 5: Parse response
    var result = promptBuilder.ParseResponse(response);
    
    if (result != null)
    {
        // Step 6: Update Firestore with AI results
        await incidentUpdater.UpdateWithAiResultsAsync(incidentId, result);
        logger.LogInformation("Successfully analyzed incident {IncidentId}", incidentId);
    }
    else
    {
        // Mark as failed and store raw response for debugging
        await incidentUpdater.MarkAiFailedAsync(incidentId, response);
        logger.LogWarning("Failed to parse AI response for incident {IncidentId}", incidentId);
    }
}

public record PubSubPushRequest(PubSubMessage Message, string Subscription);
public record PubSubMessage(string Data, string MessageId, IDictionary<string, string>? Attributes);
public record IncidentPayload
{
    public string IncidentId { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? Severity { get; set; }
}
