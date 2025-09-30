# Event Catalog

Canonical domain & integration events. All names lowercase.dotted.

| Event | Phase | Producer | Purpose | Key Fields (excerpt) | Consumers |
|-------|-------|----------|---------|----------------------|-----------|
| message.ingested | 1 | Ingest Worker | Persist normalized message | messageId, platform, tenantId, ts | Analytics, Guardian |
| poll.created | 1 | API | New poll created | pollId, tenantId, ts | Analytics |
| poll.vote.recorded | 1 | API | Vote recorded | pollId, optionId, voterHash, ts | Analytics |
| guardian.flag.raised | 1 | Guardian Pipeline | Potential violation | violationId, severity, ruleId | Guardian Queue, Analytics |
| guardian.flag.resolved | 1 | Guardian Queue | Flag triaged | violationId, resolution, ts | Analytics |
| askdb.question.asked | 2 | API | RAG question issued | questionId, tenantId | Retrieval Orchestrator |
| askdb.answer.produced | 2 | Retrieval Orchestrator | Answer ready | questionId, latencyMs | Analytics |
| broadcast.dispatched | 2 | Broadcast Service | Outbound fan-out initiated | broadcastId, platforms[] | Analytics |
| broadcast.delivery.result | 2 | Platform Adapter | Per-platform status | broadcastId, platform, status | Analytics |

## Event Conventions

* Immutable payloads (no mutation after publish).
* Timestamps in ISO 8601 UTC.
* Sensitive fields hashed or redacted before emission.

---
Navigation: [Home](home.md) | Previous: [Domain Model Overview](domain_model_overview.md) | Next: [State Transitions](state_transitions.md)
