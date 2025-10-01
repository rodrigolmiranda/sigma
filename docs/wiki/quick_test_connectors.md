# Quick Test: Telegram & WhatsApp Connectors

## 🚀 5-Minute Telegram Test

### Prerequisites
- ✅ SIGMA running locally (`docker-compose up`)
- ✅ PostgreSQL database accessible (port 5433)
- ✅ Telegram account

### Setup Steps

1. **Create bot** (@BotFather):
   ```
   /newbot
   Bot name: Test SIGMA Bot
   Username: test_sigma_analytics_bot

   /setprivacy
   Select bot → Disable
   ```
   Save token: `123456:ABC-DEF...`

2. **Add to test group** and send test message

3. **Get chat ID**:
   ```bash
   curl "https://api.telegram.org/bot<TOKEN>/getUpdates" | jq '.result[0].message.chat.id'
   # Output: -1001234567890
   ```

4. **Configure SIGMA**:
   ```bash
   # Add to appsettings.Development.json or environment
   export Platforms__Telegram__BotTokens__test-tenant="123456:ABC-DEF..."
   ```

5. **Create workspace** (GraphQL or SQL):
   ```graphql
   mutation {
     createWorkspace(input: {
       tenantId: "test-tenant-uuid"
       name: "Test Telegram Group"
       platform: TELEGRAM
       externalId: "-1001234567890"
     }) {
       success
       workspace { id }
     }
   }
   ```

6. **Set webhook**:
   ```bash
   curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
     -d "url=https://your-ngrok-url.ngrok.io/webhooks/telegram/test-tenant"
   ```

7. **Send test message** in group: "Hello SIGMA! 🚀"

8. **Verify**:
   ```sql
   SELECT * FROM messages WHERE workspace_id = 'your-workspace-id' ORDER BY timestamp_utc DESC LIMIT 5;
   ```

---

## 💬 WhatsApp Test (30 min)

### Prerequisites
- ✅ Meta for Developers account
- ✅ Test phone number
- ✅ SIGMA running with HTTPS (ngrok or public domain)

### Setup Steps

1. **Create Meta app**:
   - Go to https://developers.facebook.com
   - Create Business app
   - Add WhatsApp product

2. **Save credentials**:
   - Phone Number ID: `123456789012345`
   - App Secret: From Settings → Basic
   - Temporary token: From WhatsApp → Getting Started

3. **Configure webhook**:
   ```bash
   # Generate verify token
   export VERIFY_TOKEN=$(openssl rand -base64 32)

   # Add to SIGMA config
   export Platforms__WhatsApp__AppSecrets__test-tenant="your-app-secret"
   export Platforms__WhatsApp__VerifyToken="$VERIFY_TOKEN"
   ```

4. **Register webhook in Meta**:
   - Callback URL: `https://your-domain.com/webhooks/whatsapp/test-tenant`
   - Verify Token: Same as above
   - Subscribe to: `messages`, `message_status`

5. **Create workspace**:
   ```graphql
   mutation {
     createWorkspace(input: {
       tenantId: "test-tenant-uuid"
       name: "WhatsApp Business"
       platform: WHATS_APP
       externalId: "123456789012345"
     }) {
       success
       workspace { id }
     }
   }
   ```

6. **Send test message** to business number from WhatsApp

7. **Verify**:
   ```sql
   SELECT platform_message_id, text, sender FROM messages
   WHERE workspace_id = 'your-workspace-id'
   ORDER BY timestamp_utc DESC LIMIT 5;
   ```

---

## 🔍 Verification Checklist

### Telegram
- [ ] Bot receives messages immediately (check getUpdates)
- [ ] Webhook returns 200 OK (check getWebhookInfo)
- [ ] Messages appear in SIGMA database within 2 seconds
- [ ] Message edits are tracked
- [ ] User joins/leaves are captured

### WhatsApp
- [ ] Webhook verification succeeds (green checkmark in Meta)
- [ ] Test message arrives in SIGMA
- [ ] Phone number is hashed in database
- [ ] Message status updates work
- [ ] Media metadata is captured

---

## 🐛 Quick Debug

### Check webhook status

**Telegram:**
```bash
curl "https://api.telegram.org/bot<TOKEN>/getWebhookInfo" | jq '.'
```

**WhatsApp:**
Check Meta app → WhatsApp → Configuration → Webhook → Recent Deliveries

### Check SIGMA logs

```bash
# Docker
docker logs sigma-api --tail 100 | grep -E "telegram|whatsapp"

# Local
dotnet run --project src/API | grep -E "telegram|whatsapp"
```

### Test webhook locally with ngrok

```bash
# Start ngrok
ngrok http 5000

# Use ngrok URL for webhook
# Example: https://abc123.ngrok.io/webhooks/telegram/test-tenant
```

### Simulate webhook event

**Telegram:**
```bash
curl -X POST "http://localhost:5000/webhooks/telegram/test-tenant" \
  -H "Content-Type: application/json" \
  -d '{
    "update_id": 123,
    "message": {
      "message_id": 1,
      "from": {"id": 12345, "first_name": "Test"},
      "chat": {"id": -1001234567890, "type": "group"},
      "date": 1609459200,
      "text": "Test message"
    }
  }'
```

**WhatsApp:**
```bash
# First, get a real payload from Meta webhook test
# Then replay it locally
curl -X POST "http://localhost:5000/webhooks/whatsapp/test-tenant" \
  -H "Content-Type: application/json" \
  -H "X-Hub-Signature-256: sha256=<calculate-signature>" \
  -d @whatsapp-sample-payload.json
```

---

## 📊 Expected Results

### Database Records

After sending "Hello SIGMA! 🚀" in Telegram:

```sql
sigma_test=# SELECT id, platform, text, sender FROM messages ORDER BY timestamp_utc DESC LIMIT 1;

                  id                  | platform |      text       |                    sender
--------------------------------------+----------+-----------------+----------------------------------------------
 a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6 |        2 | Hello SIGMA! 🚀 | {"PlatformUserId":"12345","DisplayName":"Test User","IsBot":false}
```

### GraphQL Query

```graphql
{
  messages(
    workspaceId: "your-workspace-uuid"
    first: 1
    orderBy: {timestamp: DESC}
  ) {
    nodes {
      id
      platform
      text
      sender {
        displayName
      }
      timestampUtc
    }
  }
}
```

Response:
```json
{
  "data": {
    "messages": {
      "nodes": [
        {
          "id": "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6",
          "platform": "TELEGRAM",
          "text": "Hello SIGMA! 🚀",
          "sender": {
            "displayName": "Test User"
          },
          "timestampUtc": "2025-10-01T12:34:56.789Z"
        }
      ]
    }
  }
}
```

---

## 🎯 Success Criteria

✅ **Telegram:**
- Message latency < 2 seconds
- All text messages captured
- Edits tracked (if bot is admin)
- Emoji rendering correct
- No duplicate messages

✅ **WhatsApp:**
- Webhook verification passes
- 1:1 messages captured
- Phone numbers hashed
- Media metadata stored
- Status updates received

---

## 📚 Next Steps

After successful test:
1. 📖 [Full Telegram Setup Guide](setup_guide_telegram.md)
2. 📖 [Full WhatsApp Setup Guide](setup_guide_whatsapp.md)
3. 📊 [View Analytics](tw_pg_01_1_dashboard.md)
4. 🔍 [Enable Ask-DB](tw_pg_05_1_ask.md)

---

**Questions?** Check [Troubleshooting](playbook_connectors.md#troubleshooting) or [Architecture Docs](architecture_overview.md)

---
Navigation: [Home](home.md) | [Connector Playbook](playbook_connectors.md) | [Telegram Setup](setup_guide_telegram.md) | [WhatsApp Setup](setup_guide_whatsapp.md)
