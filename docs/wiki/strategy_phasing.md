# Strategy & Phasing

## Phase Overview

| Phase | Focus | Key Modules | Primary Risk Mitigation | Exit Criteria |
|-------|-------|------------|-------------------------|---------------|
| 1 | Core ingest, analytics, polls, guardian rules | Connectors, Polls, Guardian (rules), Analytics, Admin | Scope creep into LMS | 10 paying tenants; stable ingestion SLA |
| 2 | Knowledge & outbound expansion | Ask‑DB (RAG), Broadcasts, ML Guardian | Cost of embeddings & vector growth | RAG latency < 2.5s p95; broadcast success > 98% |
| 3 | Enterprise maturity | Teams connector, deeper compliance, multi‑region | Latency & compliance overhead | SOC2 Type I readiness; 99.9% uptime quarter |

## Decision Gates

| Gate | Criteria | Action if Fail |
|------|----------|---------------|
| G1 (end Phase 1) | Tenants >= 10, WAU retention >= 70% | Extend Phase 1 hardening |
| G2 (mid Phase 2) | RAG adoption >= 40% of active tenants | Adjust pricing / reposition |
| G3 (pre Phase 3) | Support load manageable (<1 FTE / 15 tenants) | Invest in tooling / docs |

## KPIs by Phase

| KPI | Phase 1 Anchor | Phase 2 Uplift Driver |
|-----|----------------|-----------------------|
| Expansion Revenue | Polls usage → upsell | Ask‑DB queries volume |
| Retention | Poll stickiness | RAG value + broadcasts |
| Cost Efficiency | Optimized ingest pipeline | Vector store lifecycle mgmt |

## Risk Register (Top 5)

| Risk | Phase | Impact | Likelihood | Mitigation |
|------|-------|--------|------------|------------|
| Overbuild early ML | 1 | Delayed GA | Medium | Defer to Phase 2 |
| Vector cost runaway | 2 | Margin erosion | Medium | Embedding dedupe + TTL |
| RBAC ambiguity | 1 | Security holes | Low | Finalize matrix pre-impl |
| Multi-region complexity | 3 | Ops overhead | Medium | Blue/Green + feature flags |
| Pricing confusion | 2 | Lost upsell | Medium | Clear add-on gating matrix |

## Timeline (Indicative)

| Month | Milestone |
|-------|-----------|
| 1 | Phase 1 GA (foundational) |
| 3 | Guardian rule tuning stable |
| 5 | RAG beta |
| 6 | Broadcasts beta |
| 8 | Phase 2 GA |
| 12 | Phase 3 kickoff |

---
Navigation: [Home](home.md) | Previous: [Vision Manifesto](vision_manifesto.md) | Next: [Personas](personas.md)
