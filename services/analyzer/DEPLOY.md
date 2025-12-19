# CloudTrace Analyzer Service — Deployment Guide

## Prerequisites
- GCP Project with Vertex AI enabled
- Firestore with incidents collection
- Pub/Sub `incidents` topic
- Service account with Vertex AI User role

---

## Deploy to Cloud Run

```bash
cd ~/CloudTrace/services/analyzer

gcloud run deploy cloudtrace-analyzer \
  --source=. \
  --region=us-central1 \
  --service-account=cloudtrace-analyzer-sa@cloudtrace-481719.iam.gserviceaccount.com \
  --set-env-vars="GCP_PROJECT_ID=cloudtrace-481719,BQ_DATASET=cloudtrace,VERTEX_AI_LOCATION=us-central1,GEMINI_MODEL=gemini-2.5-flash" \
  --allow-unauthenticated \
  --project=cloudtrace-481719 \
  --memory=512Mi \
  --timeout=120s
```

---

## Configure Pub/Sub Push Subscription

```bash
# Get the analyzer URL
ANALYZER_URL=$(gcloud run services describe cloudtrace-analyzer --region=us-central1 --format="value(status.url)")

# Delete existing subscription if it exists
gcloud pubsub subscriptions delete incidents-sub --project=cloudtrace-481719 --quiet 2>/dev/null || true

# Create push subscription
gcloud pubsub subscriptions create incidents-sub \
  --topic=incidents \
  --push-endpoint="${ANALYZER_URL}/pubsub/incidents" \
  --ack-deadline=120 \
  --project=cloudtrace-481719
```

---

## Manual Testing

### 1. Check health
```bash
curl "${ANALYZER_URL}/health"
```

### 2. Manually analyze an existing incident
```bash
# Use one of the incident IDs from the detector output
curl -X POST "${ANALYZER_URL}/analyze/bfde14e5da02ad49"
```

### 3. Check Firestore for AI results
Go to: https://console.cloud.google.com/firestore/data/incidents

Look for these fields:
- `ai_status`: "COMPLETED" or "FAILED"
- `ai_summary`: Text summary of the incident
- `ai_root_cause`: Suspected cause
- `ai_steps`: Array of mitigation steps
- `confidence`: 0.0 to 1.0

---

## Full E2E Test

```bash
# 1. Generate error burst
cd ~/CloudTrace/generator
python3 load_generator.py --profile error_burst --rps 20 --seconds 60

# 2. Trigger detector (creates incidents)
DETECTOR_URL=$(gcloud run services describe cloudtrace-detector --region=us-central1 --format="value(status.url)")
curl -X POST "${DETECTOR_URL}/run"

# 3. Check that analyzer received the incidents (via Pub/Sub)
#    The Pub/Sub push will automatically trigger analysis

# 4. After ~30 seconds, check Firestore for AI analysis
```

---

## Troubleshooting

### Check Analyzer logs
```bash
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=cloudtrace-analyzer" --limit 20 --format="table(textPayload,severity)"
```

### Verify Vertex AI API is enabled
```bash
gcloud services list --enabled | grep aiplatform
```

### Grant Vertex AI User role (if missing)
```bash
gcloud projects add-iam-policy-binding cloudtrace-481719 \
  --member="serviceAccount:cloudtrace-analyzer-sa@cloudtrace-481719.iam.gserviceaccount.com" \
  --role="roles/aiplatform.user"
```

---

## Rollback

```bash
gcloud run services delete cloudtrace-analyzer --region=us-central1 --project=cloudtrace-481719
gcloud pubsub subscriptions delete incidents-sub --project=cloudtrace-481719
```
