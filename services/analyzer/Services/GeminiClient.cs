using Google.Cloud.AIPlatform.V1;

namespace CloudTrace.Analyzer.Services;

public class GeminiClient
{
    private readonly PredictionServiceClient _client;
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _modelId;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(IConfiguration configuration, ILogger<GeminiClient> logger)
    {
        _logger = logger;
        
        _projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID not configured");
            
        _location = configuration["VERTEX_AI_LOCATION"]
            ?? Environment.GetEnvironmentVariable("VERTEX_AI_LOCATION")
            ?? "us-central1";
            
        _modelId = configuration["GEMINI_MODEL"]
            ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
            ?? "gemini-1.5-flash";

        // Create client with region-specific endpoint
        var endpoint = $"{_location}-aiplatform.googleapis.com";
        _client = new PredictionServiceClientBuilder
        {
            Endpoint = endpoint
        }.Build();
        
        _logger.LogInformation("GeminiClient initialized for {Project}/{Location}/{Model}", 
            _projectId, _location, _modelId);
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        var endpointName = EndpointName.FromProjectLocationPublisherModel(
            _projectId, _location, "google", _modelId);

        var content = new Content
        {
            Role = "user"
        };
        content.Parts.Add(new Part { Text = prompt });

        var request = new GenerateContentRequest
        {
            Model = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_modelId}",
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.2f,
                MaxOutputTokens = 2048,
                TopP = 0.8f
            }
        };
        request.Contents.Add(content);

        try
        {
            _logger.LogInformation("Sending request to Gemini...");
            var response = await _client.GenerateContentAsync(request);
            
            if (response.Candidates.Count > 0 && response.Candidates[0].Content.Parts.Count > 0)
            {
                var text = response.Candidates[0].Content.Parts[0].Text;
                _logger.LogInformation("Received response from Gemini ({Length} chars)", text.Length);
                return text;
            }
            
            _logger.LogWarning("Empty response from Gemini");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Gemini API");
            throw;
        }
    }
}
