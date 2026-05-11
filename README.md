# LabProject Notification Service

Distributed notification service using `.NET`, `Kafka`, `MongoDB`, and `ClickHouse`.

## Architecture

- `LabProject.Api` is producer + scheduler host.
- `LabProject.Worker` is consumer + channel dispatcher.
- Kafka topics:
  - `notification-topic`
  - `notification-retry`
  - `notification-dlq`
- MongoDB stores:
  - campaign (`notification_campaigns`)
  - history (`notification_history`)
  - idempotency marker (`processed_notifications`)
  - in-app notifications (`in_app_notifications`)
- ClickHouse stores analytics event table: `notification_events`.

## Run locally

1. Start infra:
   - `docker compose up -d`
2. Build:
   - `dotnet build LabProject.slnx`
3. Run API:
   - `dotnet run --project LabProject.Api`
4. Run Worker:
   - `dotnet run --project LabProject.Worker`

## API quick test

### Realtime single user

`POST /api/notifications/send`

```json
{
  "userId": "u001",
  "channels": ["email", "firebase", "inapp"],
  "source": "manual",
  "metadata": {
    "title": "hello",
    "body": "world",
    "driverCode": "driver-a"
  }
}
```

### Realtime multi user

`POST /api/notifications/send-bulk`

```json
{
  "userIds": ["u001", "u002", "u003"],
  "channels": ["email", "inapp"],
  "source": "bulk-manual",
  "metadata": {
    "title": "bulk title",
    "body": "bulk body",
    "driverCode": "driver-b"
  }
}
```

### Campaign

`POST /api/campaigns`

```json
{
  "name": "daily-campaign",
  "userIds": ["u001", "u002"],
  "channels": ["inapp", "email"],
  "metadata": {
    "title": "campaign",
    "body": "scheduled push",
    "driverCode": "driver-c"
  },
  "scheduleTimeUtc": "2026-05-11T09:30:00Z",
  "repeatType": 1,
  "enabled": true,
  "source": "campaign"
}
```

Enable/disable:
- `PATCH /api/campaigns/{id}/enabled`

## Retry + DLQ test

Force channel failure by metadata:

```json
{
  "userId": "u900",
  "channels": ["email"],
  "source": "retry-test",
  "metadata": {
    "forceFail": "email",
    "driverCode": "driver-fail"
  }
}
```

Flow expected:
- first fail -> publish to `notification-retry`
- second fail -> publish to `notification-dlq`

## Analytics API

- `GET /stats/total`
- `GET /stats/by-channel`
- `GET /stats/success-rate`
- `GET /stats/by-driverCode`

Supports optional query:
- `from=2026-05-11T00:00:00Z`
- `to=2026-05-11T23:59:59Z`
