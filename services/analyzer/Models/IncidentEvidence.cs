namespace CloudTrace.Analyzer.Models;

public class IncidentEvidence
{
    public string IncidentId { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime StartTs { get; set; }
    public DateTime? EndTs { get; set; }
    public int ErrorCount { get; set; }
    public double CurrentRate { get; set; }
    public double BaselineRate { get; set; }
    public string AnomalyType { get; set; } = "ERROR_SPIKE";
    
    // Evidence from BigQuery
    public List<ErrorSample> TopErrors { get; set; } = new();
    public LatencyStats? LatencyStats { get; set; }
}

public class ErrorSample
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorSignature { get; set; }
    public int Count { get; set; }
    public int? StatusCode { get; set; }
    public string? RequestPath { get; set; }
}

public class LatencyStats
{
    public double AvgLatencyMs { get; set; }
    public int P50LatencyMs { get; set; }
    public int P95LatencyMs { get; set; }
    public int P99LatencyMs { get; set; }
}

public class AiAnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public string RootCause { get; set; } = string.Empty;
    public List<string> MitigationSteps { get; set; } = new();
    public double Confidence { get; set; }
    public List<string> DebuggingQueries { get; set; } = new();
}
