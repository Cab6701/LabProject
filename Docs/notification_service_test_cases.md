# Notification Service Test Cases

This file is the lab output checklist for acceptance and performance.

## Case 1 - Realtime for 1000 users

- Endpoint: `POST /api/notifications/send-bulk`
- Payload:
  - `userIds`: 1000 values
  - `channels`: `["email","firebase","inapp"]`
- Verify:
  - Worker logs consume all messages
  - Mongo history has expected records (`users * channels`)
  - No missing message IDs

## Case 2 - Campaign schedule (+1 minute)

- Create campaign with `scheduleTimeUtc = now + 1 minute`
- Verify:
  - scheduler picks due campaign
  - messages appear in Kafka topic
  - history records are written at expected time window

## Case 3 - Retry + DLQ

- Send notification with metadata `forceFail = email`
- Verify:
  - first attempt written as failed history
  - same message moves to retry topic with `attempt = 1`
  - second failure sends message to DLQ topic

## Case 4 - High load 10000 messages

- Run a loop from script or any load test tool.
- Verify:
  - service remains stable
  - no message loss
  - throughput target >= 5000 msg/min (cluster and local hardware dependent)

## Notes

- `processed_notifications` unique index guarantees idempotency by `(messageId,userId,channel)`.
- `notification_events` in ClickHouse is source for stats endpoints.
