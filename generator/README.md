# CloudTrace Log Generator

Generates synthetic logs and publishes them to Google Cloud Pub/Sub for testing the CloudTrace ingestion pipeline.

## Setup

```bash
# Install dependencies
pip install -r requirements.txt

# Or in Cloud Shell (already has google-cloud-pubsub)
cd generator
```

## Usage

### Basic Usage
```bash
# Normal traffic at 10 logs/second for 60 seconds
python load_generator.py --rps 10 --seconds 60

# Dry run (print logs instead of publishing)
python load_generator.py --rps 10 --seconds 10 --dry-run
```

### Traffic Profiles

| Profile | Description | Error Rate |
|---------|-------------|------------|
| `normal` | Baseline production traffic | ~5% |
| `error_burst` | Simulates service failure | ~70% |
| `latency_spike` | Simulates slow dependency | ~5% (high latency) |
| `new_signature` | Introduces novel error messages | ~30% |

```bash
# Simulate an error burst (for testing anomaly detection)
python load_generator.py --profile error_burst --rps 20 --seconds 120

# Simulate latency issues
python load_generator.py --profile latency_spike --rps 15 --seconds 180

# Introduce new error types
python load_generator.py --profile new_signature --rps 10 --seconds 60
```

### All Options
```bash
python load_generator.py --help

Options:
  --project    GCP Project ID (default: cloudtrace-481719)
  --topic      Pub/Sub topic name (default: logs)
  --rps        Logs per second (default: 10)
  --seconds    Duration in seconds (default: 60)
  --profile    Traffic profile (default: normal)
  --dry-run    Print logs instead of publishing
```

## Testing End-to-End

### 1. Generate baseline traffic
```bash
python load_generator.py --profile normal --rps 50 --seconds 300
```

### 2. Trigger an incident
```bash
# Wait for baseline, then run error burst
python load_generator.py --profile error_burst --rps 20 --seconds 120
```

### 3. Verify in BigQuery
```sql
SELECT 
  service,
  severity,
  COUNT(*) as count,
  AVG(latency_ms) as avg_latency
FROM `cloudtrace.logs`
WHERE ts > TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 10 MINUTE)
GROUP BY service, severity
ORDER BY count DESC
```
