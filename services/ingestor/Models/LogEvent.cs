using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CloudTrace.Ingestor.Models;

public class LogEvent
{
    public DateTime Ts { get; set; }
    public string Service { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public int? StatusCode { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorSignature { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public string? RequestPath { get; set; }
    public string? Env { get; set; }
    public string? DeployId { get; set; }

    public static LogEvent? FromJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<LogEvent>(json, options);
        }
        catch
        {
            return null;
        }
    }

    public (bool IsValid, string? Error) Validate()
    {
        if (Ts == default)
            return (false, "Missing required field: ts");
        if (string.IsNullOrWhiteSpace(Service))
            return (false, "Missing required field: service");
        if (string.IsNullOrWhiteSpace(Message))
            return (false, "Missing required field: message");

        return (true, null);
    }

    public void NormalizeTimestamp()
    {
        // Ensure timestamp is in UTC
        Ts = Ts.ToUniversalTime();
    }

    public void ComputeErrorSignature()
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            ErrorSignature = null;
            return;
        }

        // Normalize: remove numbers, UUIDs, and extra whitespace
        var normalized = Regex.Replace(Message, @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", "<UUID>");
        normalized = Regex.Replace(normalized, @"\d+", "<NUM>");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        // Hash the normalized string
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        ErrorSignature = Convert.ToHexString(hash)[..16]; // Use first 16 chars
    }
}
