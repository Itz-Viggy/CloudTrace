# Pub/Sub Messaging Configuration

This file documents the Pub/Sub topics and subscriptions used for event-driven log ingestion and incident analysis.

---

## Task 3.1 — Create Pub/Sub Topics

Run these in Cloud Shell:

```bash
# Set project
PROJECT_ID="cloudtrace-481719"

# Create topics
gcloud pubsub topics create logs --project=$PROJECT_ID
gcloud pubsub topics create incidents --project=$PROJECT_ID
gcloud pubsub topics create logs-dlq --project=$PROJECT_ID
gcloud pubsub topics create incidents-dlq --project=$PROJECT_ID
```

---

## Task 3.2 — Create Subscriptions

We use Pull subscriptions for local testing and Push subscriptions once the Cloud Run services are deployed. For the MVP foundation, we start with Pull.

```bash
# Subscribe to logs (for Ingestor)
gcloud pubsub subscriptions create logs-sub \
  --topic=logs \
  --ack-deadline=60 \
  --project=$PROJECT_ID

# Subscribe to incidents (for Analyzer)
gcloud pubsub subscriptions create incidents-sub \
  --topic=incidents \
  --ack-deadline=60 \
  --project=$PROJECT_ID
```

---

## Task 3.3 — Verification Commands

To verify the setup is working, you can publish a test message and then pull it.

### Test Log Ingestion Path
```bash
# 1. Publish a test log
gcloud pubsub topics publish logs --message='{"ts": "2025-12-19T15:00:00Z", "service": "test-service", "message": "Hello CloudTrace"}'

# 2. Pull the message to verify
gcloud pubsub subscriptions pull logs-sub --auto-ack
```

### Test Incident Path
```bash
# 1. Publish a test incident
gcloud pubsub topics publish incidents --message='{"incident_id": "test-123"}'

# 2. Pull the message to verify
gcloud pubsub subscriptions pull incidents-sub --auto-ack
```

---

## Summary of Resources
- **Topic**: `logs` -> **Subscription**: `logs-sub`
- **Topic**: `incidents` -> **Subscription**: `incidents-sub`
- **DLQs**: `logs-dlq`, `incidents-dlq`
