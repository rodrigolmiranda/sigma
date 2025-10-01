# Playbook: Connectors

## Quick Start

Choose your platform:
- 📱 [**Telegram Setup Guide**](setup_guide_telegram.md) - Groups & Channels (10 min)
- 💬 [**WhatsApp Setup Guide**](setup_guide_whatsapp.md) - Business 1:1 Support (30 min)

## General Setup Flow

| Step | Action | Validation |
|------|--------|-----------|
| 1 | Register app / bot in platform | Credentials stored (Key Vault) |
| 2 | Configure webhook URL | Test ping success |
| 3 | Enable normalization mapping | Sample event persisted |
| 4 | Verify message ingestion | Query messages in database |

Rollback: disable webhook & revoke credentials.

## Supported Platforms (Phase 1)

| Platform | Status | Use Case | Setup Time | Group Support |
|----------|--------|----------|------------|---------------|
| **Telegram** | ✅ Production | Group analytics, polls, safety | ~10 min | ✅ Yes |
| **WhatsApp** | 🧪 Beta | 1:1 support, broadcasts | ~30 min | ❌ No (API limitation) |
| **Slack** | ✅ Production | Workspace analytics | ~15 min | ✅ Yes |
| Discord | 🔜 Planned | Community management | TBD | ✅ Yes |
| YouTube | 🔜 Phase 2 | Live chat moderation | TBD | N/A |
| Teams | 🔜 Phase 2 | Enterprise collaboration | TBD | ✅ Yes |

## Troubleshooting

### Common Issues

**Webhook not receiving events:**
1. Check webhook URL is publicly accessible (HTTPS required)
2. Verify signing secret/token matches configuration
3. Check platform webhook subscription settings
4. Review SIGMA logs: `docker logs sigma-api | grep webhook`

**Messages missing in database:**
1. Verify workspace `externalId` matches platform ID
2. Check tenant context is correctly set
3. Ensure database migrations are up to date
4. Query `webhook_events` table for processing errors

**Signature validation failures:**
1. Verify app secret/signing secret in configuration
2. Check timestamp tolerance (5 minutes for Slack, 30 seconds for others)
3. Ensure raw request body is preserved (no middleware modifications)

### Debug Commands

```bash
# Check recent webhook deliveries
SELECT platform, status_code, error_message, created_at
FROM webhook_events
WHERE platform IN ('Telegram', 'WhatsApp')
ORDER BY created_at DESC
LIMIT 20;

# Verify workspace configuration
SELECT id, name, platform, external_id, is_active
FROM workspaces
WHERE tenant_id = 'your-tenant-id';

# Check message ingestion rate
SELECT
  DATE_TRUNC('hour', timestamp_utc) as hour,
  platform,
  COUNT(*) as message_count
FROM messages
WHERE timestamp_utc > NOW() - INTERVAL '24 hours'
GROUP BY hour, platform
ORDER BY hour DESC;
```

---
Navigation: [Home](home.md) | [Telegram Setup](setup_guide_telegram.md) | [WhatsApp Setup](setup_guide_whatsapp.md) | Next: [Polling Module](playbook_polling_module.md)
