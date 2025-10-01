# Telegram Connector Setup Guide

## Overview

This guide walks you through connecting a Telegram group to SIGMA for message analytics, polling, and safety monitoring.

**Time to complete:** ~10 minutes
**Prerequisites:** Telegram account, admin access to target group

---

## Step 1: Create a Telegram Bot

1. Open Telegram and search for `@BotFather`
2. Start a chat and send `/newbot`
3. Follow the prompts:
   - **Bot name:** `SIGMA Analytics Bot` (or your preferred name)
   - **Bot username:** Must end in `bot` (e.g., `sigma_analytics_bot`)
4. **Save the bot token** - you'll need this later (format: `123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11`)

### Important: Disable Privacy Mode

By default, bots only receive messages that mention them. To capture all group messages:

1. Send `/setprivacy` to @BotFather
2. Select your bot
3. Choose **Disable** privacy mode
4. Confirm the change

---

## Step 2: Add Bot to Your Group

1. Open your Telegram group
2. Go to **Group Info** → **Add Members**
3. Search for your bot username (e.g., `@sigma_analytics_bot`)
4. Add the bot to the group

### Optional: Make Bot an Admin

For reliable capture of message edits and deletes:

1. Go to **Group Info** → **Administrators**
2. **Add Administrator** → Select your bot
3. Grant permissions:
   - ✅ **Delete Messages** (required for delete tracking)
   - ✅ **Pin Messages** (optional, for poll management)
   - ❌ Other permissions can stay disabled

---

## Step 3: Configure Webhook in SIGMA

### 3.1 Get Your Group Chat ID

Send a test message in your group, then visit:
```
https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
```

Look for `"chat":{"id":-1001234567890}` in the response. Save this chat ID.

### 3.2 Register in SIGMA Database

**Option A: Using GraphQL Mutation (Recommended)**

```graphql
mutation CreateTelegramWorkspace {
  createWorkspace(input: {
    tenantId: "your-tenant-uuid"
    name: "My Telegram Group"
    platform: TELEGRAM
    externalId: "-1001234567890"  # Your chat ID
  }) {
    success
    workspace {
      id
      name
      platform
    }
    errors {
      message
      code
    }
  }
}
```

**Option B: Direct Database Insert**

```sql
-- Replace with your actual values
INSERT INTO workspaces (id, tenant_id, name, platform, external_id, is_active, created_at, updated_at)
VALUES (
  gen_random_uuid(),
  'your-tenant-uuid',
  'My Telegram Group',
  2,  -- 2 = Telegram (see Platform enum)
  '-1001234567890',
  true,
  now(),
  now()
);
```

### 3.3 Set Telegram Webhook

Configure your bot token in application settings:

**appsettings.json** or **Environment Variables:**
```json
{
  "Platforms": {
    "Telegram": {
      "BotTokens": {
        "your-tenant-id": "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"
      },
      "DefaultBotToken": "your-default-token"
    }
  }
}
```

**Set the webhook URL:**
```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://your-sigma-domain.com/webhooks/telegram/your-tenant-id",
    "allowed_updates": ["message", "edited_message", "my_chat_member"]
  }'
```

Expected response:
```json
{
  "ok": true,
  "result": true,
  "description": "Webhook was set"
}
```

---

## Step 4: Verify Connection

### 4.1 Send Test Message

Send a message in your Telegram group:
```
Hello SIGMA! 🚀
```

### 4.2 Check Webhook Status

```bash
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"
```

Look for:
- `"url"`: Your webhook URL
- `"has_custom_certificate": false`
- `"pending_update_count": 0` (if messages are being processed)
- `"last_error_date"`: Should be empty or old

### 4.3 Query SIGMA Database

```sql
SELECT id, platform_message_id, sender, text, timestamp_utc
FROM messages
WHERE workspace_id = 'your-workspace-id'
ORDER BY timestamp_utc DESC
LIMIT 10;
```

Or use GraphQL:
```graphql
query GetRecentMessages {
  messages(
    workspaceId: "your-workspace-uuid"
    first: 10
    orderBy: { timestamp: DESC }
  ) {
    nodes {
      id
      text
      sender {
        displayName
      }
      timestampUtc
    }
  }
}
```

---

## Troubleshooting

### Bot not receiving messages

**Check privacy mode:**
```bash
# Should show "privacy_mode": "disabled"
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getMe"
```

**Verify bot is in group:**
```bash
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates"
# Should show recent group messages
```

### Webhook errors

**Check webhook info:**
```bash
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"
```

Common issues:
- `"last_error_message": "SSL error"` → Check your SSL certificate
- `"last_error_message": "Wrong response"` → Check webhook handler returns 200 OK
- High `pending_update_count` → Webhook processing is slow/failing

**View SIGMA logs:**
```bash
# Check for webhook processing errors
docker logs sigma-api | grep "telegram"
```

### Missing edits/deletes

- Ensure bot is an **administrator** with **Delete Messages** permission
- Privacy mode must be **disabled**

---

## What Gets Captured

✅ **Supported:**
- Text messages
- Media messages (images, videos, documents) - metadata only
- Message edits
- Message deletes (if bot is admin)
- User joins/leaves
- Reactions (via bot API polling, not webhook)
- Replies (reply chains)

❌ **Not Supported:**
- Voice messages (metadata only)
- Video notes (metadata only)
- Stickers (as image attachments)
- Private 1:1 messages (groups only)

---

## Security & Privacy

- Bot token is stored encrypted in SIGMA configuration
- Message `Raw` field (full Telegram payload) is encrypted at rest
- User phone numbers are never stored
- Message retention follows tenant settings (default: 30 days)

---

## Next Steps

1. ✅ **Setup complete!** Messages are now flowing into SIGMA
2. 📊 [View Analytics Dashboard](tw_pg_01_1_dashboard.md)
3. 📋 [Create Polls](playbook_polling_module.md)
4. 🛡️ [Enable Safety Monitoring](playbook_guardian_module.md)
5. 🔍 [Ask Questions with RAG](tw_pg_05_1_ask.md)

---

**Need help?** Check [Connector Playbook](playbook_connectors.md) or review [Architecture](architecture_overview.md)

---
Navigation: [Home](home.md) | [WhatsApp Setup](setup_guide_whatsapp.md) | [Connector Playbook](playbook_connectors.md)
