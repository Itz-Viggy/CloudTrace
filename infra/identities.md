# Service Account Identities

This file documents the service accounts for CloudTrace and provides commands to create them.

---

## Task 1.4 — Create Service Accounts

```bash
# Set project ID
PROJECT_ID=$(gcloud config get-value project)

# Create service accounts
gcloud iam service-accounts create cloudtrace-ingestor-sa \
  --display-name="CloudTrace Ingestor Service" \
  --project=$PROJECT_ID

gcloud iam service-accounts create cloudtrace-detector-sa \
  --display-name="CloudTrace Detector Service" \
  --project=$PROJECT_ID

gcloud iam service-accounts create cloudtrace-analyzer-sa \
  --display-name="CloudTrace Analyzer Service" \
  --project=$PROJECT_ID

gcloud iam service-accounts create cloudtrace-api-sa \
  --display-name="CloudTrace API Service" \
  --project=$PROJECT_ID
```

---

## Service Account Emails

After creation, your service account emails will be:

- **Ingestor SA**: `cloudtrace-ingestor-sa@${PROJECT_ID}.iam.gserviceaccount.com`
- **Detector SA**: `cloudtrace-detector-sa@${PROJECT_ID}.iam.gserviceaccount.com`
- **Analyzer SA**: `cloudtrace-analyzer-sa@${PROJECT_ID}.iam.gserviceaccount.com`
- **API SA**: `cloudtrace-api-sa@${PROJECT_ID}.iam.gserviceaccount.com`

### Verification
```bash
# List all service accounts
gcloud iam service-accounts list --project=$PROJECT_ID
```

---

## Notes
- These service accounts follow the principle of least privilege
- Each service will run with only the permissions it needs
- Service accounts are assigned to Cloud Run services during deployment
