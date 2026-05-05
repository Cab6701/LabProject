# Notification Service LAB - Learning + Implementation Roadmap

Tai lieu nay chia cong viec theo giai doan de ban vua hoc vua lam, phu hop cho nguoi moi da biet backend API co ban.

## Cach dung tai lieu

- Moi giai doan co 4 phan: `Muc tieu`, `Kien thuc can hoc`, `Cong viec can lam`, `Definition of Done`.
- Lam theo thu tu tu Giai doan 0 den 6.
- Cuoi moi ngay, cap nhat tien do theo 3 cau hoi:
  - Hom nay da hoc duoc gi?
  - Hom nay da chay duoc gi?
  - Van de lon nhat can go o buoc tiep theo la gi?

---

## Giai doan 0 - Chuan bi moi truong (1-2 ngay)

### Muc tieu
Dung duoc full local stack va khoi tao solution dung huong.

### Kien thuc can hoc truoc
- Docker va docker compose co ban.
- .NET solution nhieu project.
- `BackgroundService` co ban.

### Cong viec can lam
- Tao solution gom: `LabProject.Domain`, `LabProject.Application`, `LabProject.Infrastructure`, `LabProject.Api`, `LabProject.Worker`.
- Tao `docker-compose.yaml` de chay Kafka, MongoDB, ClickHouse.
- Cau hinh API va Worker chay local.
- Them health check endpoint cho API (va worker neu can).

### Definition of Done
- `docker compose up` chay on dinh.
- API start duoc, goi health endpoint tra ve thanh cong.
- Worker start duoc, khong loi cau hinh.

---

## Giai doan 1 - Realtime flow co ban (2-3 ngay)

### Muc tieu
Gui request vao API va Worker nhan duoc message tu Kafka.

### Kien thuc can hoc truoc
- Kafka: topic, partition, consumer group, offset.
- Producer/Consumer trong .NET.
- Validate request co ban.

### Cong viec can lam
- Dinh nghia message contract JSON (messageId, userId, channels, attempt, source, createdAt, metadata).
- Tao endpoint `POST /api/notifications/send`.
- Tu API publish message len `notification-topic`.
- Worker consume tu topic va log thong tin message.

### Definition of Done
- Goi API 1 lan -> Worker nhan du message.
- Request sai format bi validate va tra ve loi dung.
- Co log de trace duoc `messageId`.

---

## Giai doan 2 - Multi-channel + Mongo history (3-4 ngay)

### Muc tieu
Xu ly da kenh va luu lich su gui thong bao vao MongoDB.

### Kien thuc can hoc truoc
- Strategy Pattern.
- MongoDB CRUD va index co ban.

### Cong viec can lam
- Tao `INotificationChannel` va 3 channel strategy:
  - FirebaseChannel (mock)
  - EmailChannel (mock)
  - InAppChannel (ghi Mongo)
- Worker xu ly tung channel trong message.
- Luu `NotificationHistory` theo tung `(messageId, userId, channel)`.

### Definition of Done
- 1 message co 2 channels -> tao 2 ban ghi history.
- InApp notification luu vao collection rieng.
- Worker van chay on dinh khi 1 channel fail (chua retry o phase nay).

---

## Giai doan 3 - Idempotency + Retry + DLQ (3-4 ngay)

### Muc tieu
Khong xu ly trung va co luong retry/dlq dung de bai.

### Kien thuc can hoc truoc
- At-least-once delivery.
- Idempotent consumer.
- Offset commit strategy.

### Cong viec can lam
- Tao collection `ProcessedNotifications` va unique index:
  - `(messageId, userId, channel)`
- Check idempotency truoc khi send.
- Neu fail lan 1 -> publish sang `notification-retry` (attempt + 1).
- Neu fail tiep -> publish sang `notification-dlq`.
- Commit offset sau khi xu ly an toan.

### Definition of Done
- Message duplicate khong bi gui lai.
- Quan sat duoc flow: main topic -> retry -> dlq.
- Khong co vong lap retry vo han.

---

## Giai doan 4 - Campaign + Scheduler (2-3 ngay)

### Muc tieu
Ho tro gui theo lich va lap lai theo `repeatType`.

### Kien thuc can hoc truoc
- Scheduler voi `BackgroundService`.
- Xu ly thoi gian UTC.

### Cong viec can lam
- Tao API:
  - `POST /api/campaigns`
  - `PATCH /api/campaigns/{id}/enabled`
  - `GET /api/campaigns/{id}` (tuy chon)
- Luu campaign Mongo voi `nextRunAt`, `enabled`, `repeatType`.
- Scheduler quet campaign den han va publish message vao `notification-topic`.
- Cap nhat `nextRunAt` theo `repeatType` 0-3.

### Definition of Done
- Tao campaign +1 phut -> tu dong phat message dung gio.
- Tat campaign -> scheduler bo qua.
- Campaign repeat chay dung chu ky.

---

## Giai doan 5 - ClickHouse + Stats API (2-3 ngay)

### Muc tieu
Thong ke du lieu gui thong bao theo yeu cau de bai.

### Kien thuc can hoc truoc
- Event table append-only.
- Aggregate query co ban.

### Cong viec can lam
- Tao bang `notification_events` tren ClickHouse.
- Worker ghi event sau moi lan gui (success/fail).
- Tao 4 endpoint:
  - `GET /stats/total`
  - `GET /stats/by-channel`
  - `GET /stats/success-rate`
  - `GET /stats/by-driverCode`

### Definition of Done
- So lieu stats khop voi history mau.
- Co filter thoi gian `from/to`.
- Du lieu moi duoc cap nhat sau khi worker xu ly.

---

## Giai doan 6 - Hieu nang + README hoan chinh (2 ngay)

### Muc tieu
Dat muc tieu throughput cua lab va hoan thien tai lieu.

### Kien thuc can hoc truoc
- Concurrency control (`SemaphoreSlim`).
- Logging va theo doi basic metrics.

### Cong viec can lam
- Tang parallelism worker co kiem soat.
- Test 4 case trong de:
  - Realtime 1000 users
  - Campaign +1 phut
  - Retry + DLQ
  - High load 10k
- Hoan thien structured logging.
- Viet README huong dan setup, run, test.

### Definition of Done
- Chay du 4 case va co bang chung log/data.
- Throughput dat muc tieu de ra (hoac ghi ro gioi han hien tai).
- Nguoi khac clone repo co the tu chay theo README.

---

## Checklist hang ngay (ap dung xuyen suot)

- Hoc ly thuyet dung chu de phase hien tai: 60-90 phut.
- Code va test: 3-5 gio.
- Tong ket cuoi ngay:
  - Da hoan thanh task nao?
  - Co blocker nao?
  - Ke hoach ngay mai la gi?

---

## Thu tu uu tien neu bi qua tai

Neu ban thay nhieu qua, tap trung theo thu tu toi thieu sau:

1. Realtime publish/consume chay duoc.
2. History Mongo dung va ro rang.
3. Idempotency + retry + dlq.
4. Campaign scheduler.
5. Stats ClickHouse.
6. Performance tuning va README.

