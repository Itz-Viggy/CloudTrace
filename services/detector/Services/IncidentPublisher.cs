using Google.Cloud.PubSub.V1;
using Google.Protobuf;

namespace CloudTrace.Detector.Services;

public class IncidentPublisher
{
    private readonly PublisherClient _publisher;
    private readonly ILogger<IncidentPublisher> _logger;

    public IncidentPublisher(IConfiguration configuration, ILogger<IncidentPublisher> logger)
    {
        _logger = logger;
        
        var projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");
            
        var topicId = configuration["PUBSUB_INCIDENTS_TOPIC"]
            ?? Environment.GetEnvironmentVariable("PUBSUB_INCIDENTS_TOPIC")
            ?? "incidents";

        var topicName = TopicName.FromProjectTopic(projectId, topicId);
        _publisher = PublisherClient.Create(topicName);
        _logger.LogInformation("IncidentPublisher initialized for topic {Topic}", topicName);
    }

    public async Task PublishIncidentAsync(string incidentId, string service, string severity)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            incident_id = incidentId,
            service = service,
            severity = severity,
            timestamp = DateTime.UtcNow.ToString("O")
        });

        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(payload)
        };

        var messageId = await _publisher.PublishAsync(message);
        _logger.LogInformation("Published incident {IncidentId} to Pub/Sub with messageId {MessageId}", 
            incidentId, messageId);
    }
}
