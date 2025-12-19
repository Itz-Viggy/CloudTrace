namespace CloudTrace.Detector.Models;

public class Anomaly
{
    public string Service { get; set; } = string.Empty;
    public DateTime WindowStart { get; set; }
    public DateTime? WindowEnd { get; set; }
    public int ErrorCount { get; set; }
    public int TotalRequests { get; set; }
    public double CurrentErrorRate { get; set; }
    public double BaselineErrorRate { get; set; }
    public string AnomalyType { get; set; } = "ERROR_SPIKE";
    
    // For latency anomalies
    public int? CurrentP95 { get; set; }
    public int? BaselineP95 { get; set; }
    
    // For novel signatures
    public string? ErrorSignature { get; set; }
    public int? OccurrenceCount { get; set; }
    
    public string GenerateIncidentId()
    {
        // Deterministic ID: hash of service + window_start + type
        var input = $"{Service}|{WindowStart:O}|{AnomalyType}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
    
    public string DetermineSeverity()
    {
        if (AnomalyType == "ERROR_SPIKE")
        {
            if (CurrentErrorRate >= 0.5) return "CRITICAL";
            if (CurrentErrorRate >= 0.2) return "WARNING";
            return "INFO";
        }
        
        if (AnomalyType == "LATENCY_SPIKE")
        {
            if (CurrentP95 >= 10000) return "CRITICAL";
            if (CurrentP95 >= 5000) return "WARNING";
            return "INFO";
        }
        
        return "WARNING";
    }
}
