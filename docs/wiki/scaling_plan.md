# Scaling Plan

## Capacity Assumptions (Phase 1)

| Metric | Assumption |
|--------|------------|
| Avg messages/tenant/day | 10k |
| Peak ingest per second | 50 msg/s |
| Active polls concurrently | 200 |

## Triggers

| Trigger | Action |
|---------|--------|
| p95 ingest latency > 2s | Scale workers + DB IOPS review |
| Guardian queue backlog > 5m | Add worker instance |
| Vector store > planned size | Initiate compaction (Phase 2) |

## Phase 2 Additions

Redis caching for hot poll tallies; pgvector indexing; summarisation job scheduling.

---
Navigation: [Home](home.md) | Previous: [Class & Aggregate Diagrams](class_aggregate_diagrams.md) | Next: [Caching Strategy](caching_strategy.md)
