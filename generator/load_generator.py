#!/usr/bin/env python3
"""
CloudTrace Log Generator

Generates synthetic logs and publishes them to Google Cloud Pub/Sub.
Supports multiple error profiles for testing anomaly detection.

Usage:
    python load_generator.py --rps 10 --seconds 60 --profile normal
    python load_generator.py --rps 50 --seconds 120 --profile error_burst
"""

import argparse
import json
import random
import string
import time
import uuid
from datetime import datetime, timezone
from typing import Optional

# Try to import Pub/Sub client - gracefully handle missing dependency
try:
    from google.cloud import pubsub_v1
    PUBSUB_AVAILABLE = True
except ImportError:
    PUBSUB_AVAILABLE = False
    print("WARNING: google-cloud-pubsub not installed. Running in dry-run mode.")


# Configuration
SERVICES = ["api-gateway", "user-service", "order-service", "payment-service", "inventory-service"]
ENDPOINTS = ["/api/users", "/api/orders", "/api/products", "/api/payments", "/api/inventory", "/health"]
ENVIRONMENTS = ["prod", "staging"]
SEVERITIES = ["DEBUG", "INFO", "WARN", "ERROR"]

# Error message templates
ERROR_MESSAGES = [
    "Connection timeout to database after {ms}ms",
    "Failed to authenticate user {user_id}",
    "Payment processing failed: insufficient funds",
    "Inventory check failed for product {product_id}",
    "Rate limit exceeded for IP {ip}",
    "Invalid request payload: missing required field 'email'",
    "Service unavailable: upstream connection refused",
    "Database query timeout: SELECT * FROM orders WHERE...",
]

INFO_MESSAGES = [
    "Request processed successfully",
    "User {user_id} logged in",
    "Order {order_id} created",
    "Health check passed",
    "Cache hit for key {key}",
    "Metrics exported successfully",
]


class LogGenerator:
    def __init__(self, project_id: str, topic_id: str = "logs", dry_run: bool = False):
        self.project_id = project_id
        self.topic_id = topic_id
        self.dry_run = dry_run or not PUBSUB_AVAILABLE
        self.publisher = None
        self.topic_path = None
        self.deploy_id = f"v{random.randint(100, 999)}"
        
        if not self.dry_run:
            self.publisher = pubsub_v1.PublisherClient()
            self.topic_path = self.publisher.topic_path(project_id, topic_id)
            print(f"Connected to Pub/Sub topic: {self.topic_path}")
        else:
            print("Running in DRY-RUN mode (logs printed, not published)")

    def generate_log(self, profile: str = "normal") -> dict:
        """Generate a single log event based on the profile."""
        
        ts = datetime.now(timezone.utc).isoformat()
        service = random.choice(SERVICES)
        request_path = random.choice(ENDPOINTS)
        trace_id = str(uuid.uuid4())
        env = random.choice(ENVIRONMENTS)
        
        # Profile-specific behavior
        if profile == "error_burst":
            # 70% errors during burst
            is_error = random.random() < 0.7
        elif profile == "latency_spike":
            # Normal error rate, but high latency
            is_error = random.random() < 0.05
        elif profile == "new_signature":
            # Introduce novel error messages
            is_error = random.random() < 0.3
        else:  # normal
            is_error = random.random() < 0.05
        
        if is_error:
            severity = "ERROR"
            status_code = random.choice([500, 502, 503, 504, 400, 401, 403, 429])
            
            if profile == "new_signature":
                # Generate unique error signatures
                message = f"NOVEL_ERROR_{random.randint(1000, 9999)}: Unexpected failure in {service}"
            else:
                message = random.choice(ERROR_MESSAGES).format(
                    ms=random.randint(5000, 30000),
                    user_id=f"user_{random.randint(1, 1000)}",
                    product_id=f"prod_{random.randint(1, 500)}",
                    order_id=f"ord_{random.randint(10000, 99999)}",
                    ip=f"192.168.{random.randint(1, 255)}.{random.randint(1, 255)}",
                    key=f"cache_key_{random.randint(1, 100)}"
                )
            latency_ms = random.randint(1000, 10000)
        else:
            severity = random.choice(["INFO", "DEBUG"])
            status_code = 200
            message = random.choice(INFO_MESSAGES).format(
                user_id=f"user_{random.randint(1, 1000)}",
                order_id=f"ord_{random.randint(10000, 99999)}",
                key=f"cache_key_{random.randint(1, 100)}"
            )
            
            if profile == "latency_spike":
                # High latency even for successful requests
                latency_ms = random.randint(2000, 15000)
            else:
                latency_ms = random.randint(10, 500)
        
        return {
            "ts": ts,
            "service": service,
            "severity": severity,
            "status_code": status_code,
            "latency_ms": latency_ms,
            "message": message,
            "trace_id": trace_id,
            "request_path": request_path,
            "env": env,
            "deploy_id": self.deploy_id
        }

    def publish_log(self, log: dict) -> Optional[str]:
        """Publish a log to Pub/Sub or print in dry-run mode."""
        
        message_data = json.dumps(log).encode("utf-8")
        
        if self.dry_run:
            print(f"[DRY-RUN] {log['severity']:5} | {log['service']:20} | {log['message'][:50]}")
            return None
        
        future = self.publisher.publish(self.topic_path, message_data)
        return future.result()

    def run(self, rps: int, seconds: int, profile: str = "normal"):
        """Run the generator at the specified rate."""
        
        total_logs = rps * seconds
        interval = 1.0 / rps if rps > 0 else 1.0
        
        print(f"\n{'='*60}")
        print(f"Starting log generation")
        print(f"  Profile: {profile}")
        print(f"  Rate: {rps} logs/sec")
        print(f"  Duration: {seconds} seconds")
        print(f"  Total logs: {total_logs}")
        print(f"  Deploy ID: {self.deploy_id}")
        print(f"{'='*60}\n")
        
        start_time = time.time()
        logs_sent = 0
        errors = 0
        
        try:
            for i in range(total_logs):
                loop_start = time.time()
                
                log = self.generate_log(profile)
                
                try:
                    self.publish_log(log)
                    logs_sent += 1
                except Exception as e:
                    errors += 1
                    print(f"Error publishing log: {e}")
                
                # Progress update every 10% or 100 logs
                if logs_sent % max(100, total_logs // 10) == 0:
                    elapsed = time.time() - start_time
                    actual_rps = logs_sent / elapsed if elapsed > 0 else 0
                    print(f"Progress: {logs_sent}/{total_logs} logs ({logs_sent*100//total_logs}%) | Actual RPS: {actual_rps:.1f}")
                
                # Rate limiting
                elapsed_loop = time.time() - loop_start
                sleep_time = interval - elapsed_loop
                if sleep_time > 0:
                    time.sleep(sleep_time)
                    
        except KeyboardInterrupt:
            print("\n\nInterrupted by user")
        
        elapsed = time.time() - start_time
        print(f"\n{'='*60}")
        print(f"Generation complete")
        print(f"  Logs sent: {logs_sent}")
        print(f"  Errors: {errors}")
        print(f"  Duration: {elapsed:.1f} seconds")
        print(f"  Actual RPS: {logs_sent/elapsed:.1f}" if elapsed > 0 else "N/A")
        print(f"{'='*60}")


def main():
    parser = argparse.ArgumentParser(description="CloudTrace Log Generator")
    parser.add_argument("--project", default="cloudtrace-481719", help="GCP Project ID")
    parser.add_argument("--topic", default="logs", help="Pub/Sub topic name")
    parser.add_argument("--rps", type=int, default=10, help="Logs per second")
    parser.add_argument("--seconds", type=int, default=60, help="Duration in seconds")
    parser.add_argument("--profile", choices=["normal", "error_burst", "latency_spike", "new_signature"],
                        default="normal", help="Traffic profile")
    parser.add_argument("--dry-run", action="store_true", help="Print logs instead of publishing")
    
    args = parser.parse_args()
    
    generator = LogGenerator(
        project_id=args.project,
        topic_id=args.topic,
        dry_run=args.dry_run
    )
    
    generator.run(
        rps=args.rps,
        seconds=args.seconds,
        profile=args.profile
    )


if __name__ == "__main__":
    main()
