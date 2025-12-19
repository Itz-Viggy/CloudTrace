# Data Stores Configuration

This file documents the BigQuery and Firestore data stores.

---

## BigQuery (Analytics State)

### Dataset: `cloudtrace`
- **Location**: same as `GCP_REGION` (e.g., `us-central1`)
- **Retention**: [System Default]

### Table: `logs`
- **Partitioning**: Daily on `ts` column
- **Schema**: See `infra/bq/logs_schema.json`

#### Creation Command:
```bash
# Create dataset
bq mk --dataset --location=us-central1 YOUR_PROJECT_ID:cloudtrace

# Create partitioned table
bq mk --table \
  --schema=infra/bq/logs_schema.json \
  --time_partitioning_field=ts \
  --time_partitioning_type=DAY \
  YOUR_PROJECT_ID:cloudtrace.logs
```

---

## Firestore (Operational State)

### Mode: Native
### Database: `(default)`
- **Location**: same as `GCP_REGION`

#### Initialization Command:
```bash
gcloud firestore databases create --location=us-central1
```
