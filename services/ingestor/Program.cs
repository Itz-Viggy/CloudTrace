using CloudTrace.Ingestor.Models;
using CloudTrace.Ingestor.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<BigQueryWriter>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/", () => "CloudTrace Ingestor Service");

app.MapPost("/pubsub/logs", async (HttpContext context, ILogger<Program> logger, BigQueryWriter bqWriter) =>
{
    // Parse the Pub/Sub push request
    var request = await context.Request.ReadFromJsonAsync<PubSubPushRequest>();
    if (request?.Message?.Data == null)
    {
        return Results.BadRequest("Invalid Pub/Sub message format.");
    }

    // Decode base64 payload
    var base64Data = request.Message.Data;
    var rawData = Convert.FromBase64String(base64Data);
    var messageString = System.Text.Encoding.UTF8.GetString(rawData);

    logger.LogInformation("Received log via Pub/Sub: {Message}", messageString);

    // Parse the log event
    var logEvent = LogEvent.FromJson(messageString);
    if (logEvent == null)
    {
        logger.LogWarning("Failed to parse log JSON: {Raw}", messageString);
        return Results.BadRequest("Invalid JSON payload.");
    }

    // Validate required fields
    var (isValid, error) = logEvent.Validate();
    if (!isValid)
    {
        logger.LogWarning("Log validation failed: {Error}", error);
        return Results.BadRequest(error);
    }

    // Normalize timestamp to UTC
    logEvent.NormalizeTimestamp();

    // Compute error signature for clustering
    logEvent.ComputeErrorSignature();

    // Insert into BigQuery
    try
    {
        await bqWriter.InsertAsync(logEvent);
        logger.LogInformation("Log inserted into BigQuery: service={Service}, ts={Ts}", logEvent.Service, logEvent.Ts);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to insert log into BigQuery");
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.Run();

public record PubSubPushRequest(PubSubMessage Message, string Subscription);
public record PubSubMessage(string Data, string MessageId, IDictionary<string, string>? Attributes);
