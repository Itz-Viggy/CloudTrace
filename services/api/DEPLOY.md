# CloudTrace API Service — Deployment Guide

## Prerequisites
- GCP Project: `cloudtrace-481719`
- BigQuery dataset: `cloudtrace`
- Firestore initialized
- Service account: `cloudtrace-api-sa@cloudtrace-481719.iam.gserviceaccount.com`

---

## Deploy to Cloud Run

```bash
cd ~/CloudTrace/services/api

gcloud run deploy cloudtrace-api \
  --source=. \
  --region=us-central1 \
  --service-account=cloudtrace-api-sa@cloudtrace-481719.iam.gserviceaccount.com \
  --set-env-vars="GCP_PROJECT_ID=cloudtrace-481719,BQ_DATASET=cloudtrace" \
  --allow-unauthenticated \
  --project=cloudtrace-481719
```

---

## Manual Testing

### 1. Check health
```bash
API_URL=$(gcloud run services describe cloudtrace-api --region=us-central1 --format="value(status.url)")
curl "${API_URL}/health"
```

### 2. List Incidents
```bash
curl "${API_URL}/incidents"
```

### 3. Get Overview Metrics
```bash
curl "${API_URL}/metrics/overview"
```

---

## Rollback

```bash
gcloud run services delete cloudtrace-api --region=us-central1 --project=cloudtrace-481719
```
