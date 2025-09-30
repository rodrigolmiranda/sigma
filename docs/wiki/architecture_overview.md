# Architecture Overview

## Stack

.NET 10 (C# 14), GraphQL (Hot Chocolate), EF Core + PostgreSQL, Azure Queues/Functions, Blob, Key Vault, App Insights, OpenTelemetry, optional Redis (Phase 2), pgvector (Phase 2).

## Patterns

* Clean Architecture layers
* Lightweight CQRS (command/query handlers)
* Outbox pattern for reliable events
* DataLoader for N+1 mitigation
* Persisted GraphQL queries with complexity & depth limits

## Deployment

Blue/Green in Azure App Service + Functions; Infrastructure scaling captured in [Scaling Plan](scaling_plan.md).

---
Navigation: [Home](home.md) | Previous: [UX Patterns](ux_patterns.md) | Next: [Sequence Diagrams](sequence_diagrams.md)
