# CloudTrace SRE Dashboard — Deployment Guide

## Prerequisites
- Cloud Run API Service deployed and URL available

---

## Deploy to Cloud Run

```bash
cd ~/CloudTrace/ui

# Get the API URL
API_URL=$(gcloud run services describe cloudtrace-api --region=us-central1 --format="value(status.url)")

# Deploy the UI
gcloud run deploy cloudtrace-ui \
  --source=. \
  --region=us-central1 \
  --set-env-vars="NEXT_PUBLIC_API_URL=${API_URL}" \
  --allow-unauthenticated \
  --project=cloudtrace-481719
```

---

## Local Development

```bash
cd ui
export NEXT_PUBLIC_API_URL="https://YOUR_API_URL"
npm install
npm run dev
```

Visit: `http://localhost:3000`

---

## Features
- **Live Incidents Feed**: Auto-refreshes every 30 seconds.
- **AI Diagnostics**: Direct visualization of Gemini's root cause analysis.
- **Metric Cards**: Real-time error rate and latency monitoring.
- **Responsive SRE Theme**: Designed for large NOC displays.

---

## Rollback
```bash
gcloud run services delete cloudtrace-ui --region=us-central1 --project=cloudtrace-481719
```
