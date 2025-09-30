# SIGMA Documentation Scaffolding Plan (Draft)

Status: Draft for review (do not implement yet)  
Date: 2025-09-28

## 1. Assessment of Current Core Docs

| Doc | Scope Clarity | Strengths | Gaps / Ambiguities | Action (Later) |
|-----|---------------|-----------|--------------------|----------------|
| PRD_GroupIntelligence.md | High (well-focused on multi-platform chat intelligence & light engagement) | Clear phased platforms, modules & workflows | Lacks explicit event model, persona empathy maps, feature-level acceptance tests matrix | Expand with events map + persona deep dive + acceptance matrix |
| Architecture_Technical.md | High (clean architecture & CQRS explained) | Solid layering, performance & security controls | Missing sequence diagrams, class/aggregate relationships diagram, scaling topology diagrams | Add diagrams & sizing models |
| Marketing_GTM_Business.md | High (positioning, pricing, GTM phases) | Clear pricing levers & ICP | Could align KPIs to product metrics data model; no narrative to feature mapping | Add KPI ↔ metric mapping table |
| Competitors_SWOT_Matrix.md | High | Differentiators explicit | No threat response playbook | Add mitigation playbook appendix |
| Security_Privacy_Compliance.md | High | Concrete controls list | Missing RACI & data classification table | Add RACI, data class matrix |
| Connectors_WhatsApp_Telegram.md | (Not read here) assumed medium | Likely deep technical detail | Need unified canonical MessageEvent schema & error taxonomy reference pointer | Cross-link in event catalog |

## 2. Documentation Pyramid (Top → Bottom)

1. Vision & Strategy Layer
2. Product Definition Layer (Personas, JTBD, PRD core)
3. Business & Commercial Layer (GTM, Pricing, KPIs)
4. Domain & Event Model Layer
5. UX & Portal Specification Layer
6. Technical Architecture & Design Layer
7. Operational & Security Layer
8. Data & Analytics Layer
9. Implementation Playbooks & Runbooks
10. Testing, Quality & Governance Layer

Each layer produces structured artefacts; lower layers reference upper layers; cross-links maintained.

## 3. Proposed Document Set & Hierarchy

### 3.1 Vision & Strategy

- `vision_manifesto.md` — concise narrative (problem, vision, positioning one-pager)
- `strategy_phasing.md` — phases, decision gates, KPIs per phase, exit criteria

### 3.2 Product Definition

- `personas.md` — detailed persona cards (Professor, Community Manager, Enterprise IT)
- `jobs_to_be_done.md` — JTBD forces diagram & outcome metrics
- `prd_master.md` — merges existing PRD (refactor: keep core, link out to modules docs)
- `feature_acceptance_matrix.md` — per feature: user story, acceptance criteria, phase, KPIs influenced

### 3.3 Business & Commercial

- `pricing_model.md` — formulas, retention multiplier logic, add-on monetisation triggers
- `kpi_metric_mapping.md` — KPI ↔ product event/metric source (ties to analytics schema)
- `competitor_playbook.md` — counter-messaging & quick response patterns (extends SWOT)

### 3.4 Domain & Event Model

- `domain_model_overview.md` — aggregates, relationships, invariants
- `event_catalog.md` — canonical events (domain + integration) with name, purpose, payload, producer, consumers
- `state_transitions.md` — lifecycle diagrams (Message, Poll, Violation, KnowledgeSource)
- `er_diagram.md` — logical ER (Mermaid) + table to aggregate mapping

### 3.5 UX & Portal Specification

- `portal_sitemap.md` — site map, modules, pages, menu tree (with IDs)
- `rbaс_matrix.md` — RBAC per module/page/action; add-on gating flags
- `page_contracts/` (folder)
  - `{pageId}.md` — purpose, upstream events, downstream events, fields, UI components, validation rules, masking/encryption, error states, UX patterns
- `component_library.md` — design tokens, component definitions (table, list, chart, modal)
- `ux_patterns.md` — patterns (onboarding wizard, poll creation wizard, guardian triage queue UI)

### 3.6 Technical Architecture & Design

- `architecture_overview.md` — high level refinement (links existing Architecture doc)
- `sequence_diagrams.md` — key flows (ingest pipeline, poll vote, guardian flag, Ask‑DB answer, broadcast dispatch)
- `class_aggregate_diagrams.md` — UML / simplified aggregate vs repository boundaries
- `scaling_plan.md` — capacity assumptions, sizing formulas, cost model per phase
- `caching_strategy.md`
- `api_schema_evolution.md` — versioning & deprecation process details

### 3.7 Operational & Security

- `sec_privacy_controls.md` — expand existing with RACI, data classification, threat model summary
- `runbooks/` — per operational scenario (ingest lag, queue saturation, vector store bloat, cost spike)
- `incident_response.md` — severity matrix, comms templates
- `compliance_roadmap.md` — ISO27001 → SOC2 timeline

### 3.8 Data & Analytics

- `metrics_schema.md` — table definitions for metrics_daily etc.
- `analytics_events_mapping.md` — product events → metrics ETL mapping
- `dashboards_spec.md` — each dashboard: tiles, query sources, latency, refresh rules
- `retention_policies.md` — per data class retention & purge method

### 3.9 Implementation Playbooks

- `playbook_connectors.md`
- `playbook_polling_module.md`
- `playbook_guardian_module.md`
- `playbook_rag_module.md`
- `playbook_broadcasts.md`

Each playbook: prerequisites, steps, test harness pointers, rollout/rollback, instrumentation.

### 3.10 Testing & Quality

- `testing_strategy.md` — unit/integration/e2e/perf/security mapping
- `contract_testing.md` — GraphQL schema snapshot process
- `quality_gates.md` — CI gates & thresholds

## 4. Event Catalog Initial Skeleton

(Will be filled later; illustrate format)

| Event | Type | Phase | Trigger | Producer | Key Fields | Consumers | Notes |
|-------|------|-------|---------|----------|------------|-----------|-------|
| message.ingested | domain | 1 | Platform webhook validated | ingest worker | messageId, platform, channelId, tenantId, ts | analytics rollup, guardian | Immutable base event |
| poll.created | domain | 1 | CreatePoll command success | API | pollId, creatorId, tenantId, ts | poll dispatcher, analytics | Persisted before dispatch |
| poll.vote.recorded | domain | 1 | Vote received | API | pollId, optionId, voterHash, ts | analytics | Dedup via option+voterHash |
| guardian.flag.raised | domain | 1 | Rule/ML triggers | guardian pipeline | violationId, severity, ruleId | moderator queue, analytics | Evidence stored separately |
| askdb.question.asked | domain | 2 | Ask question mutation | API | questionId, tenantId, userId | retrieval orchestrator, analytics | Text hashed for PII variant |
| broadcast.dispatched | domain | 2 | Send broadcast | broadcast service | broadcastId, platforms[], ts | analytics | Track per-platform result separately |

## 5. RBAC Matrix (High-Level Draft)

| Module/Page | Owner | Admin | Moderator | Analyst | Viewer |
|-------------|-------|-------|----------|---------|--------|
| Dashboard | M | M | R (limited stats) | R | R |
| Connectors | M | M | - | - | - |
| Polls (create/manage) | M | M | C (close only) | R | - |
| Guardian Rules | M | M | S (suggest) | - | - |
| Guardian Queue | M | M | M | R (redacted) | R (more redacted) |
| Ask‑DB Sources | M | M | S | R | - |
| Broadcasts | M | M | - | D (draft) | - |
| Analytics Reports | M | M | R (limited) | M | R |
| Users & Roles | M | M | - | - | - |
| Billing | M | M | - | - | - |

Legend: M=Manage, C=Close, R=Read, S=Suggest, D=Draft.

Add-on gating flags (examples):

- guardian.pro.enabled
- askdb.enabled
- broadcasts.enabled
- youtube.live.enabled

## 6. Page Contract Skeleton (Example: Poll Create Wizard)

```text
File: page_contracts/poll_create.md
Page ID: polls.create
Purpose: Allow authorized users to create a poll across selected platforms.
Upstream Events: none
Downstream Events: poll.created
UI Elements:
  - TextInput(question) [rules: required, 10..240 chars]
  - OptionsList[1..10] (each: required, 1..80 chars, uniqueness enforced client & server)
  - PlatformSelector (Slack/Discord/Telegram/YouTubeLive? [phase gating])
  - SchedulePicker (optional future)
  - SubmitButton (disabled until valid)
Data Mapping:
  - poll.question -> polls.question (varchar 240)
  - poll.options[] -> poll_options(text, fk poll_id)
  - poll.platform_targets -> poll_targets(platform enum, fk poll_id)
Security/Privacy:
  - No PII; audit create action
Validation:
  - Server rejects duplicate option text case-insensitive
  - Rate limit: max 20 active polls / tenant (config)
Error States:
  - ERR_POLL_LIMIT
  - ERR_INVALID_OPTION
  - ERR_UNAUTHORIZED
UX Patterns:
  - Progressive disclosure: scheduling section hidden until toggle
  - Inline validation per option
```

## 7. Diagram Inventory To Produce

| Diagram | Purpose | Tool/Format | Layer |
|---------|---------|-------------|-------|
| Context Diagram | External systems & SIGMA boundary | Mermaid / C4 Level 1 | Architecture |
| Container Diagram | API, Workers, DB, Queue, Blob, Vector | Mermaid / C4 Level 2 | Architecture |
| Component Diagram | Modules (Polls, Guardian, Ask‑DB) | Mermaid / C4 Level 3 | Architecture |
| Sequence: Ingest Message | Show webhook → queue → worker → db → event | Mermaid seq | Architecture |
| Sequence: Poll Vote | Client → GraphQL → Command → Repo → Event | Mermaid seq | Architecture |
| Sequence: Guardian Flag | Message ingest → rule eval → violation event | Mermaid seq | Architecture |
| Sequence: Ask‑DB Answer | Question → retrieval → ranking → compose | Mermaid seq | Architecture |
| Sequence: Broadcast Dispatch | Compose → per-platform adapters | Mermaid seq | Architecture |
| ER Diagram | Logical data model | Mermaid erDiagram | Domain/Data |
| Event Flow Map | Choreography of core events | Mermaid graph | Domain |
| RBAC Overlay | Roles vs pages/actions | Table + heatmap | UX/Security |
| Scaling Topology | Phase 1 vs 2 infrastructure footprint | Table + diagram | Architecture |
| Cost Curve Chart | Cost vs tenants/users | Graph (mermaid/ASCII) | Business |

## 8. Cost & Sizing Model (Outline for scaling_plan.md)

- Assumptions: avg messages/day/tenant, poll frequency, RAG queries.
- Formulas: storage/year, queue throughput, embedding cost/1k tokens.
- Break-even recalculation formulas with add-on attach rates.
- Capacity thresholds for scale triggers (read replica, Redis, worker autoscale, multi-region).

## 9. Governance & Change Control

- Each doc has an OWNER & REVIEW cadence (e.g., quarterly).
- Change proposals via lightweight RFC in `rfcs/` (template).
- Obsolete docs moved to `archive/` with deprecation notice.

## 10. Phased Delivery of Documentation

| Phase | Deliverables | Rationale |
|-------|-------------|-----------|
| A (Week 1) | vision_manifesto, personas, jobs_to_be_done, portal_sitemap, event_catalog skeleton | Align business & UX early |
| B (Week 2) | domain_model_overview, er_diagram, rbac_matrix, page_contracts (core: dashboard, poll_create, guardian_queue) | Unblock architecture & UI wiring |
| C (Week 3) | sequence_diagrams, scaling_plan (draft), metrics_schema, analytics_events_mapping | Enable performance & analytics planning |
| D (Week 4) | playbooks (connectors, polls, guardian), testing_strategy, quality_gates, sec_privacy_controls extensions | Operational readiness |
| E (Week 5) | Remaining page_contracts, dashboards_spec, runbooks, incident_response, compliance_roadmap | Complete coverage |

## 11. Tooling & Automation Ideas

- Pre-commit script validates page_contract format (YAML header + sections).
- Mermaid diagrams auto-render to PNG on CI for docs site.
- Event catalog lint: ensure all downstream consumers exist in code (future).

## 12. Risks

- Over-documentation slowing build → Mitigation: strict weekly scope & DONE definition.
- Drift between code & docs → Mitigation: add doc update checkbox in PR template.
- Ambiguous RBAC causing rework → Mitigation: finalize matrix before implementing role claims.

## 13. Approval Checklist (for this Plan)

- [ ] Layers ordering agreed
- [ ] Document list accepted
- [ ] Phase schedule feasible
- [ ] RBAC legend sufficient
- [ ] Event catalog columns adequate

---
End of draft plan.
