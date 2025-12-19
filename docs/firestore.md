# Firestore Schema Documentation

## Collection: `incidents`
Stores the lifecycle and AI analysis of detected incidents.

### Document structure: `incidents/{incident_id}`

| Field | Type | Description |
| :--- | :--- | :--- |
| `id` | `string` | Deterministic ID: `hash(service + window_start + type)` |
| `status` | `string` | `OPEN`, `ACKNOWLEDGED`, `RESOLVED` |
| `severity` | `string` | `CRITICAL`, `WARNING`, `INFO` |
| `service` | `string` | The primary service impacted |
| `start_ts` | `timestamp` | Start time of the anomaly window |
| `end_ts` | `timestamp` | End time of the anomaly window |
| `impacted_services` | `array<string>` | List of all services showing anomalies |
| `error_count` | `number` | Number of errors in the window |
| `baseline_rate` | `number` | Historical baseline for comparison |
| `ai_status` | `string` | `PENDING`, `COMPLETED`, `FAILED` |
| `ai_summary` | `string` | Human-readable explanation from Gemini |
| `ai_root_cause` | `string` | Suspected reason for the spike |
| `ai_steps` | `array<string>` | Recommended mitigation steps |
| `confidence` | `number` | AI confidence score (0.0 - 1.0) |
| `created_at` | `timestamp` | System creation time |
| `updated_at` | `timestamp` | Last update time |
