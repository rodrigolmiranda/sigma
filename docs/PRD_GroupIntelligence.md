# PRD — Group Intelligence (v2025-09-27)

A multi‑platform **chat intelligence** and **light engagement** product for educators, creators, and organisations. It ingests conversations from Slack, Discord, Telegram (Phase 1), optionally YouTube Live chat, and expands to Microsoft Teams (Phase 3). It unifies: analytics, polls, a safety engine (Guardian), an optional RAG knowledge add‑on (Ask‑DB), and cross‑posting. **WhatsApp** (Cloud API) is treated as an **adjacent B2C / announcement channel** (not core group analytics yet) – limited to permitted business-initiated / user-initiated 24‑hour session flows; **consumer group ingestion is not generally available** and is explicitly out‑of‑scope until Meta policy changes.

---

## 1) Problem & Vision
- **Problem:** fragmented chat communities; no unified insights; repeated questions; safety risks; difficulty directing people to the right resources at the right moment.
- **Vision:** a single pane to **measure**, **guide**, and **safeguard** chat communities across platforms, with pricing that works for small groups yet scales to enterprise expectations.

## 2) In‑scope Platforms
- **Phase 1 (MVP):** Slack, Discord, Telegram; Optional YouTube Live moderation. Parallel (non‑blocking) enablement of WhatsApp outbound + limited inbound 1:1/session messages for broadcast & FAQ deflection (flagged as “Beta – non‑core metric”).
- **Phase 2:** Phase 1 + Ask‑DB/RAG + cross‑posting/broadcasts (adds unified composer that can also push templated WhatsApp messages if session open / template approved).
- **Phase 3:** Microsoft Teams; enterprise SSO/SCIM; data residency (AU/BR); potential WhatsApp evolution if group APIs become compliant/available (decision gate).
- **WhatsApp note:** ONLY Cloud API; store minimal PII (phone hashed + last 4 digits); opt‑in registry per tenant.

## 3) Users & Jobs‑to‑Be‑Done

- **Professor / Educator:** measure engagement; run polls; auto‑answer repetitive questions with citations.
- **Community Manager / Support Lead:** find trends, unanswered threads; escalate to helpdesk; keep spaces safe.
- **Enterprise Comms / University IT:** policy filters, residency, SSO/SCIM, audit.

## 4) Success Metrics (MVP → Growth)

- ↓ Unanswered threads; ↓ time‑to‑first‑answer
- ↑ Poll participation; ↑ weekly active posters
- Ticket deflection from Ask‑DB; moderation workload reduction
- Setup time < 30 minutes for 3 platforms

## 5) Features by Module

### 5.1 Connectors (how it works)

- **Slack** — subscribe to Events API (`message.*`), validate challenge; store message & metadata; optional history backfill using Conversations API.
- **Discord** — connect via Gateway/WebSocket; enable **Message Content** privileged intent (verification for 100+ servers); capture attachments.
- **Telegram** — add bot to groups; **privacy mode OFF**; webhook for updates; optional admin role for best coverage; track `update_id` for idempotency; throttling guard (≥ 50ms spacing outbound bursts) to stay under per‑bot limits.
- **YouTube Live (optional)** — use `liveChatMessages.streamList` for low‑quota streaming; mirror Qs to moderator console; post answers back.
- **Microsoft Teams (Phase 3)** — Microsoft Graph access (`ChannelMessage.Read.All` etc.) with admin consent; webhooks for change notifications.
- **WhatsApp (adjacent)** — Cloud API webhook (messages, statuses) → minimal ingest only for 1:1/session chats; template + session message send; NO group metrics; hashed phone + opt‑in flag stored.

See `Connectors_WhatsApp_Telegram.md` for deep integration design (schemas, rate limits, failure modes).

### 5.2 Analytics

- **Dashboards**: growth, messages/day, active posters, leaders, top threads/links/media, unanswered threads, policy flags.
- **Filtering**: by tenant/workspace/channel/platform/time; tag topics.

### 5.3 Polls

- MCQ, rating, emoji vote; one‑off and scheduled; adapters per platform; merged results; CSV export.

### 5.4 Guardian (Safety)

- **Basic (P1)**: regex/keyword rules; link expansion; manual review queue; evidence bundle (message + user + context + link snapshots).
- **Pro (P2)**: toxicity classifier, simple leak/PII heuristics, OCR for images; actions: flag, notify moderator, warn user (platform permitting), open ticket via webhook.

### 5.5 Ask‑DB (RAG, P2)

- Upload PDFs/Docs/URLs; ingest video transcripts with timestamps; chunk + embed (pgvector); retrieve + re‑rank; compose short **answers with citations**; “Teacher Pointers” to prefer selected sources.

### 5.6 Cross‑posting/Broadcasts (P2)

- Compose once, schedule multi‑channel; rate‑limit aware; delivery & click analytics; A/B titles.

### 5.7 Admin & Billing

- Tenants; plans; **retention selector** (14/30/60/90/180/365 days); add‑ons; quotas; invoices; audit; API keys.

### 5.8 Data Lifecycle

- Retention enforcement by tenant; anonymised aggregates retained; export & delete APIs; evidence vault for Guardian.

## 6) Non‑Goals (MVP)

- Full community platform replacement; mass automation/spam; scraping.

## 7) Sitemap (Modules → Pages → Actions)

### 7.1 Landing / Sales

- Home, Features, Pricing (interactive calculator), Docs, Privacy, Terms

### 7.2 Tenant Admin

- **Dashboard** — KPIs, ingest health, errors; filters; exports
- **Workspaces & Connectors** — add/verify Slack, Discord, Telegram, (Teams P3); select channels; test events
- **Channels** — list, tags, retention override
- **Polls** — create, schedule, post targets, results, export
- **Guardian** — rules editor, violations queue, evidence viewer, exports
- **Ask‑DB** — upload sources, index status, test questions, citations view
- **Broadcasts** — composer, schedule, A/B, delivery report
- **Users & Roles** — invite, RBAC (Owner/Admin/Moderator/Analyst)
- **Billing** — plan, retention, add‑ons, invoices
- **Settings** — webhooks, API keys, SSO (P3), data residency (P3)

### 7.3 Moderator Console

- Live feed (flags, unanswered), quick actions (assign, escalate), notes

### 7.4 Reports

- Engagement, Top Questions, Unanswered, Poll insights, Guardian trends

## 8) Workflows (detailed)

### 8.1 Onboarding

1. Create tenant → 2. Choose plan + retention → 3. Connect platforms (auth/scopes) → 4. Select channels → 5. Fire test events → 6. Start ingest (optional history) → 7. First dashboard within minutes.

### 8.2 Ingest → Normalise → Store

Webhook/gateway → Queue → Ingest worker (canonical `MessageEvent`) → Postgres rows + Blob media refs → nightly metrics rollup.

### 8.3 Poll

Create → select channels/platforms → adapters post → collect votes → merge → charts/export.

### 8.4 Guardian

New message → rules + optional ML → if flagged: create violation + evidence → DM moderator + queue item → optional ticket webhook → resolution & feedback loop.

### 8.5 Ask‑DB

Question → retrieve chunks (tenant/workspace scope) → re‑rank → compose short answer + **citations** → post → collect feedback.

### 8.6 YouTube Live (optional)

Start stream → subscribe to `streamList` → mirror hot Qs to console → moderator approves answers → bot posts back → auto debrief doc.

### 8.7 Billing & Retention

Stripe events → update subscription → retention job deletes raw data beyond window → aggregates preserved → invoices downloadable.

### 8.8 Data Export/Deletion

Admin triggers → async job → signed URLs → audit trail.

## 9) Acceptance Criteria (MVP)

- Connect and ingest Slack/Discord/Telegram within 5 minutes; basic WhatsApp channel can send a verified template (if configured) within onboarding but is excluded from core activation KPI; merged poll results; Guardian flag with export; retention job runs; setup < 30 minutes.

## 10) Phases, Costs & Break‑Even

- **Phase 1 infra:** US$120–300/mo (mid 220) → BE: 6–12 customers (Plus/Starter tiers).
- **Phase 2 infra:** US$900–1,400/mo (mid 1,100) → BE: ~23–29 (with/without add‑on uplift ~+$10 ARPU).
- **Phase 3 infra:** US$3,000–6,000/mo (mid 4,000) → BE via mix (e.g., 10×SMB + 8×Pro ≈ $4.1k MRR) or 1 enterprise.

## 11) Risks & Mitigation

Platform policy changes; LLM cost spikes; privacy concerns; WhatsApp policy constraints; Telegram spam/abuse vectors → adapter abstraction; token budgets/caps; strict consent, export/delete; clear docs; opt‑in registry; abuse rate limiting; decision gate for any expansion of WhatsApp scope.

## 12) Open Questions / TODO (Tracking)

1. Define KPI impact model for WhatsApp Beta (exclude from retention, include in expansion metric?).
2. Evaluate Telegram large supergroup (>100k) edge cases – memory & pagination for history backfill (Phase 2 candidate).
3. Decide on canonical `MessageEvent` schema versioning strategy (proposed v1 in connectors doc).
4. Add multi‑lingual template library for outbound FAQ deflection (Phase 2/3).
5. Evaluate need for ephemeral caching layer (Redis) before Phase 2 scale.

