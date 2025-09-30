# Caching Strategy

| Layer | Data | Strategy | Invalidation |
|-------|------|----------|-------------|
| API (Phase 2) | Poll tallies | Redis key poll:{id}:tally | Vote event pub/sub |
| RAG | Embeddings lookups | In-memory LRU | TTL (6h) |
| Guardian | Rule set | In-memory snapshot | On rule change event |

---
Navigation: [Home](home.md) | Previous: [Scaling Plan](scaling_plan.md) | Next: [API Schema Evolution](api_schema_evolution.md)
