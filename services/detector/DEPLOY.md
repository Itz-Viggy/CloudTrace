# CloudTrace Detector Service — Deployment Guide

## Prerequisites
- GCP Project: configured in environment
- BigQuery table with logs
- Firestore initialized
- Pub/Sub `incidents` topic created

---

## Deploy to Cloud Run

```bash
cd ~/CloudTrace/services/detector

gcloud run deploy cloudtrace-detector \
  --source=. \
  --region=us-central1 \
  --service-account=cloudtrace-detector-sa@cloudtrace-481719.iam.gserviceaccount.com \
  --set-env-vars="GCP_PROJECT_ID=cloudtrace-481719,BQ_DATASET=cloudtrace,PUBSUB_INCIDENTS_TOPIC=incidents" \
  --allow-unauthenticated \
  --project=cloudtrace-481719
```

---

## Configure Cloud Scheduler (Task 6.11)

Run the detector every minute:

```bash
# Get the detector URL
DETECTOR_URL=$(gcloud run services describe cloudtrace-detector --region=us-central1 --format="value(status.url)")

# Create the scheduler job
gcloud scheduler jobs create http cloudtrace-detector-job \
  --location=us-central1 \
  --schedule="* * * * *" \
  --uri="${DETECTOR_URL}/run" \
  --http-method=GET \
  --oidc-service-account-email=cloudtrace-detector-sa@cloudtrace-481719.iam.gserviceaccount.com \
  --project=cloudtrace-481719
```

---

## Manual Testing

### 1. Check health
```bash
curl "${DETECTOR_URL}/health"
```

### 2. Check log count (debug)
```bash
curl "${DETECTOR_URL}/debug/count"
```

### 3. Trigger detection manually
```bash
curl -X POST "${DETECTOR_URL}/run"
```

### 4. Generate error burst to trigger detection
```bash
cd ~/CloudTrace/generator
python3 load_generator.py --profile error_burst --rps 20 --seconds 60
```

### 5. Trigger detector and check results
```bash
curl -X POST "${DETECTOR_URL}/run"
# Should return: {"anomalies_detected": N, "incidents_created": [...]}
```

### 6. Verify incident in Firestore
Go to: https://console.cloud.google.com/firestore/data/incidents

---

## Rollback

```bash
gcloud run services delete cloudtrace-detector --region=us-central1 --project=cloudtrace-481719
gcloud scheduler jobs delete cloudtrace-detector-job --location=us-central1 --project=cloudtrace-481719
```
