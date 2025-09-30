# AI Restart Development Prompt (v2025-09-28)

Use this when resuming work mid-project. It enforces continuity, adherence to guidelines, and prevents architectural drift.

---
You are re-entering SIGMA (Social Insights & Group Metrics Analytics): a .NET 10, GraphQL-first, Clean Architecture codebase with in-house CQRS (no MediatR). Continue strictly with existing conventions.

## Before Doing Anything

1. Summarize current task (if provided) OR request a task description.
2. Load & review relevant docs: `Architecture_Technical.md`, `AI_Initial_Development_Prompt.md`, any feature spec involved.
3. Confirm no pending build errors, failing tests, unchecked warnings, or coverage regressions (target ≥80% line coverage). If unknown, instruct to run build + tests + coverage first.
4. Enumerate open TODOs / Open Questions sections that relate to the task.

## Golden Rules (Do NOT Deviate)

- .NET 10 only; no framework substitutions.
- GraphQL is primary API; minimal endpoints only for webhooks/health/internal.
- In-house CQRS only (ICommand/IQuery handlers) – NEVER introduce MediatR or similar.
- TDD (strict): add/update tests first (never implement code for a new path before tests); never leave failing tests behind at session end.
- Zero warnings policy: fix or justify (rare) immediately.
- Coverage: maintain or improve ≥80% global line coverage; highlight coverage diff in summary if material.
- Autonomous execution: proceed without seeking incremental approval unless a Golden Rule/non‑negotiable must be broken—then pause with rationale & await instruction.
- Library currency: always choose latest stable versions; verify via official docs (may use context7) before adoption.
- Tooling: may use Playwright MCP for E2E / interaction tests; context7 for authoritative library usage patterns.
- Cumulative phase validation: each session end re-checks all previously completed phase objectives across entire codebase; fix gaps before new work.
- Security: enforce tenant isolation, validate inputs, respect complexity/depth limits, never log raw PII.
- Keep docs updated if contracts, architecture pieces, or security aspects change.

## Session Workflow

1. Re-state objective & scope boundaries.
2. Identify impacted layers (Domain, Application, Infrastructure, API, Workers, Docs, Tests).
3. Define micro-contract (inputs, outputs, error modes, side effects, idempotency) in ≤5 bullets.
4. Write/adjust tests (unit + integration/GraphQL) BEFORE implementation.
5. Implement smallest passing code (autonomous unless violation required).
6. Run build + all impacted tests; ensure 0 warnings; capture coverage delta.
7. Cumulative assessment: verify architectural, security, prior phase criteria (CQRS boundaries, tenant isolation, schema snapshot, complexity limits). Add/fix tests then remediate drift before continuing.
8. Optimize (projection, batching, allocations) & confirm no performance regress vs. established baselines.
9. Update documentation (schema snapshot, architecture notes, security implications, coverage notes if trade-offs).
10. Produce session summary: changes, tests added, coverage impact, cumulative assessment results, follow-ups (S/M/L) and unresolved questions.

## Recovery / Drift Handling

If evidence of architectural drift (e.g., direct EF usage in API layer, missing tenant filter, duplicate command logic):
 
- Stop feature coding; create Retrofit Task List.
- Add failing test(s) capturing desired enforcement; then refactor to pass.

## Security Checklist (Quick Pass)

- Tenant context required for all handlers.
- Webhook endpoints verify signatures.
- GraphQL complexity & depth enforced.
- Sensitive fields encrypted / hashed appropriately.
- No secrets or keys in source; config binding only.

## Regression Prevention

- Maintain schema snapshot test (fail on unplanned change).
- Add guard tests for previously fixed defects.
- Consider property-based tests for parsing/normalization edge cases (Phase 2+).

## Performance Hygiene

- Ensure DataLoader or batch pattern for N+1 hotspots.
- Avoid synchronous over async IO.
- Use AsNoTracking for pure read queries.

## Observability & Metrics

- Add tracing spans around new handlers if absent.
- Emit or extend metrics (duration, failures) for new command/query.
- Log correlation ID preserved end-to-end.

## End-of-Session Output Format

1. Task Restatement
2. Changes Summary
3. Tests (list + status) + Coverage Impact
4. Build/Test Status (all green?)
5. Warnings (should be 0)
6. Cumulative Assessment (prior phases & guideline revalidation; remediation log)
7. Performance / Security notes
8. Follow-ups & TODOs

## Escalation / Uncertainty

If blocked by missing specification details:
 
- Provide assumption list (1–3 concise assumptions) and proceed OR explicitly request clarification if assumption risk is high.

Re-acknowledge adherence to these rules and root `claude.md` before finalizing any answer.

Return now a confirmation you’ve loaded guidelines. Await the specific task if not provided.
