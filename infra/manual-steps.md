# Manual GCP Setup Steps

This file documents manual infrastructure commands for CloudTrace MVP.

---

## Task 1.2 — Enable Required APIs

Run these commands after setting your project (see `project.md`):

```bash
# Store project ID in a variable for convenience
PROJECT_ID=$(gcloud config get-value project)

# Enable all required APIs
gcloud services enable run.googleapis.com \
  pubsub.googleapis.com \
  bigquery.googleapis.com \
  firestore.googleapis.com \
  cloudscheduler.googleapis.com \
  aiplatform.googleapis.com \
  logging.googleapis.com \
  monitoring.googleapis.com \
  --project=$PROJECT_ID
```

### Verification
```bash
# List enabled services
gcloud services list --enabled --project=$PROJECT_ID
```

### APIs Enabled
- ✅ Cloud Run (`run.googleapis.com`)
- ✅ Pub/Sub (`pubsub.googleapis.com`)
- ✅ BigQuery (`bigquery.googleapis.com`)
- ✅ Firestore (`firestore.googleapis.com`)
- ✅ Cloud Scheduler (`cloudscheduler.googleapis.com`)
- ✅ Vertex AI (`aiplatform.googleapis.com`)
- ✅ Cloud Logging (`logging.googleapis.com`)
- ✅ Cloud Monitoring (`monitoring.googleapis.com`)

---

## Notes
- API enablement may take 1-2 minutes to propagate
- Some APIs may require billing to be enabled first
