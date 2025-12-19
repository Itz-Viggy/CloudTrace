var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/", () => "CloudTrace Ingestor Service");

app.MapPost("/pubsub/logs", async (HttpContext context, ILogger<Program> logger) =>
{
    var request = await context.Request.ReadFromJsonAsync<PubSubPushRequest>();
    if (request?.Message?.Data == null)
    {
        return Results.BadRequest("Invalid Pub/Sub message format.");
    }

    var base64Data = request.Message.Data;
    var rawData = Convert.FromBase64String(base64Data);
    var messageString = System.Text.Encoding.UTF8.GetString(rawData);

    logger.LogInformation("Received log via Pub/Sub: {Message}", messageString);

    return Results.Ok();
});

app.Run();

public record PubSubPushRequest(PubSubMessage Message, string Subscription);
public record PubSubMessage(string Data, string MessageId, IDictionary<string, string>? Attributes);
