# Runbook: Vector Store Bloat

| Symptom | Cause | Action |
|---------|-------|-------|
| Storage growth > plan | Stale embeddings | Run compaction job |
| Latency increase | Oversized index | Rebuild w/ pruning |

---
Navigation: [Home](home.md) | Previous: [Runbook: Queue Saturation](runbook_queue_saturation.md) | Next: [Runbook: Cost Spike](runbook_cost_spike.md)
