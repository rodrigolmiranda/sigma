# Metrics Schema

| Table | Purpose | Key Fields |
|-------|---------|------------|
| metrics_daily | Aggregated engagement stats | date, tenantId, messages, activeUsers |
| poll_metrics | Poll performance | pollId, engagement_rate |
| guardian_metrics | Safety KPIs | violationId, triage_time_ms |
| rag_metrics | RAG usage | questionId, latency_ms |
| broadcast_metrics | Outreach | broadcastId, reach |

---
Navigation: [Home](home.md) | Previous: [Compliance Roadmap](compliance_roadmap.md) | Next: [Analytics Events Mapping](analytics_events_mapping.md)
