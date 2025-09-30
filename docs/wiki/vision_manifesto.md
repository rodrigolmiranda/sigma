# Vision Manifesto

## Problem

Communities and educational cohorts operate across fragmented chat platforms creating visibility gaps, weak safety oversight, and low-signal engagement metrics.

## Vision

Deliver a unified, privacy-conscious intelligence and light engagement layer that normalizes multi-platform chat streams, surfaces actionable signals, enables safe participation, and augments group knowledge without locking users into a proprietary chat surface.

## Positioning Statement

For community & learning operators who need insight and safe engagement across Slack/Discord/Telegram, SIGMA is a cross‑platform chat intelligence & activation platform that unifies analytics, polls, safety triage, and knowledge queries—without becoming an LMS.

## Differentiators

* Platform-Agnostic Normalization (no forced migration)
* Privacy‑by‑Design (hashing, encryption envelopes, minimised PII)
* Add‑on Upsell Path (Guardian, Ask‑DB, Broadcasts) layered over core analytics
* Role & Event Traceability (strict RBAC + domain event lineage)
* Lightweight Engagement (polls, broadcasts) vs heavy course structures

## Non‑Goals

* Hosting curricular content or SCORM artifacts
* Replacing native chat UX with a full alternate client
* Becoming a generic ticketing/moderation queue system

## Success Metrics (North Star + Support)

| Metric | Definition | Target (Phase 1) | Target (Phase 2) |
|--------|------------|------------------|------------------|
| Weekly Active Tenants | Tenants w/ >= 1 meaningful event (poll vote, guardian flag, dashboard view) | 50 | 200 |
| Poll Engagement Rate | (Unique voters / Eligible members) | 25% | 35% |
| Mean Flag Triage Time | From flag raised → status resolved | < 4h | < 2h |
| RAG Answer Adoption | (% questions receiving accepted answer) | - | 70% |
| Net Retention | Expansion vs churn | 100% | 115% |

## Guiding Principles

1. Integrity of Source Context: Preserve message provenance.
2. Minimise Data Gravity: Store only what’s necessary for value & compliance.
3. Observability First: Instrument domain events before feature expansion.
4. Progressive Capability: Core first, opt‑in advanced modules later.
5. Performance Budgeting: Guardrail complexity early (GraphQL depth, query cost).

---
Navigation: [Home](home.md) | Previous: Home | Next: [Strategy & Phasing](strategy_phasing.md)
