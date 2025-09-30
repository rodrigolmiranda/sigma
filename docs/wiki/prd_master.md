# PRD Master

## Scope

Cross-platform chat intelligence & engagement: ingest, normalize, analyze, engage (polls/broadcasts), protect (guardian), augment (Ask‑DB).

## Modules & Phasing

Refer to: [Strategy & Phasing](strategy_phasing.md) & [Portal Sitemap](portal_sitemap.md).

## High-Level User Stories (Sample)

| ID | Story | Acceptance (Summary) | Phase |
|----|-------|----------------------|-------|
| POLL-01 | As a Community Manager I create a poll across platforms | Validates fields, emits `poll.created`, visible in dashboard | 1 |
| GUARD-05 | As a Moderator I resolve a flag | Status updated, `guardian.flag.resolved` emitted, audit logged | 1 |
| ASK-03 | As a User I ask a knowledge question | Embeddings generated, retrieval < 2.5s p95, answer cached | 2 |

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Performance | GraphQL depth & cost limits, p95 ingest < 2s from platform webhook -> persistence |
| Security | Envelope encryption, role claims enforcement, rate limits |
| Privacy | PII hashing, configurable retention, minimised data fields |
| Observability | 100% domain events logged (structured), OpenTelemetry traces |
| Scalability | Horizontal queue workers, phased caching (Redis Phase 2) |

## Open Questions (Track)

| Topic | Question | Target Resolution |
|-------|----------|-------------------|
| Poll Scheduling | Include in Phase 1? | After first 5 tenants |
| Multi-lingual Guardian | Require translation pipeline? | Pre Phase 2 ML |

---
Navigation: [Home](home.md) | Previous: [Jobs To Be Done](jobs_to_be_done.md) | Next: [Feature Acceptance Matrix](feature_acceptance_matrix.md)
