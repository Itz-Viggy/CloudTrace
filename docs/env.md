# Environment Variables Specification

This document lists the environment variables required for each service. Values should be populated in the deployment environment.

## Shared (GCP)
- `GCP_PROJECT_ID`: The ID of your Google Cloud Project.
- `GCP_REGION`: The region where services and datasets are deployed (e.g., `us-central1`).

## Ingestor Service (`services/ingestor`)
- `BQ_DATASET`: BigQuery dataset name (e.g., `cloudtrace`).
- `BQ_LOGS_TABLE`: BigQuery table name for logs (e.g., `logs`).
- `PUBSUB_LOGS_SUB`: Subscription ID for logs push (if applicable).

## Detector Service (`services/detector`)
- `BQ_DATASET`: BigQuery dataset name.
- `BQ_LOGS_TABLE`: BigQuery table name for logs.
- `FIRESTORE_PROJECT_ID`: Project ID for Firestore.
- `PUBSUB_INCIDENTS_TOPIC`: Topic ID for publishing incident events.

## Analyzer Service (`services/analyzer`)
- `GCP_PROJECT_ID`: Project ID for Vertex AI.
- `BQ_DATASET`: BigQuery dataset name for evidence fetching.
- `FIRESTORE_PROJECT_ID`: Project ID for incident updates.

## API Service (`services/api`)
- `GCP_PROJECT_ID`: Project ID for Firestore and BigQuery access.
- `BQ_DATASET`: BigQuery dataset name.
- `FIRESTORE_COLLECTION`: Firestore collection name for incidents (e.g., `incidents`).
