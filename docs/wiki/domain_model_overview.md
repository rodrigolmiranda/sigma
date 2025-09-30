# Domain Model Overview

## Aggregates

| Aggregate | Entities | Key Invariants |
|-----------|---------|----------------|
| Tenant | Users, Plans, Settings | Active plan required for feature enable |
| MessageStream | Messages (normalized) | Message immutable post-ingest |
| Poll | PollOptions, PollVotes | Options unique; vote idempotency (option + voter hash) |
| GuardianViolation | EvidenceItems | Status transitions controlled sequence |
| KnowledgeSource | Chunks, Embeddings | Chunk size limit; TTL for stale embeddings |
| Broadcast | BroadcastTargets, DeliveryResults | Only platforms enabled & authorized |

## Relationships (High-Level)

* Tenant 1..* Users
* Tenant 1..* Polls
* Poll 1..\* PollOptions; Poll 1..\* PollVotes
* MessageStream 1..* Messages
* GuardianViolation references Message (by ID) + Rule
* KnowledgeSource 1..* Chunks

## Invariants Enforcement Strategy

* Command Handlers validate business rules.
* Repository layer enforces unique constraints (options, votes).
* Outbox ensures domain event durability.

---
Navigation: [Home](home.md) | Previous: [Competitor Playbook](competitor_playbook.md) | Next: [Event Catalog](event_catalog.md)
