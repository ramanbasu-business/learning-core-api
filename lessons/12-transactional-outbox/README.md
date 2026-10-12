# 12 - Transactional outbox

**Problem:** "save to the database, then publish an event" is two systems and cannot be one transaction. If the process crashes or the broker is down between the two steps, you have an order nobody heard about (or an event about an order that never saved).

## Run it

```bash
cd lessons/12-transactional-outbox
dotnet run
```

## Naive

`Naive.cs` saves each order, then calls `FakeBroker.PublishAsync`. The fake broker fails every 10th call, so 10 of 100 events are lost. The note under the table shows it.

## The pattern

1. In the **same** `SaveChanges`, insert the business row (order) and an **outbox row** (`message_queue`, status `pending`). One database transaction, so both or neither.
2. A separate **relay** (background worker) reads pending outbox rows, publishes them, and marks them `processed`. Failures stay pending and are retried.

Delivery becomes **at-least-once**, so consumers must be idempotent (handle the same message twice safely).

## Your turn

Edit `Fixed.cs` following the two-part comment.

## Takeaways

- You can't make a DB write and a broker publish atomic; you make the *intent to publish* atomic.
- The outbox table is the source of truth; the broker is best effort.
- Real systems poll with `FOR UPDATE SKIP LOCKED` so several relays don't take the same message.

## Interview questions

- Why can't you just wrap SaveChanges and a publish in a transaction?
- At-least-once vs exactly-once delivery? How do you make consumers idempotent?
- What happens if the relay crashes after publishing but before marking processed?
