# CloudTrace Ingestor Service — Deployment Guide

This document provides deployment commands for the Ingestor service.

---

## Prerequisites
- GCP Project: `cloudtrace-481719`
- BigQuery dataset and table created (see `infra/data.md`)
- Pub/Sub topics created (see `infra/pubsub.md`)
- Service accounts configured (see `infra/identities.md`)

---

## Task 4.10 — Deploy Ingestor to Cloud Run

### Option 1: Deploy from Source (Recommended)

Run these commands in Cloud Shell:

```bash
# Navigate to the ingestor source (upload via Cloud Shell or clone from GitHub)
cd services/ingestor

# Deploy directly from source (Cloud Build handles the container)
gcloud run deploy cloudtrace-ingestor \
  --source=. \
  --region=us-central1 \
  --service-account=cloudtrace-ingestor-sa@cloudtrace-481719.iam.gserviceaccount.com \
  --set-env-vars=GCP_PROJECT_ID=cloudtrace-481719,BQ_DATASET=cloudtrace,BQ_LOGS_TABLE=logs \
  --allow-unauthenticated \
  --project=cloudtrace-481719
```

### Option 2: Build and Push Manually

```bash
# Build and push to Container Registry
gcloud builds submit --tag gcr.io/cloudtrace-481719/cloudtrace-ingestor .

# Deploy the image
gcloud run deploy cloudtrace-ingestor \
  --image=gcr.io/cloudtrace-481719/cloudtrace-ingestor \
  --region=us-central1 \
  --service-account=cloudtrace-ingestor-sa@cloudtrace-481719.iam.gserviceaccount.com \
  --set-env-vars=GCP_PROJECT_ID=cloudtrace-481719,BQ_DATASET=cloudtrace,BQ_LOGS_TABLE=logs \
  --allow-unauthenticated \
  --project=cloudtrace-481719
```

---

## Task 4.11 — Configure Pub/Sub Push to Ingestor

After deployment, you'll receive a service URL like:
`https://cloudtrace-ingestor-XXXXX-uc.a.run.app`

Configure the push subscription:

```bash
# Get the service URL
INGESTOR_URL=$(gcloud run services describe cloudtrace-ingestor --region=us-central1 --format="value(status.url)")

# Delete the existing pull subscription and recreate as push
gcloud pubsub subscriptions delete logs-sub --project=cloudtrace-481719

gcloud pubsub subscriptions create logs-sub \
  --topic=logs \
  --push-endpoint="${INGESTOR_URL}/pubsub/logs" \
  --ack-deadline=60 \
  --project=cloudtrace-481719
```

---

## Verification

### Test the Health Endpoint
```bash
curl "${INGESTOR_URL}/health"
# Expected: {"status":"healthy"}
```

### Test End-to-End Ingestion
```bash
# Publish a log to the logs topic
gcloud pubsub topics publish logs \
  --message='{"ts": "2025-12-19T16:00:00Z", "service": "test-service", "severity": "INFO", "message": "Hello from CloudTrace!"}' \
  --project=cloudtrace-481719

# Check BigQuery for the row (wait a few seconds)
bq query --use_legacy_sql=false \
  "SELECT * FROM cloudtrace.logs ORDER BY ts DESC LIMIT 5"
```

---

## Rollback

To delete the service:
```bash
gcloud run services delete cloudtrace-ingestor --region=us-central1 --project=cloudtrace-481719
```
