# Documentation Naming Conventions

Purpose: Keep a scalable, predictable structure as page-level specs grow.

## File Naming Pattern (Portal Pages)

| Portal | Prefix | Example File | Page ID Mapping |
|--------|--------|--------------|-----------------|
| Global Admin | ga_pg_&lt;module&gt;_&lt;sub&gt;_&lt;slug&gt;.md | ga_pg_02_1_plans_catalog.md | PG:GA:02.1 |
| Tenant Workspace | tw_pg_&lt;module&gt;_&lt;sub&gt;_&lt;slug&gt;.md | tw_pg_02_3_poll_analytics.md | PG:TW:02.3 |

Rules:

1. module = two-digit module number (00 padded) from portal module index.
2. sub = page sequence inside module (one digit unless nested becomes x_y).
3. slug = short kebab description; avoid stop words.
4. Keep IDs (Page ID) inside the file meta table; IDs are canonical—file names are mutable but discouraged to change once linked.

## Non-Page Artifacts

| Artifact | Pattern | Example |
|----------|---------|---------|
| Impact Matrix | impact_matrix.md | impact_matrix.md |
| ID Namespace JSON | id_namespace.json | id_namespace.json |
| Naming Conventions | naming_conventions.md | naming_conventions.md |
| Sequence Diagram Collections | diag_&lt;area&gt;.md | diag_onboarding.md |

## Inside Each Page Spec

Section order (fixed): Meta → Purpose → Context & KPIs → Data Sources → UI Elements → Actions & Events → Domain Aggregates → Metrics → Workflow Diagram (optional) → Validation/Rules → Test Hooks → Change Impact.

## Data Classification Labels

| Label | Definition | Examples |
|-------|-----------|----------|
| Public | Non-sensitive, safe to expose in marketing or docs | Plan names |
| Internal | Operational metadata not for external analytics sharing | Workflow IDs |
| Confidential | Business-sensitive metrics or financials | MRR, ARR |
| PII | Direct or indirect personal identifiers | userEmailHash (hashed), ipCountry |
| Sensitive | Security / keys / privileged config | API key material (never rendered) |

PII column in UI tables is Y/N; if Y ensure retention & masking rules defined elsewhere.

---
Navigation: [Home](home.md) | Previous: [ID Reference Matrix](id_reference_matrix.md) | Next: [Impact Matrix](impact_matrix.md)
