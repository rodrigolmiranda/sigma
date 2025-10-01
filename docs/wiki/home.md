# SIGMA Product & Technology Wiki

Welcome to the consolidated, scope-aligned SIGMA documentation. This wiki replaces prior scattered docs (PRD, Architecture, Security, GTM, etc.) with a navigable, versionable single source of truth. All pages live flat in this folder for GitHub Wiki parity.

## Navigation (Ordered)

1. [Vision Manifesto](vision_manifesto.md)
2. [Strategy & Phasing](strategy_phasing.md)
3. [Personas](personas.md)
4. [Jobs To Be Done](jobs_to_be_done.md)
5. [PRD Master](prd_master.md)
6. [Feature Acceptance Matrix](feature_acceptance_matrix.md)
7. [Pricing Model](pricing_model.md)
8. [KPI ↔ Metric Mapping](kpi_metric_mapping.md)
9. [Competitor Playbook](competitor_playbook.md)
10. [Domain Model Overview](domain_model_overview.md)
11. [Event Catalog](event_catalog.md)
12. [State Transitions](state_transitions.md)
13. [ER Diagram](er_diagram.md)
14. [Portal Sitemap](portal_sitemap.md)
15. [RBAC Matrix](rbac_matrix.md)
16. Page Contracts: [Dashboard](page_contract_dashboard.md), [Poll Create](page_contract_poll_create.md), [Guardian Queue](page_contract_guardian_queue.md)
17. [Component Library](component_library.md)
18. [UX Patterns](ux_patterns.md)
19. [Architecture Overview](architecture_overview.md)
20. [Sequence Diagrams](sequence_diagrams.md)
21. [Class & Aggregate Diagrams](class_aggregate_diagrams.md)
22. [Scaling Plan](scaling_plan.md)
23. [Caching Strategy](caching_strategy.md)
24. [API Schema Evolution](api_schema_evolution.md)
25. [Security & Privacy Controls](sec_privacy_controls.md)
26. [Incident Response](incident_response.md)
27. [Compliance Roadmap](compliance_roadmap.md)
28. [Metrics Schema](metrics_schema.md)
29. [Analytics Events Mapping](analytics_events_mapping.md)
30. [Dashboards Spec](dashboards_spec.md)
31. [Retention Policies](retention_policies.md)
32. Playbooks: [Connectors](playbook_connectors.md), [Polling](playbook_polling_module.md), [Guardian](playbook_guardian_module.md), [RAG (Ask‑DB)](playbook_rag_module.md), [Broadcasts](playbook_broadcasts.md)
   - 🚀 **Quick Start:** [5-Min Connector Test](quick_test_connectors.md)
   - 📱 **Setup Guides:** [Telegram](setup_guide_telegram.md) | [WhatsApp](setup_guide_whatsapp.md)
33. Runbooks: [Ingest Lag](runbook_ingest_lag.md), [Queue Saturation](runbook_queue_saturation.md), [Vector Store Bloat](runbook_vector_store_bloat.md), [Cost Spike](runbook_cost_spike.md)
34. [Testing Strategy](testing_strategy.md)
35. [Contract Testing](contract_testing.md)
36. [Quality Gates](quality_gates.md)
37. [Governance & Change Control](governance_change_control.md)
38. [RFC Template](rfc_template.md)

## Conventions

* Every page ends with a navigation footer.
* Event names: lowercase.dotted (e.g., `poll.vote.recorded`).
* File naming: snake_case, no spaces.
* Roles: Owner, Admin, Moderator, Analyst, Viewer (limited read), System (internal automation).
* Avoid disallowed legacy LMS/SCORM terminology.

## Change Workflow

1. Propose: create new RFC via `rfc_template.md` copy -> PR.
2. Review: at least 1 product + 1 engineering approval.
3. Merge: update affected wiki pages atomically.
4. Tag: increment documentation version (doc-vX.Y) in `governance_change_control.md`.

## High-Level Modules

| Module | Purpose | Phase | Add-on? | Notes |
|--------|---------|-------|---------|-------|
| Connectors | Multi-platform chat ingest & normalization | 1 | Core | Slack, Discord, Telegram (YouTube opt-in, WhatsApp limited) |
| Polls | Lightweight engagement across platforms | 1 | Core | Unified vote tally & analytics |
| Guardian | Safety rules & future ML classification | 1 | Add-on (pro) | Rule engine now, ML Phase 2 |
| Ask‑DB (RAG) | Knowledge Q&A over curated sources | 2 | Add-on | Vector store, retrieval orchestration |
| Broadcasts | Multi-platform outbound messaging | 2 | Add-on | Rate limiting & per-platform adapters |
| Analytics | Cross-module KPIs & dashboards | 1 | Core | Metrics store, aggregations |
| Admin/Billing | Tenancy, roles, plans, invoices | 1 | Core | RBAC & metering |

---
Navigation: Home | Next: [Vision Manifesto](vision_manifesto.md)
