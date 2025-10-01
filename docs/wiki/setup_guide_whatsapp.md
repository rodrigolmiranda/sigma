# WhatsApp Connector Setup Guide

## Overview

This guide walks you through connecting WhatsApp Business to SIGMA. Note: WhatsApp is an "adjacent" channel focused on **1:1 customer support** and **broadcast messages**. Group analytics are NOT available due to Meta API limitations.

**Time to complete:** ~30 minutes
**Prerequisites:**
- Facebook Business account
- Phone number for WhatsApp Business
- Meta for Developers account

---

## Step 1: Create WhatsApp Business App

### 1.1 Set Up Meta for Developers

1. Go to [Meta for Developers](https://developers.facebook.com)
2. Sign in with your Facebook account
3. Click **My Apps** → **Create App**
4. Select **Business** as the app type
5. Fill in app details:
   - **App Name:** `SIGMA WhatsApp Integration`
   - **App Contact Email:** Your business email
   - **Business Account:** Select or create a business account

### 1.2 Add WhatsApp Product

1. In your app dashboard, find **WhatsApp** product
2. Click **Set up**
3. Select your **Business Portfolio**
4. Follow the setup wizard

### 1.3 Configure Phone Number

1. Go to **WhatsApp** → **Getting Started**
2. You'll see a test phone number (limited to 5 recipients)
3. For production, click **Add Phone Number**:
   - Enter your business phone number
   - Verify via SMS or voice call
   - Accept WhatsApp Business terms

---

## Step 2: Get API Credentials

### 2.1 Save Your Credentials

From the **WhatsApp** → **Getting Started** page:

1. **Phone Number ID** (e.g., `123456789012345`)
2. **WhatsApp Business Account ID** (e.g., `987654321098765`)
3. **App Secret** (under **Settings** → **Basic**)

### 2.2 Generate Access Token

1. Go to **WhatsApp** → **Getting Started**
2. Under **Temporary access token**, click **Generate**
3. **Save this token** (24-hour validity for testing)

For production:
1. Go to **Settings** → **Basic** → **App Secret**
2. Or create a **System User** for permanent tokens:
   - Go to **Business Settings** → **Users** → **System Users**
   - Create new system user
   - Assign **WhatsApp Business Management** permission
   - Generate token (never expires)

---

## Step 3: Configure Webhook in SIGMA

### 3.1 Set Up App Secret in SIGMA

**appsettings.json** or **Environment Variables:**
```json
{
  "Platforms": {
    "WhatsApp": {
      "AppSecrets": {
        "your-tenant-id": "your-app-secret-from-meta"
      },
      "DefaultAppSecret": "default-app-secret",
      "VerifyToken": "your-random-verify-token-min-20-chars"
    }
  }
}
```

Generate a verify token:
```bash
# Generate random 32-character token
openssl rand -base64 32
```

### 3.2 Register Webhook with Meta

1. In your Meta app, go to **WhatsApp** → **Configuration**
2. Click **Edit** next to Webhook
3. Enter:
   - **Callback URL:** `https://your-sigma-domain.com/webhooks/whatsapp/your-tenant-id`
   - **Verify Token:** Same token from your config above
4. Click **Verify and Save**

Meta will send a verification request to your webhook. SIGMA will automatically respond if configured correctly.

### 3.3 Subscribe to Webhook Events

After verification, subscribe to these fields:
- ✅ **messages** (incoming messages)
- ✅ **message_status** (delivery, read receipts)
- ❌ **message_template_status_update** (optional, for templates)

---

## Step 4: Create Workspace in SIGMA

### 4.1 Get Your Phone Number ID

From **WhatsApp** → **API Setup**, copy the **Phone Number ID** (not the phone number itself).

### 4.2 Register in SIGMA Database

**Using GraphQL Mutation:**

```graphql
mutation CreateWhatsAppWorkspace {
  createWorkspace(input: {
    tenantId: "your-tenant-uuid"
    name: "WhatsApp Business Support"
    platform: WHATS_APP
    externalId: "123456789012345"  # Your Phone Number ID
  }) {
    success
    workspace {
      id
      name
      platform
      externalId
    }
    errors {
      message
      code
    }
  }
}
```

**Or Direct Database Insert:**

```sql
INSERT INTO workspaces (id, tenant_id, name, platform, external_id, is_active, created_at, updated_at)
VALUES (
  gen_random_uuid(),
  'your-tenant-uuid',
  'WhatsApp Business Support',
  5,  -- 5 = WhatsApp (see Platform enum)
  '123456789012345',  -- Phone Number ID
  true,
  now(),
  now()
);
```

---

## Step 5: Verify Connection

### 5.1 Send Test Message

1. Send a WhatsApp message to your business number from a test phone
2. Message: `Hello SIGMA! 🚀`

### 5.2 Check Webhook Deliveries

In Meta app dashboard:
1. Go to **WhatsApp** → **Configuration** → **Webhook**
2. Click **Test** → Select `messages` event
3. Should see `200 OK` response

### 5.3 Query SIGMA Database

```sql
SELECT id, platform_message_id, sender, text, timestamp_utc
FROM messages
WHERE workspace_id = 'your-workspace-id'
  AND platform = 5  -- WhatsApp
ORDER BY timestamp_utc DESC
LIMIT 10;
```

Or via GraphQL:
```graphql
query GetWhatsAppMessages {
  messages(
    workspaceId: "your-workspace-uuid"
    platform: WHATS_APP
    first: 10
    orderBy: { timestamp: DESC }
  ) {
    nodes {
      id
      text
      sender {
        displayName
        platformUserId  # Hashed phone number
      }
      timestampUtc
    }
  }
}
```

---

## Troubleshooting

### Webhook verification fails

**Check verify token:**
- Must match exactly between SIGMA config and Meta settings
- Minimum 20 characters recommended

**Check webhook endpoint:**
```bash
curl -X GET "https://your-sigma-domain.com/webhooks/whatsapp/test-tenant?hub.mode=subscribe&hub.verify_token=YOUR_TOKEN&hub.challenge=test123"
# Should return: test123
```

### Messages not arriving

**Check webhook subscription:**
- Go to Meta app → **WhatsApp** → **Configuration**
- Ensure `messages` is checked under Webhook fields

**Check SIGMA logs:**
```bash
docker logs sigma-api | grep "whatsapp"
```

**Verify webhook signature:**
- SIGMA validates X-Hub-Signature-256 header
- Check app secret matches in config

### Invalid signature errors

```bash
# In SIGMA logs you might see:
# "WhatsApp webhook signature validation failed"
```

**Fix:**
1. Verify app secret in config matches Meta app settings
2. Check tenant ID in webhook URL matches config key
3. Regenerate app secret in Meta if compromised

---

## What Gets Captured

✅ **Supported:**
- 1:1 text messages (customer → business)
- Media messages (images, documents) - metadata + download URL
- Message status (sent, delivered, read, failed)
- Quick reply responses
- Button click responses

❌ **Not Supported (Meta API Limitations):**
- ❌ Group messages (WhatsApp doesn't provide group analytics API)
- ❌ User profile photos
- ❌ Status updates
- ❌ Voice calls
- ❌ Video calls

---

## Security & Privacy

⚠️ **WhatsApp Privacy Requirements:**
- Phone numbers are **hashed (SHA256 + per-tenant salt)** before storage
- Never stored in plaintext except in encrypted `Raw` field
- `Raw` field purged after 30 days (or tenant retention window)
- No analytics on group conversations (not supported by Meta)

**Compliance:**
- Messages stored with consent (business communication)
- 24-hour customer care window enforced by Meta
- GDPR: Right to delete implemented via tenant purge

---

## Message Templates & Broadcasting

WhatsApp requires pre-approved templates for outbound messages outside 24-hour window.

### Create Template (Meta Business Manager)

1. Go to **WhatsApp Manager** → **Message Templates**
2. Click **Create Template**
3. Example template:
   ```
   Name: welcome_message
   Category: MARKETING
   Language: English

   Content:
   Hello {{1}}! Welcome to SIGMA Analytics.
   Reply with "help" for assistance.
   ```
4. Submit for approval (usually 24-48 hours)

### Send Template via SIGMA

```graphql
mutation SendWhatsAppTemplate {
  sendWhatsAppMessage(input: {
    workspaceId: "your-workspace-uuid"
    recipientPhone: "+1234567890"
    templateName: "welcome_message"
    templateParams: ["John"]
  }) {
    success
    messageId
    errors {
      message
    }
  }
}
```

---

## Rate Limits

**Test Number (Free):**
- 250 conversations/day
- 1,000 messages/day
- Maximum 5 recipients

**Production (Paid):**
- Tier 1: 1,000 conversations/day
- Tier 2: 10,000 conversations/day (after quality rating)
- Tier 3: 100,000 conversations/day (after quality rating)

**Quality Rating:**
- Maintain >60% quality score
- Low quality = reduced limits
- Blocks = account suspension

---

## Next Steps

1. ✅ **Setup complete!** WhatsApp messages are flowing
2. 📤 [Setup Broadcast Templates](tw_pg_06_1_broadcast_composer.md)
3. 🤖 [Configure Auto-Responses](playbook_rag_module.md)
4. 📊 [View Support Analytics](tw_pg_07_1_engagement_analytics.md)
5. 💰 [Monitor Usage & Billing](tw_pg_09_1_billing.md)

---

## Additional Resources

- [Meta for Developers - WhatsApp](https://developers.facebook.com/docs/whatsapp)
- [WhatsApp Business API Pricing](https://developers.facebook.com/docs/whatsapp/pricing)
- [Message Templates Best Practices](https://developers.facebook.com/docs/whatsapp/message-templates/guidelines)
- [SIGMA Connector Playbook](playbook_connectors.md)

---

**Need help?** Check [Architecture Overview](architecture_overview.md) or [Connector Deep Dive](../Connectors_WhatsApp_Telegram.md)

---
Navigation: [Home](home.md) | [Telegram Setup](setup_guide_telegram.md) | [Connector Playbook](playbook_connectors.md)
