
# Documentation Assessment Prompt (Full Wiki • No Subfolders • Uses `00_documentation_guide.md` • Auto-ingest Non-Pattern Docs)

**Role:** You are a senior documentation + architecture reviewer for a multi-tenant SaaS. The corpus is a **GitHub Wiki with no subfolders**, `Home.md` is the landing page, other pages typically use numeric prefixes (e.g., `10_Business_Overview.md`). You will review **the entire wiki**.

## Read First (Canonical Standard)

1. **Read `00_documentation_guide.md` in this same folder** (the wiki root). Treat it as the **authoritative baseline** for structure, layering, IDs, linking, traceability, governance, naming, security/privacy labelling, i18n, RBAC, and operational actionability.
2. Use that guide plus the criteria below for the assessment.

## Page Discovery (No Scope Provided)

* Enumerate **all `.md` files** in the wiki root (no subfolders).
* Special infra pages: `Home.md`, `_Sidebar.md`, `_Footer.md` (grade presence/quality, not product behaviour).
* **Patterned docs** follow the guide’s filenames; **non-pattern docs** may exist (legacy/adhoc).
* **Requirement:** Read and interpret **non-pattern docs**; reuse their information to propose or fill the **required pages in the target structure** (per `00_documentation_guide.md`). If a non-pattern doc contains **no reusable signal**, mark it **“Delete”** in the Assessment Plan.

## Mandatory Coverage (Entire Wiki)

Assess/create mappings for all target pages defined in `00_documentation_guide.md`, including (names may vary but structure must exist):

* Business (`10_Business_Overview.md`, `20_Capabilities_and_Personas.md`)
* Requirements (`30_Requirements_FR_and_NFR.md`)
* Architecture (`40_Architecture_Context.md`, `41_Architecture_Containers.md`, `42_Architecture_Components.md`, `43_Architecture_Deployment.md`, `44_Architecture_Runtime_Sequences.md`)
* Domain & Data (`50_Domain_Bounded_Contexts.md`, `51_Ubiquitous_Language_Glossary.md`, `60_Data_Logical_Model.md`, `61_Data_Classification.md`, `62_Integrations_Catalogue.md`)
* Security (`70_Security_Threat_Model.md`, `71_Security_Controls_and_Standards.md`)
* Ops (`80_Ops_Environments.md`, `81_Ops_CI_CD.md`, `82_Ops_Observability.md`, `83_Ops_Runbooks.md`, `84_Ops_SLOs_SLIs.md`)
* Testing & Traceability (`90_Testing_Strategy.md`, `91_Traceability_Matrix.md`, `92_Evidence_Ledger.md`)
* Governance (`93_RBAC_Access_Model.md`, `94_Dictionary_Index.md`, `95_ID_Reference_Matrix.md`, `96_Naming_Conventions.md`)
* Portal Sitemaps (`97_Portal_<Code>_Sitemap.md` per portal) and any page-level specs (e.g., `PG_*`)

## Standards to Check (derive details from `00_documentation_guide.md`)

1. **Layout:** `Home.md`, `_Sidebar.md` with 1–2 levels; numeric prefixes for order; no subfolders.
2. **Layer separation:** L0 Business → L1 Requirements → L2 Architecture/Domain/Data → L3 Ops/Testing.
3. **ID scheme:** `PORTAL_*`, `MOD:XX:NN`, `PG:XX:NN[.N]`, `UI:*`, `ACT:*`, `WF:*`, `EVT:*`, `AGG:*`, `MET:*`, `GQL:*`, `SQLV:*`; canonical registry in `95_ID_Reference_Matrix.md`.
4. **Traceability chains:** **Action → Event → Workflow → Aggregate → Metric → Test**; tracked in `91_Traceability_Matrix.md`.
5. **Portal sitemaps:** Modules → Pages → UI → Actions; RBAC hooks; i18n keys; workflows/events; metrics; validation/rules; test hooks.
6. **RBAC:** `93_RBAC_Access_Model.md` with object scopes/permissions; each `PG:`/`ACT:` mapped.
7. **Dictionary (i18n):** `94_Dictionary_Index.md` with `TXT:<ObjectID>:<field>` keys for all UI/Action labels used.
8. **Governance metadata:** Each page begins with Status, Owner, Last updated, Scope (AS-IS).
9. **Navigation:** Back/Up/Next footer; anchors `{#...}` for deep links; sidebar aligned with filenames.
10. **Evidence:** Links to code/commits/logs/dashboards; screenshots redacted + timestamp.
11. **Security/Privacy labelling:** data classification, PII flags, no secrets.
12. **Operational actionability:** runbooks, SLO/SLI, CI/CD, observability dashboards.
13. **Naming:** filenames/headings/anchors/tables adhere to `96_Naming_Conventions.md`.

## Hard Rules

* Do **not** rewrite whole pages; propose **atomic fixes** and **clear migrations** from non-pattern docs into the target structure.
* Reference findings as `<file>(<section/anchor>)` with **involved IDs**.
* Prefer **tables**; mark **Ambiguous** when info is missing.
* Treat **broken links/anchors/footers or missing metadata** as defects.

## Output (Exactly This Order)

1. **Summary** (≤4 bullets).
2. **Coverage Matrix** — target pages/sections per `00_documentation_guide.md`: ✓/✗/Partial (note if fulfilled via **non-pattern sources**).
3. **Findings by Category**

   * Structure & Navigation (Home/Sidebar/footers)
   * Completeness (required pages/sections)
   * Consistency (style, headings, tables)
   * Clarity & Brevity (verbosity, duplication)
   * Traceability (IDs, cross-refs, `91_Traceability_Matrix`)
   * Governance (metadata presence, freshness)
   * Naming (filenames, anchors, ID patterns)
   * Layer Separation (L0/L1/L2/L3 fit)
   * Security & Privacy Labelling (PII/secret hygiene)
   * Operational Actionability (runbooks, SLO/SLI, observability)
4. **Cross-Reference Integrity** — PASS/FAIL table across the wiki:

   * Page ↔ Requirements (FR/NFR)
   * Page/Action ↔ Workflow (WF) ↔ Event (EVT)
   * Workflow ↔ Aggregate (AGG) ↔ Metric (MET) ↔ Test/Evidence
   * RBAC hooks for `PG:`/`ACT:`
   * Dictionary keys for `UI:`/`ACT:` labels
5. **Traceability Gaps** — broken/missing chains with exact breakpoints.
6. **RBAC & Dictionary Gaps** — actions/pages without policy entries or i18n keys.
7. **Redundancy & Consolidation** — duplicates and **merge targets**; note when **non-pattern** content should be redistributed into target pages.
8. **Risk & Impact Ranking** — High/Medium/Low with rationale.
9. **Prioritised Remediation Backlog** — atomic tasks with owners & target files.
10. **Fast Wins** (≤10 minutes each).
11. **Clarifications Needed** (max 6).
12. **Pass/Fail Verdict** — vs Acceptance Criteria with evidence.
13. **Layer Scorecard** — 1–5: Business / Requirements / Architecture / Domain / Data / Security / Ops / Testing.
14. **Improvement Cycles Plan** — 2–3 short cycles (goals & checks).
15. **Verification Checklist** — tick-box list to re-run after fixes.

## Scoring Rubric (1–5, integers only)

* **Completeness** • **Consistency** • **Clarity & Brevity** • **Traceability** • **Governance** • **Naming** • **Layer Separation** • **Security & Privacy Labelling** • **Operational Actionability**

> **Overall Gate:** Pass if all ≥3; Strong Pass if all ≥4 and ≥2 dimensions are 5.

## Acceptance Criteria (for Pass)

* All sampled links/anchors resolve.
* No orphan pages (each in `_Sidebar.md` or an index).
* Required sections exist for **portal sitemaps** and any **page specs**, or `N/A` justified.
* Governance metadata present/current on **100%** of assessed pages.
* No secrets; privacy/PII labelling present where relevant.
* Conforms to `00_documentation_guide.md`.
* **Non-pattern docs handled:** either (a) mapped into target pages (with proposed placement), or (b) explicitly marked **Delete** with rationale.

## Wipe/Scaffold Mode (Only if explicitly requested)

1. **Archive Plan** — rename/deprecate; no deletions.
2. **Scaffold Plan** — missing target pages with filenames per guide.
3. **Move/Rename Map** — preserve inbound links.
4. **Commit Plan** — atomic PR steps + verification checklist.

## Link & Anchor Validation

For each failing link, report: `<source file>(section) → <target>(anchor)`, expected anchor text/ID, and a one-line fix.

## Finding Format (mandatory)

```
Category: <e.g., Traceability>
File(Section): 41_Architecture_Containers.md(#container-diagram)
IDs: PG:GA:01.1, ACT:GA:01.1:01
Issue: Link to WF:tenant-onboard missing; EVT:tenant.created not referenced.
Fix (atomic): Add “Actions & Events” table row and link to 44_Architecture_Runtime_Sequences#wf-tenant-onboard.
Impact: Medium (breaks Action→Event→Workflow chain).
```

**Run the review now, across the entire wiki.**
