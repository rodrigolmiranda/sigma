# Runbook: Cost Spike

| Symptom | Cause | Mitigation |
|---------|-------|-----------|
| Unexpected vector cost | Excess queries | Introduce caching TTL |
| High egress | Broadcast spike | Rate limit + batching |

---
Navigation: [Home](home.md) | Previous: [Runbook: Vector Store Bloat](runbook_vector_store_bloat.md) | Next: [Testing Strategy](testing_strategy.md)
