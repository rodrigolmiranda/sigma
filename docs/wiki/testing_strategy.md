# Testing Strategy

| Layer | Focus | Tools |
|-------|-------|-------|
| Unit | Handlers, utils | xUnit / NUnit |
| Integration | Repositories, GraphQL resolvers | Testcontainers (Postgres) |
| Contract | GraphQL schema snapshots | See [Contract Testing](contract_testing.md) |
| Performance | Ingest & RAG latency | k6 / Azure Load Testing |
| Security | AuthZ bypass, rate limit | Custom harness |

---
Navigation: [Home](home.md) | Previous: [Runbook: Cost Spike](runbook_cost_spike.md) | Next: [Contract Testing](contract_testing.md)
