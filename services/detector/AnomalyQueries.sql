-- Error Spike Detection Query
-- Compares error count in the last 5 minutes against the baseline from the last 60 minutes
-- Returns services where error rate is significantly higher than baseline

WITH recent_window AS (
  SELECT
    service,
    COUNT(*) AS total_requests,
    COUNTIF(severity = 'ERROR') AS error_count,
    TIMESTAMP_TRUNC(MIN(ts), MINUTE) AS window_start,
    TIMESTAMP_TRUNC(MAX(ts), MINUTE) AS window_end
  FROM `{project_id}.{dataset}.logs`
  WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 5 MINUTE)
  GROUP BY service
),
baseline_window AS (
  SELECT
    service,
    COUNT(*) AS total_requests,
    COUNTIF(severity = 'ERROR') AS error_count,
    SAFE_DIVIDE(COUNTIF(severity = 'ERROR'), COUNT(*)) AS error_rate
  FROM `{project_id}.{dataset}.logs`
  WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 60 MINUTE)
    AND ts < TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 5 MINUTE)
  GROUP BY service
)
SELECT
  r.service,
  r.window_start,
  r.window_end,
  r.error_count,
  r.total_requests,
  SAFE_DIVIDE(r.error_count, r.total_requests) AS current_error_rate,
  COALESCE(b.error_rate, 0) AS baseline_error_rate,
  'ERROR_SPIKE' AS anomaly_type
FROM recent_window r
LEFT JOIN baseline_window b ON r.service = b.service
WHERE 
  r.error_count >= 5  -- Minimum threshold
  AND (
    -- Error rate is at least 3x the baseline
    SAFE_DIVIDE(r.error_count, r.total_requests) >= COALESCE(b.error_rate, 0) * 3
    -- Or baseline was near-zero and now we have significant errors
    OR (COALESCE(b.error_rate, 0) < 0.05 AND SAFE_DIVIDE(r.error_count, r.total_requests) >= 0.2)
  )
ORDER BY r.error_count DESC;


-- Latency Regression Query (MVP+)
-- Detects p95 latency spikes compared to baseline

WITH recent_latency AS (
  SELECT
    service,
    APPROX_QUANTILES(latency_ms, 100)[OFFSET(95)] AS p95_latency,
    AVG(latency_ms) AS avg_latency,
    COUNT(*) AS request_count,
    TIMESTAMP_TRUNC(MIN(ts), MINUTE) AS window_start
  FROM `{project_id}.{dataset}.logs`
  WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 5 MINUTE)
    AND latency_ms IS NOT NULL
  GROUP BY service
),
baseline_latency AS (
  SELECT
    service,
    APPROX_QUANTILES(latency_ms, 100)[OFFSET(95)] AS p95_latency,
    AVG(latency_ms) AS avg_latency
  FROM `{project_id}.{dataset}.logs`
  WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 60 MINUTE)
    AND ts < TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 5 MINUTE)
    AND latency_ms IS NOT NULL
  GROUP BY service
)
SELECT
  r.service,
  r.window_start,
  r.p95_latency AS current_p95,
  COALESCE(b.p95_latency, 0) AS baseline_p95,
  r.request_count,
  'LATENCY_SPIKE' AS anomaly_type
FROM recent_latency r
LEFT JOIN baseline_latency b ON r.service = b.service
WHERE 
  r.request_count >= 10
  AND r.p95_latency >= COALESCE(b.p95_latency, 100) * 2  -- 2x latency increase
ORDER BY r.p95_latency DESC;


-- Novel Error Signature Detection (MVP+)
-- Finds error signatures that appeared recently but weren't seen in baseline

WITH recent_signatures AS (
  SELECT
    service,
    error_signature,
    COUNT(*) AS occurrence_count,
    MIN(ts) AS first_seen
  FROM `{project_id}.{dataset}.logs`
  WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 10 MINUTE)
    AND severity = 'ERROR'
    AND error_signature IS NOT NULL
  GROUP BY service, error_signature
),
baseline_signatures AS (
  SELECT DISTINCT
    service,
    error_signature
  FROM `{project_id}.{dataset}.logs`
  WHERE ts >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 24 HOUR)
    AND ts < TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 10 MINUTE)
    AND severity = 'ERROR'
    AND error_signature IS NOT NULL
)
SELECT
  r.service,
  r.error_signature,
  r.occurrence_count,
  r.first_seen AS window_start,
  'NOVEL_SIGNATURE' AS anomaly_type
FROM recent_signatures r
LEFT JOIN baseline_signatures b 
  ON r.service = b.service AND r.error_signature = b.error_signature
WHERE b.error_signature IS NULL  -- Not seen in baseline
  AND r.occurrence_count >= 3    -- Appeared multiple times
ORDER BY r.occurrence_count DESC;
