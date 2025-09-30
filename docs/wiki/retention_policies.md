# Retention Policies

| Data Class | Default Retention | Rationale | Notes |
|------------|-------------------|----------|-------|
| Raw Message Content | 90 days | Minimise PII risk | Hash persists |
| Poll Votes | Lifetime | Analytical comparatives | Non-PII (hashed voter) |
| Guardian Violations | 1 year | Compliance evidence | Redacted after resolution + period |
| Embeddings | 180 days (rolling) | Cost mgmt & drift | Recompute on demand |

---
Navigation: [Home](home.md) | Previous: [Dashboards Spec](dashboards_spec.md) | Next: [Connectors Playbook](playbook_connectors.md)
