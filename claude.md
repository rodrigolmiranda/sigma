# SIGMA Claude Code Operational Guide (v2025-09-28)

Product: SIGMA — Social Insights & Group Metrics Analytics.

This document instructs Claude Code EXACTLY how to operate inside this repository. It consolidates and references authoritative docs:

- `docs/AI_Initial_Development_Prompt.md` (startup rules)
- `docs/AI_Restart_Development_Prompt.md` (resume rules)
- `docs/Architecture_Technical.md` (system design)
- `docs/Connectors_WhatsApp_Telegram.md` (connector schema & details)
- `docs/PRD_GroupIntelligence.md` (product scope & KPIs)
- `docs/Security_Privacy_Compliance.md` (controls & compliance)

Claude MUST treat this file as a controller for workflow discipline.

---

## 1. Mission

Preserve SIGMA’s GraphQL-first, Clean Architecture, .NET 10 codebase with in‑house CQRS while advancing product value (multi‑platform chat intelligence: ingest, analytics, polls, safety, RAG, broadcasts) under strict test-first, secure, and observable engineering discipline.

## 2. Operating Modes

| Mode | Trigger | Behavior |
|------|---------|----------|
| Init | New feature / first session | Use Startup Checklist (Section 5) and rules in `docs/AI_Initial_Development_Prompt.md` |
| Resume | Midstream change / fix | Apply Resume Checklist (Section 6) + `docs/AI_Restart_Development_Prompt.md` |
| Drift Remediation | Architecture deviation detected | Pause feature; create remediation tasks; add guard tests |
| Hardening | Pre-release or security-sensitive change | Re-run security & complexity audits; update docs |

## 3. Non-Negotiable Constraints

1. **.NET 10 only** – no target changes.
2. **GraphQL-first** – All external API calls except webhooks/health/internal maintenance go through GraphQL.
3. **No MediatR** – Only in-house CQRS interfaces.
4. **TDD ENFORCED** – Tests MUST be authored/updated BEFORE implementation (reject any plan lacking test-first steps).
5. **≥ 80% Line Test Coverage** overall (do not let coverage drop; raise if critical path uncovered). New code paths must be covered.
6. **Zero Warnings Policy** – Treat warnings as build failures.
7. **Security** – Tenant isolation, input validation, complexity & depth limits, webhook signature validation, no raw PII logs.
8. **Idempotency** – Webhooks & side-effect commands must be idempotent.
9. **Minimalism** – No speculative abstractions or premature optimization.
10. **Documentation Sync** – Public contract / architectural / security change updates docs SAME session.

## 4. Source of Truth Hierarchy

If conflicts occur:

1. Security & Compliance (`docs/Security_Privacy_Compliance.md`)
2. Architecture (`docs/Architecture_Technical.md`)
3. AI Operational Docs (`claude.md` > `docs/AI_Initial_Development_Prompt.md` > `docs/AI_Restart_Development_Prompt.md`)
4. PRD (`docs/PRD_GroupIntelligence.md`)
5. Connector Specs (`docs/Connectors_WhatsApp_Telegram.md`)
6. Local code comments (if consistent). If contradiction: propose correction.

## 5. Startup Checklist (Init Mode)

1. Restate task & scope.
2. Identify impacted layers (Domain/Application/Infrastructure/API/Workers/Docs/Tests).
3. Define micro-contract: inputs, outputs, errors, idempotency, auth notes.
4. Write/extend tests FIRST (unit + GraphQL integration + schema snapshot if schema changes) aiming to maintain/improve coverage.
5. Implement minimal code to pass tests.
6. Run build & all tests; ensure 0 warnings, 0 failures; verify coverage ≥ threshold.
7. Optimize (projection, batching, allocation reductions; eliminate N+1 via DataLoader).
8. Update docs (schema, architecture, security notes, KPI impact if any).
9. Summarize: changes, rationale (WHY), coverage delta, follow-ups, unresolved questions.

## 6. Resume Checklist (Resume Mode)

1. Load open TODO / Open Questions.
2. Confirm build/tests/coverage status (re-run if unknown).
3. Verify no schema or migration drift.
4. Reapply Steps 3–9 of Startup Checklist.

## 7. Drift Detection & Remediation

Flag as drift if ANY of:

- Direct EF access in resolver (instead of handler).
- Missing tenant filter in query.
- New mutation without tests or coverage.
- GraphQL field added without schema snapshot update.

Action: Add guard test → remediate → re-run full suite.

## 8. Output Format (All Sessions)

```text
Task Restatement
Contract
Tests (list + status + coverage impact)
Implementation Summary (diff narrative – no full file dumps unless asked)
Validation (build/tests/coverage/security checks)
Docs Updated (list + purpose)
Follow-Ups (S/M/L)
Unresolved Questions
```

Do NOT emit entire large files unless explicitly requested.

## 9. Security & Performance Quick Gates

| Category | Checks |
|----------|--------|
| Security | Tenant scoping, signature validation, PII scrub, no raw secrets, complexity limits enforced |
| Performance | DataLoader use, projection queries, async I/O, compiled queries for hot paths |
| Reliability | Idempotency keys, outbox persisted, retry/backoff semantics present |
| Observability | Traces (command/query name), correlation ID propagation, key metrics updated |

## 10. GraphQL Discipline

- Explicit object/enum types; nullable only when semantically optional.
- Deprecate before remove (reason + target removal version).
- Enforce depth/complexity; weight list fields.
- DataLoader for relational lists (no N+1); add integration test for all new mutations.

## 11. Test Strategy Summary

| Layer | Purpose | Tools |
|-------|---------|-------|
| Unit | Domain logic & handlers | xUnit / NUnit |
| Integration | GraphQL end-to-end + Postgres | TestServer + containerized DB |
| Contract | Schema snapshot/approval | Stored .graphql schema artifact |
| Performance (light) | Hot query latency regression | Timed integration test (<50ms p95 baseline) |
| Security | Access denial, complexity rejection | Focused GraphQL tests |

Coverage Enforcement: Reject merges reducing global coverage or introducing <80% coverage modules without approved exception.

## 12. Mutation / Command Flow Pattern

1. Mutation -> Resolver -> Construct Command -> Validate -> Authorization -> Transaction -> Persist -> Domain events -> Outbox -> Return payload.
2. Errors mapped to GraphQL with `extensions.code` (VALIDATION_ERROR, PERMISSION_DENIED, NOT_FOUND, CONFLICT, RATE_LIMITED).

## 13. Handling Open Questions

If spec incomplete:

- List 1–3 assumptions (with rationale) OR request clarification if risk high.
- Mark assumptions in summary until resolved.

## 14. Documentation Update Rules

Update docs WHEN:

- New public GraphQL field/mutation/subscription (Architecture + schema snapshot).
- External connector behavior changes (Connectors doc).
- Security/process change (Security/Architecture + maybe this file).
- New KPI/metric definition (PRD + Observability section).

## 15. Prohibited Behaviors

- Introducing MediatR / heavy reflection frameworks.
- Silent schema changes (must update snapshot & mention).
- Logging raw PII or secrets.
- Adding dependencies without clear justification & footprint analysis.
- Large unrelated refactors bundled with a feature.

## 16. Session Termination Criteria

Only finish after: all tests green, coverage ≥ 80%, zero warnings, docs updated (if required), follow-ups logged, assumptions captured.

## 17. Live Feature Flags (Reference Hooks)

| Flag | Purpose |
|------|---------|
| RAG.Enabled | Enable Ask‑DB (Phase 2) |
| Subscriptions.Enabled | Enable GraphQL subscriptions |
| WhatsAppBeta.Enabled | Toggle WhatsApp adjacent channel features |
| PersistedQueries.Enforce | Enforce persisted query whitelist |

## 18. Maintenance Tasks (Recurring)

| Cadence | Task |
|---------|------|
| Weekly | Review open TODO / Open Questions; prune or promote to tasks |
| Weekly | Security dependency audit |
| Per Release | Schema diff review; deprecation removal pass |
| Monthly | Performance & coverage baseline review |

## 19. If Claude Attempts to Deviate

If constraints violated (e.g., plan lacks test-first, coverage ignored), self-correct and restate compliance BEFORE proceeding.

## 20. Minimal Kickoff Prompt Template

```text
Load claude.md and docs/AI_Initial_Development_Prompt.md. Feature: <SHORT DESCRIPTION>.
Apply Startup Checklist. Respond with: Task Restatement, Contract, Planned Tests.
Wait for confirmation before coding if any high-risk assumptions.
```

---
Owned by: Architecture / Platform Team.
Update cadence: Immediate on any architectural, workflow, or policy change.
