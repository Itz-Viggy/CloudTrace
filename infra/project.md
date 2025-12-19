# GCP Project Configuration

## Project Setup (Task 1.1)

### Option 1: Create a new project
```bash
gcloud projects create YOUR-PROJECT-ID --name="CloudTrace"
gcloud config set project YOUR-PROJECT-ID
# Enable billing in the Console: https://console.cloud.google.com/billing
```

### Option 2: Use existing project
```bash
gcloud config set project YOUR-EXISTING-PROJECT-ID
```

### Project Details
- **PROJECT_ID**: `YOUR_PROJECT_ID`
- **Project Name**: CloudTrace
- **Billing**: Enabled

---

## Region Configuration (Task 1.3)

### Set default region
```bash
gcloud config set run/region us-central1
gcloud config set compute/region us-central1
```

### Region Details
- **REGION**: `us-central1`
- **Rationale**: Good balance of availability and pricing for Cloud Run, BigQuery, and Vertex AI

---

## Quick Reference
After completing the above:
- Update PROJECT_ID above with your actual project ID
- All subsequent commands will use this project and region
