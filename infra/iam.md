# IAM Roles and Permissions

This file documents the IAM roles granted to each service account. These roles should be applied to your specific GCP project.

---

## Task 1.5 — Grant IAM Roles

```bash
# Set project ID and dataset
PROJECT_ID=$(gcloud config get-value project)
BQ_DATASET="cloudtrace"

# ============================================
# Ingestor Service Account Roles
# ============================================
INGESTOR_SA="cloudtrace-ingestor-sa@${PROJECT_ID}.iam.gserviceaccount.com"

# Pub/Sub Subscriber (for logs subscription)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${INGESTOR_SA}" \
  --role="roles/pubsub.subscriber"

# BigQuery Data Editor (for writing logs)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${INGESTOR_SA}" \
  --role="roles/bigquery.dataEditor"

# BigQuery Job User (to run jobs)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${INGESTOR_SA}" \
  --role="roles/bigquery.jobUser"

# Storage Object Creator (optional, for archiving)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${INGESTOR_SA}" \
  --role="roles/storage.objectCreator"

# ============================================
# Detector Service Account Roles
# ============================================
DETECTOR_SA="cloudtrace-detector-sa@${PROJECT_ID}.iam.gserviceaccount.com"

# BigQuery Data Viewer (read logs for anomaly detection)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${DETECTOR_SA}" \
  --role="roles/bigquery.dataViewer"

# BigQuery Job User
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${DETECTOR_SA}" \
  --role="roles/bigquery.jobUser"

# Firestore User (create/update incidents)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${DETECTOR_SA}" \
  --role="roles/datastore.user"

# Pub/Sub Publisher (publish incident events)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${DETECTOR_SA}" \
  --role="roles/pubsub.publisher"

# ============================================
# Analyzer Service Account Roles
# ============================================
ANALYZER_SA="cloudtrace-analyzer-sa@${PROJECT_ID}.iam.gserviceaccount.com"

# Pub/Sub Subscriber (receive incident events)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${ANALYZER_SA}" \
  --role="roles/pubsub.subscriber"

# BigQuery Data Viewer (read evidence logs)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${ANALYZER_SA}" \
  --role="roles/bigquery.dataViewer"

# BigQuery Job User
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${ANALYZER_SA}" \
  --role="roles/bigquery.jobUser"

# Firestore User (update incidents with AI results)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${ANALYZER_SA}" \
  --role="roles/datastore.user"

# Vertex AI User (call Gemini API)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${ANALYZER_SA}" \
  --role="roles/aiplatform.user"

# ============================================
# API Service Account Roles
# ============================================
API_SA="cloudtrace-api-sa@${PROJECT_ID}.iam.gserviceaccount.com"

# Firestore User (read incidents)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${API_SA}" \
  --role="roles/datastore.user"

# BigQuery Data Viewer (read-only analytics)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${API_SA}" \
  --role="roles/bigquery.dataViewer"

# BigQuery Job User
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:${API_SA}" \
  --role="roles/bigquery.jobUser"
```

---

## Role Summary

### Ingestor Service
- `roles/pubsub.subscriber` - Consume log messages
- `roles/bigquery.dataEditor` - Write to logs table
- `roles/bigquery.jobUser` - Execute BigQuery jobs
- `roles/storage.objectCreator` - Archive logs (optional)

### Detector Service
- `roles/bigquery.dataViewer` - Read logs for analysis
- `roles/bigquery.jobUser` - Run anomaly queries
- `roles/datastore.user` - Create/update incidents in Firestore
- `roles/pubsub.publisher` - Publish incident events

### Analyzer Service
- `roles/pubsub.subscriber` - Receive incident events
- `roles/bigquery.dataViewer` - Fetch evidence
- `roles/bigquery.jobUser` - Run evidence queries
- `roles/datastore.user` - Update incidents with AI analysis
- `roles/aiplatform.user` - Call Vertex AI (Gemini)

### API Service
- `roles/datastore.user` - Read incidents from Firestore
- `roles/bigquery.dataViewer` - Read analytics data
- `roles/bigquery.jobUser` - Run analytics queries

---

## Verification

```bash
# Check roles for a specific service account
gcloud projects get-iam-policy $PROJECT_ID \
  --flatten="bindings[].members" \
  --format="table(bindings.role)" \
  --filter="bindings.members:cloudtrace-ingestor-sa@${PROJECT_ID}.iam.gserviceaccount.com"
```

---

## Security Notes
- All roles follow least-privilege principle
- No service has project-wide admin access
- Service accounts cannot create other service accounts
- Roles can be revoked at any time without affecting other services
