# HotelOS — Real-Time Microservices Hotel Management System

A .NET 8 implementation of a hotel management system built as **4 independent
microservices** that communicate **only** through a **RabbitMQ** message broker
(publish/subscribe), with a **SignalR** real-time operations dashboard.

> Academic context: BTEC architecture brief (4 services, message broker, WebSockets,
> three required algorithms) extended with custom requirements (accounts, room styles,
> keys/master keys, advance payments, 24‑hour cancellation policy, amenities, multi‑branch).

---

## 1. Architecture at a glance

```
                         ┌──────────────────────────────┐
                         │        RabbitMQ broker        │
                         │   topic exchange: hotel.events │
                         └───┬───────┬────────┬────────┬──┘
        publish/subscribe    │       │        │        │
              ┌──────────────┘       │        │        └──────────────┐
              ▼                      ▼        ▼                       ▼
      ┌──────────────┐     ┌──────────────┐ ┌──────────────┐  ┌──────────────┐
      │  Reception   │     │ Housekeeping │ │ Room Service │  │ Maintenance  │
      │   :5001      │     │   :5002      │ │   :5003      │  │   :5004      │
      │ reception.db │     │housekeeping.db│ │roomservice.db│  │maintenance.db│
      └──────────────┘     └──────────────┘ └──────────────┘  └──────────────┘
              │                      │        │                       │
              └──────────────┬───────┴────────┴───────────────────────┘
                             ▼
                   ┌────────────────────┐   WebSocket   ┌──────────────┐
                   │   Dashboard :5000  │ ════════════▶ │   Browser    │
                   │  (SignalR + UI)    │               │  live feed   │
                   └────────────────────┘               └──────────────┘
```

* **No service calls another service over HTTP.** All coordination is event-driven.
* **Database-per-service.** Each service owns a private SQLite database; data is
  never shared directly, only via event payloads.
* The **dashboard** subscribes to the entire event catalogue and pushes every
  event to browsers over WebSockets in real time.

---

## 2. Solution layout

```
HotelOS.sln
├── src/Shared/HotelOS.Shared        # contracts only: enums, events, event bus, algorithm interfaces
├── src/Services/HotelOS.Reception   # guests, rooms, bookings, keys, billing — owns Room Assignment + Billing
├── src/Services/HotelOS.Housekeeping# cleaning tasks, maintenance reporting
├── src/Services/HotelOS.RoomService # menu, orders, delivery
├── src/Services/HotelOS.Maintenance # priority-queue backlog of repairs
├── src/Gateway/HotelOS.Dashboard    # SignalR hub + web UI
├── tests/HotelOS.Algorithms.Tests   # xUnit tests for the 3 algorithms
└── infrastructure/                  # docker-compose (RabbitMQ) + config
```

---

## 3. Design patterns used

| Pattern | Where |
|---|---|
| **Observer** | `IEventBus` / `RabbitMqEventBus` + `IIntegrationEventHandler<T>` — services subscribe to events |
| **Facade** | `ReceptionFacade`, `HousekeepingFacade`, `RoomServiceFacade`, `MaintenanceFacade` |
| **Strategy** | `IRoomAssignmentStrategy` → `RoomAssignmentStrategy` |
| **Priority Queue (heap)** | `MaintenancePriorityQueue` over `PriorityQueue<,>` |
| **Factory** | `RoomKeyFactory` (normal + master keys) |
| **Repository (via EF Core)** | one `DbContext` per service |
| **DTO / immutable records** | every integration-event payload |

---

## 4. The three required algorithms

1. **Room Assignment** — `src/Services/HotelOS.Reception/Algorithms/RoomAssignmentStrategy.cs`
   Hard filter (style + Available + Clean), then lexicographic ranking:
   preferred floor → proximity → **longest clean duration** → room number.
   Fallback is automatic when a preference can't be met.
2. **Billing** — `.../Algorithms/BillingCalculator.cs`
   `nightly rate × nights + room service + extras` (no tax). Plus the **24-hour
   cancellation policy**: ≥24h → full refund, <24h → **50%**, after check-in → none.
3. **Maintenance Priority Queue** — `src/Services/HotelOS.Maintenance/Algorithms/MaintenancePriorityQueue.cs`
   Critical → High → Normal → Low, FIFO within a priority, stable via a sequence counter.

Run the tests:

```bash
dotnet test
```

---

## 5. Event catalogue (topics)

| Event (routing key) | Publisher | Subscriber(s) |
|---|---|---|
| `booking.created` | Reception | Dashboard |
| `booking.confirmed` | Reception | Dashboard |
| `booking.cancelled` | Reception | Dashboard |
| `guest.checkedin` | Reception | Dashboard |
| `guest.checkedout` | Reception | Room Service, Dashboard |
| `room.cleaning.requested` | Reception | Housekeeping, Dashboard |
| `room.cleaning.started` | Housekeeping | Dashboard |
| `room.cleaning.completed` | Housekeeping | Reception, Dashboard |
| `room.maintenance.requested` | Housekeeping | Maintenance, Dashboard |
| `maintenance.status.updated` | Maintenance | Dashboard |
| `maintenance.resolved` | Maintenance | Dashboard |
| `roomservice.order.placed` | Room Service | Dashboard |
| `roomservice.order.delivered` | Room Service | Reception, Dashboard |
| `key.issued` | Reception | Dashboard |
| `key.returned` | Reception | Dashboard |

Each event class carries its routing key in an `[EventKey("…")]` attribute, so the
contract and topic can never drift.

---

## 6. Running it

### Prerequisites
* .NET 8 SDK
* Docker (for RabbitMQ)

### Steps

```bash
# 1) Start the broker
cd infrastructure
docker compose up -d
# RabbitMQ management UI → http://localhost:15672  (guest / guest)

# 2) Build & test
cd ..
dotnet build
dotnet test

# 3) Run everything (4 services + dashboard)
./run-local.sh
```

Then open:

* **Dashboard (live):** http://localhost:5000
* **Reception API (Swagger):** http://localhost:5001/swagger
* Housekeeping: http://localhost:5002/swagger · Room Service: http://localhost:5003/swagger · Maintenance: http://localhost:5004/swagger

Databases (`*.db`) and 10 seeded rooms across 2 floors are created automatically on first run.

---

## 7. End-to-end demo flow

Watch the dashboard while you call these (Reception Swagger or curl):

1. **Create a booking** → `POST :5001/api/bookings`
   ```json
   {
     "guest": { "fullName": "Jane Doe", "email": "jane@x.com", "phoneNumber": "+100", "nationalId": "A1" },
     "style": 1,
     "checkIn": "2026-07-01",
     "checkOut": "2026-07-04",
     "preferredFloor": 2,
     "advancePayment": 120
   }
   ```
   → emits `booking.created` + `booking.confirmed`; response shows the assigned room and *why*.
2. **Check in** → `POST :5001/api/bookings/{id}/checkin` → `guest.checkedin` + `key.issued`.
3. **Order room service** → `POST :5003/api/orders` then `POST :5003/api/orders/{id}/deliver`
   → `roomservice.order.delivered`; Reception adds the charge to the bill.
4. **Check out** → `POST :5001/api/bookings/{id}/checkout`
   → final bill returned; emits `guest.checkedout` + `room.cleaning.requested`.
5. **Housekeeping** → `GET :5002/api/cleaning/tasks`, then `start` and `complete`
   → `room.cleaning.completed`; Reception flips the room back to Clean/Available.
6. **Report a fault** → `POST :5002/api/cleaning/maintenance-requests` (priority 0 = Critical)
   → `room.maintenance.requested`; see it ordered in `GET :5004/api/maintenance/queue`.

---

## 8. Mapping to the brief

| Requirement | Where |
|---|---|
| Exactly 4 microservices, no direct HTTP | `src/Services/*`, all coordination via `IEventBus` |
| Message broker pub/sub | RabbitMQ topic exchange `hotel.events` |
| WebSockets real-time dashboard | `HotelOS.Dashboard` (SignalR) |
| Room Assignment / Billing / Priority Queue | Section 4 above + unit tests |
| Accounts (Guest/Receptionist/Housekeeper/System) | Guest entity, Reception & Housekeeping actions, system-published events |
| Room styles + keys + master key | `RoomStyle` enum, `RoomKeyFactory` |
| 10 rooms / 2 floors, search & book, advance payment | `ReceptionSeeder`, `ReceptionFacade` |
| 24-hour cancellation refunds | `BillingCalculator.CalculateRefund` |
| Extra services (amenities) | Room Service menu + `BookingExtra` |
| Multiple branches | `BranchId` on rooms/bookings/events |
| Guest↔Booking, receptionist & housekeeper actions | EF relationships + facades |
