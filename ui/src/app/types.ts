export interface MetricOverview {
    total_logs: number;
    total_errors: number;
    error_rate: number | null;
    avg_latency: number | null;
  }
  
  export interface Incident {
    id: string;
    service: string;
    severity: 'CRITICAL' | 'WARNING' | 'INFO';
    status: 'OPEN' | 'INVESTIGATING' | 'RESOLVED';
    start_ts: { seconds: number; nanoseconds: number } | string;
    anomaly_type: string;
    ai_status: 'PENDING' | 'COMPLETED' | 'FAILED';
    ai_summary?: string;
    ai_root_cause?: string;
    ai_steps?: string[];
    error_count: number;
    current_rate: number;
    baseline_rate: number;
    debugging_queries?: string[];
  }
  
  export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
