using System.Text.Json;
using CloudTrace.Analyzer.Models;

namespace CloudTrace.Analyzer.Services;

public class PromptBuilder
{
    public string BuildAnalysisPrompt(IncidentEvidence evidence)
    {
        var errorSummary = string.Join("\n", evidence.TopErrors.Take(5).Select(e => 
            $"  - [{e.Count}x] (HTTP {e.StatusCode}) {e.Message.Take(100)}"));
        
        var latencyInfo = evidence.LatencyStats != null
            ? $"Latency: avg={evidence.LatencyStats.AvgLatencyMs:F0}ms, p50={evidence.LatencyStats.P50LatencyMs}ms, p95={evidence.LatencyStats.P95LatencyMs}ms, p99={evidence.LatencyStats.P99LatencyMs}ms"
            : "Latency: N/A";

        var prompt = $@"You are an expert Site Reliability Engineer analyzing a production incident.

## Incident Summary
- **Service**: {evidence.Service}
- **Severity**: {evidence.Severity}
- **Anomaly Type**: {evidence.AnomalyType}
- **Time Window**: {evidence.StartTs:u} to {evidence.EndTs?.ToString("u") ?? "ongoing"}
- **Error Count**: {evidence.ErrorCount}
- **Error Rate**: Current {evidence.CurrentRate:P1} vs Baseline {evidence.BaselineRate:P1}

## Top Error Messages
{errorSummary}

## Performance Metrics
{latencyInfo}

## Your Task
Analyze this incident and provide:
1. A concise summary (2-3 sentences) of what went wrong
2. The most likely root cause
3. 3-5 actionable mitigation steps
4. Your confidence level (0.0 to 1.0)
5. Useful debugging queries for BigQuery

## Response Format
Respond ONLY with valid JSON in this exact format:
{{
  ""summary"": ""Brief description of the incident"",
  ""root_cause"": ""Most likely cause of the issue"",
  ""mitigation_steps"": [""Step 1"", ""Step 2"", ""Step 3""],
  ""confidence"": 0.85,
  ""debugging_queries"": [""SELECT ... FROM logs WHERE ...""]
}}";

        return prompt;
    }

    public AiAnalysisResult? ParseResponse(string response)
    {
        try
        {
            // Clean up the response - remove markdown code blocks if present
            var cleaned = response.Trim();
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            cleaned = cleaned.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<AiAnalysisResult>(cleaned, options);
            return result;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
