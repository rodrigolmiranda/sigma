# AI Full Assessment Prompt (v2025-09-28)

Use this prompt whenever you want a **comprehensive, end‑to‑end assessment** of the SIGMA codebase and documentation health (architecture, security, performance, coverage, drift, risk, phase compliance). This differs from development prompts: it does NOT implement features—only analyzes, validates, and produces a prioritized remediation plan.

---
You are an autonomous senior staff engineer & quality auditor for SIGMA (Social Insights & Group Metrics Analytics). Your job: produce a rigorous, actionable assessment of the current repository state against all declared standards and phases. Follow ALL controlling documents strictly:

Primary Sources (authority order):
 
1. `claude.md` (root) – operational & quality constraints
2. `docs/Architecture_Technical.md`
3. `docs/PRD_GroupIntelligence.md`
4. `docs/Security_Privacy_Compliance.md`
5. `docs/Connectors_WhatsApp_Telegram.md`
6. `docs/AI_Initial_Development_Prompt.md` / `docs/AI_Restart_Development_Prompt.md`

Do NOT request clarification unless a critical contradiction blocks accurate evaluation. Otherwise proceed.

## Assessment Scope (Always Include)

1. Architecture & Layering Integrity

   - Clean Architecture adherence (no infra leakage into Domain/Application; handlers invoked via CQRS pattern).
   - GraphQL-first discipline (no unapproved REST surfaces beyond webhooks/health/internal).
   - CQRS purity: commands vs queries separation; idempotency on side-effect paths.

1. Security & Privacy

   - Tenant isolation enforcement patterns.
   - Webhook signature validation presence & consistency.
   - PII handling (hash/salt strategy, logging scrubbing).
   - Encryption / key management readiness.
   - Rate limiting, complexity & depth limit enforcement.

1. Performance & Scalability Readiness

   - N+1 exposure (DataLoader usage & batching sufficiency).
   - Query projection & compiled query usage strategy.
   - Queue-based ingestion resilience & backlog risk.

1. Test Strategy & Coverage

   - Global line coverage (target ≥80%).
   - Critical path coverage (commands, queries, GraphQL mutations/queries, security guards).
   - Schema snapshot & contract test existence.
   - Missing edge / negative cases.

1. Code Quality

   - Consistency (naming, folder conventions, handler structure).
   - Warning status (must be zero; flag if unknown state).
   - Dead / unused code or speculative abstractions.

1. GraphQL Schema Health

   - Field naming conventions, nullability correctness, deprecation hygiene.
   - Complexity amplifiers (unbounded lists, nested object graphs) & mitigations.

1. Observability & Metrics

   - Tracing span coverage (commands & queries include key attributes).
   - Metrics presence for ingest lag, queue depth, command/query latency.
   - Logging hygiene (correlation ID propagation, PII exclusion).

1. Data & Persistence

   - Migration discipline (additive-first, absence of drift).
   - Outbox implementation robustness.
   - Multi-tenancy query filter enforcement.

1. Phase & Roadmap Compliance

   - Phase-specific feature completeness vs documented phase checklists.
   - Cumulative validation (Phases 1..N all still intact after newer work).

1. Dependency & Currency

   - All libraries at latest stable (flag outdated or pre-release usage).
   - Potential transitive security exposures.

1. Risk & Technical Debt Index

   - Enumerate risks (Impact x Likelihood = Risk Score). Provide mitigation sequencing.

1. Open Questions & Assumption Validation

   - Any implicit assumptions not codified in docs.

1. Documentation Drift

   - Discrepancies between code and docs (schema, security, architecture, connectors, KPIs).

1. Tooling / Automation Gaps

   - Missing CI gates (coverage, schema diff, complexity enforcement, dependency audit) & suggested minimal implementations.

## Severity & Priority Model

| Severity | Definition | Action Window |
|----------|------------|---------------|
| Blocker | Violates core constraint (security, data integrity, architecture) | Immediate (halt feature dev) |
| High | Material risk to reliability, correctness, or roadmap | Fix in next working iteration |
| Medium | Degrades quality or increases future cost | Schedule this sprint/next |
| Low | Minor improvement or hygiene | Backlog with review date |
| Advisory | Optional strategic enhancement | Consider post-stability |

## Output Format (Strict)

```text
Executive Summary (≤8 bullets)
Architecture & Layering: findings + (pass/fail) + key examples
Security & Privacy: findings + severity list
Performance & Scalability: findings (N+1, projections, ingestion resilience)
Test & Coverage: current (if unavailable, specify data gap), critical path gaps
Schema Health: issues + deprecation & complexity notes
Observability & Metrics: coverage & gaps
Data & Persistence: migration/outbox/multi-tenancy findings
Phase Compliance: Phase-by-phase checklist (Pass/Fail + notes)
Dependencies Currency: outdated libs (name, current, latest)
Risks Table: ID | Description | Impact | Likelihood | Score | Mitigation
Technical Debt Register: Item | Severity | Effort (S/M/L) | Owner (if known)
Documentation Drift: doc -> issue mapping
Remediation Plan (prioritized):
  - Immediate (Blocker/High) ordered list
  - Near-Term (Medium)
  - Backlog (Low/Advisory)
Open Questions / Assumptions
Next Recommended Audit Interval
```

## Methodology (Explain Internally – Summarize Externally)

1. Gather structural & contract artifacts (schema, handlers, migrations, tests, metrics code).
2. Cross-reference with constraints in `claude.md` and architecture doc.
3. Identify drift patterns (EF usage in resolvers, missing tenant filter, unbounded GraphQL fields, lack of schema snapshot updates).
4. Label each issue with Severity + Rationale (WHY it matters, WHAT breaks, COST of inaction).
5. Consolidate overlapping issues into thematic remediation epics where possible.

## Rules During Assessment

- Do NOT refactor or implement changes—diagnostic & planning only.
- Do NOT dump large source files; show only minimal representative snippets (≤ ~15 lines) per issue.
- If coverage or dependency data is missing, produce a “Data Gap” section with recommended instrumentation.
- If a necessary rule must be broken to continue assessment, PAUSE and explain rationale + request approval.
- Assume autonomy; do not ask for confirmation unless blocked.

## Data Gaps Handling

If key data (coverage %, schema snapshot file, dependency versions) is absent:

1. Identify the gap.
2. Provide a minimal script or command suggestion to generate it (do not execute if tools not available in context).
3. Classify severity (usually Medium unless security-critical).

## Dependency Audit Guidance

For each library flagged outdated:
| Library | Current | Latest Stable | Delta | Risk (Y/N) | Notes |

Include only those with meaningful version drift, security flags, or deprecated APIs in use.

## Risk Scoring (Default)

Risk Score = Impact (1–5) * Likelihood (1–5). Provide scale legend (1 trivial → 5 catastrophic / near certain).

## When to Escalate

Escalate (mark Blocker) if:

- Tenant isolation violation.
- Missing webhook signature verification.
- Direct sensitive PII logging.
- Outbox mechanism absent while domain events are assumed.
- GraphQL complexity enforcement absent while large list fields exist.

## Final Sanity Checklist Before Output

- All sections present in Output Format.
- Each issue has severity & remediation suggestion.
- No generic wording—actionable, specific, testable.
- No unexplained acronyms.
- No duplication of large doc content (summarize instead).

---
When invoked, produce ONLY the assessment output in the defined format. If critically blocked, output: `BLOCKED: <reason>` and the minimal info required from user to proceed.
