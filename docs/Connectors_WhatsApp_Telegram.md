# Connectors Deep Dive — WhatsApp & Telegram (v2025-09-27)

Status: Draft (Phase 1 focus). WhatsApp is an "adjacent" channel (Beta) for outbound + limited inbound 1:1/session support; Telegram is a core Phase 1 ingest platform.

---
## 1. Goals

- **Telegram:** Reliable group message ingest (text, media metadata, edits, deletes), polling, safety signals, low-latency (<2s p95) normalization.
- **WhatsApp:** Outbound educational / announcement templates; inbound session replies -> optional FAQ deflection & Ask-DB answer; *no* group analytics (API not available). Minimise stored PII.

## 2. Canonical MessageEvent (v1)

```json
MessageEvent {
  Id: string (uuid)
  Platform: enum (Slack|Discord|Telegram|YouTube|Teams|WhatsApp)
  PlatformMessageId: string
  PlatformChannelId: string|null  // WhatsApp 1:1 may map to pseudo-channel: wa:<hashed-phone>
  WorkspaceId: Guid
  TenantId: Guid
  Sender: { PlatformUserId: string, DisplayName: string|null, IsBot: bool }
  Type: enum (Text|Image|File|Poll|System|Reaction|Unknown)
  Text: string|null (sanitised, trimmed)
  RichFragments: Fragment[] // links, mentions, emojis
  Media: [{ Url: string (blob or external), Mime: string, Size: int? }]
  TimestampUtc: DateTime
  EditedUtc: DateTime? 
  ReplyToPlatformMessageId: string|null
  Reactions: [{ Key: string, Count: int }]
  Raw: JsonDocument (encrypted-at-rest / purge after 30d if retention < 30d)
  Version: 1
}
```

Retention: respect tenant window; Raw field is the first candidate for early purge. WhatsApp hashed phone (SHA256 + per-tenant salt) replaces direct phone anywhere outside transient processing.

## 3. Telegram Integration

### 3.1 Setup

- Create bot with @BotFather -> disable privacy mode (OFF) to receive all group messages.
- Add to target groups; optional promote to admin to capture deletes/edits reliably.
- Set webhook: `POST /webhooks/telegram/{botId}` (Functions endpoint).

### 3.2 Events Consumed

- `message` (text, media, new_chat_members, left_chat_member)
- `edited_message`
- `channel_post` (ignored in MVP unless channel ingestion desired)
- `my_chat_member` (track bot removed)

### 3.3 Idempotency & Ordering

- Track `update_id` high-water mark per bot; discard duplicates; store in fast table or cache. Telegram delivers mostly in order but do not rely; ordering by timestamp.

### 3.4 Rate Limits & Outbound

- Telegram: ~30 messages/sec per bot overall; safe throttle (token bucket) at 10 msg/sec soft + burst 5.
- Polls: build platform-specific message chains (question + options) or native Telegram Poll API (bind response ids -> internal poll option ids).

### 3.5 Media Handling

- Receive file id -> call `getFile` lazily only when needed (polling worker) to fetch file path; stream into Blob (virus scan optional Phase 2) -> store blob URL.

### 3.6 Error Handling

- Network / 5xx -> exponential backoff (jitter). 429 -> observe `retry_after`.
- Malformed updates -> log sample (1%) with PII scrub.

### 3.7 Security

- Validate secret token in webhook URL (bot token fragment hashed) to prevent spoof.
- Input validation on all text fields; length caps; strip zero-width chars.

## 4. WhatsApp (Cloud API) Beta

### 4.1 Scope

- Support: template messages (pre-approved) + session replies (within 24h of last user message).
- Ingest only educational help requests & FAQ triggers. No group metrics. Treat each phone as a pseudonymous member (hashed).

### 4.2 Setup

- Meta App -> WhatsApp Business Account -> Cloud API credentials (per-tenant or managed multi-tenant proxy model). Store access token in Key Vault; rotate every 90d.
- Webhook: `POST /webhooks/whatsapp/{tenantId}` verifying X-Hub-Signature-256 (HMAC-SHA256) with App Secret.

### 4.3 Events Consumed

- `messages` (types: text, image, interactive, button)
- `statuses` (delivery/read receipts) -> optional analytics (delivery ratio) Phase 2.

### 4.4 Outbound Flow

1. Admin creates template (in Meta UI) -> adds template name + variables into system.
2. System selects recipients with active session OR sends template (outside session) using template send endpoint.
3. Store send attempt (idempotency key = tenant + phoneHash + template + timestamp bucket).
4. On user reply -> create MessageEvent (Platform=WhatsApp) with pseudo-channel id `wa:<tenantIdHash>:<phoneHashPrefix4>`.

### 4.5 Privacy & PII Minimisation

- Store only hashed phone (SHA256 + tenant salt) + last 4 digits clear for support display.
- No raw media retained unless required for Guardian flag; otherwise transient scan -> discard.

### 4.6 Rate / Throughput Considerations

- Template quality tiers (Meta) gate throughput; implement internal queue with adaptive concurrency.
- Backoff on 429 with vendor-specified wait.

### 4.7 Abuse & Compliance

- Opt-in registry (timestamp + source). Reject outbound if not opted-in or session window closed (non-template message).
- Automatic cap: max X (config default 5) unsolicited templates per 24h per recipient.

### 4.8 Security

- Validate HMAC signature for every webhook.
- Access token encryption at rest; rotate; least privilege.
- Structured PII redaction in logs (phone numbers scrubbed).

## 5. Failure Modes & Recovery

| Scenario | Impact | Mitigation |
|----------|--------|-----------|
| Telegram API downtime | Delayed ingest | Queue local retries; alert if >5m lag |
| WhatsApp token expired | Outbound failures | Pre-expiry rotation job & health check |
| High outbound burst (Telegram polls) | Rate limit 429 | Token bucket + jittered retry |
| Malformed WhatsApp webhook | Drop or partial ingest | Schema validation + sample log |
| Storage latency spike | Slower normalization | Buffer in queue; circuit breaker for downstream |

## 6. Metrics

- Telegram: ingest latency (update->persist), dropped updates, media fetch time, poll response mapping accuracy.
- WhatsApp: template send success %, session reply latency, opt-in coverage %, HMAC failures count.

## 7. Roadmap / Phasing

| Phase | Telegram | WhatsApp |
|-------|----------|----------|
| 1 | Basic ingest, text/media, edits | Template send + session ingest, hashing |
| 2 | Poll API integration, media lazy fetch, backfill tool | Delivery/read analytics, FAQ auto-reply via Ask-DB |
| 3 | Supergroup scale tuning, shard bots if needed | Potential expansion if policy changes |

## 8. Open Questions

1. Do we unify WhatsApp pseudo-channels into a single logical 'WhatsApp Direct' workspace channel for analytics (masked)? (Leaning Yes but exclude from core KPIs initially.)
2. Need a phone opt-out mechanism surfaced to user? (Likely Phase 2; store opt-out timestamp.)
3. Guardian applicability to WhatsApp inbound (subset of regex + toxicity)? (Evaluate cost/benefit).

## 9. Security Checklist (Both)

- [ ] Webhook signature / secret validation
- [ ] Idempotency store
- [ ] Input size caps
- [ ] PII redaction log filter test cases
- [ ] Access token rotation job

---
Owned by: Connectors Team. Update cadence: with each connector capability change.
